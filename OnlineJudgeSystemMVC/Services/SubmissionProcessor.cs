using System.Data;
using System.Globalization;
using BusinessLayer.API;
using BusinessLayer.Entities;
using DataLayer.Contexts;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceLayer.EntityServices;
using ServiceLayer.ExternalServices;
using ServiceLayer.Storage;

namespace OnlineJudgeSystemMVC.Services;

public class SubmissionProcessor
{
    private readonly OnlineJudgeDbContext _db;
    private readonly IJudge0Service _judge;
    private readonly IWebHostEnvironment _environment;
    private readonly JudgingOptions _options;
    private readonly ILogger<SubmissionProcessor> _logger;

    public SubmissionProcessor(OnlineJudgeDbContext db, IJudge0Service judge, IWebHostEnvironment environment,
        IOptions<JudgingOptions> options, ILogger<SubmissionProcessor> logger)
        => (_db, _judge, _environment, _options, _logger) = (db, judge, environment, options.Value, logger);

    // A failed POST may already have reached the provider. Do not automatically resubmit quota-limited work.
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(120)]
    public async Task ProcessAsync(int submissionId)
    {
        var submission = await _db.Submissions.Include(s => s.Problem).ThenInclude(p => p.Competition)
            .ThenInclude(g => g.YearCompetition).ThenInclude(y => y.Competition)
            .SingleOrDefaultAsync(s => s.Id == submissionId);
        if (submission == null || submission.Status != "Queued") return;
        submission.Status = "Running";
        await _db.SaveChangesAsync();
        try
        {
            var problem = submission.Problem;
            var year = problem.Competition.YearCompetition;
            var directory = ProblemStorage.DirectoryFor(Path.Combine(_environment.ContentRootPath, "TestCases"),
                year.Competition.Notation, year.Year, problem.ProblemNotation);
            var (inputs, outputs) = ProblemStorage.TestPairs(directory, _options.MaxTestCases);
            var request = await _judge.CreatePostQueryAsync(inputs, outputs, submission.LanguageId,
                submission.Code, problem.TimeLimit, problem.MemoryLimit * 1024);
            var tokens = await _judge.SubmitBatchAsync(request);
            if (tokens.Count != inputs.Length || tokens.Any(t => string.IsNullOrWhiteSpace(t.Token)))
                throw new InvalidOperationException("Judge returned an incomplete token batch.");
            await Task.Delay(TimeSpan.FromSeconds(_options.ResultDelaySeconds));
            var results = await _judge.GetBatchResultsAsync(tokens);
            // One POST + one GET only. Pending results must not be scored or trigger more API calls.
            if (results.Submissions == null || results.Submissions.Count != inputs.Length ||
                results.Submissions.Any(r => r.Status == null || r.Status.id < 3))
            {
                submission.Status = "Incomplete";
                submission.Error = "The judge had not finished at the single result check. No extra API requests were made.";
                await _db.SaveChangesAsync();
                return;
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var user = await _db.Users.SingleAsync(u => u.Id == submission.UserId);
            var previous = await _db.Submissions.Include(s => s.Problem)
                .Where(s => s.UserId == user.Id && s.Status == "Completed").ToListAsync();
            submission.TestCaseResults = results.Submissions.Select(r => new TestCaseResult(submission.Id,
                r.Status.description, (int)Math.Ceiling((r.Memory ?? 0) / 1024.0), r.Stdout ?? "", r.Time ?? "0")).ToList();
            submission.Time = results.Submissions.Max(r => double.Parse(r.Time ?? "0", CultureInfo.InvariantCulture));
            submission.Memory = submission.TestCaseResults.Max(r => r.Memory);
            submission.Points = (int)Math.Round(100m * results.Submissions.Count(r => r.Status.id == 3) / inputs.Length);
            submission.Error = results.Submissions.FirstOrDefault(r => r.Status.id != 3)?.Status.description;
            submission.Status = "Completed";
            submission.ChallengeBonus = SubmissionScoring.EarnsChallengeBonus(submission, user, previous) ? 10 : 0;
            previous.Add(submission);
            user.Points = SubmissionScoring.Calculate(previous);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception exception)
        {
            _db.ChangeTracker.Clear();
            var failed = await _db.Submissions.FindAsync(submissionId);
            if (failed != null)
            {
                failed.Status = "Failed";
                failed.Error = "Judging could not be completed. No automatic API retry was attempted.";
                await _db.SaveChangesAsync();
            }
            _logger.LogWarning("Judging submission {SubmissionId} failed ({ExceptionType}).", submissionId, exception.GetType().Name);
        }
    }
}
