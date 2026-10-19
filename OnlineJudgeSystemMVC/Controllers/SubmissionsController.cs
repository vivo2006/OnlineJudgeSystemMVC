using System.Data;
using System.Security.Claims;
using BusinessLayer.API;
using BusinessLayer.Entities;
using DataLayer.Contexts;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineJudgeSystemMVC.Models;
using OnlineJudgeSystemMVC.Services;
using ServiceLayer.Storage;

namespace OnlineJudgeSystemMVC.Controllers;

[Authorize(Roles = "Admin,User")]
public class SubmissionsController : Controller
{
    private static readonly int[] Languages = { 54, 62, 71, 63, 51 };
    private readonly OnlineJudgeDbContext _db;
    private readonly IBackgroundJobClient _jobs;
    private readonly JudgingOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SubmissionsController> _logger;

    public SubmissionsController(OnlineJudgeDbContext db, IBackgroundJobClient jobs, IOptions<JudgingOptions> options,
        IWebHostEnvironment environment, ILogger<SubmissionsController> logger)
        => (_db, _jobs, _options, _environment, _logger) = (db, jobs, options.Value, environment, logger);

    public async Task<IActionResult> Index(int problemId)
    {
        var problem = await _db.Problems.FindAsync(problemId);
        if (problem == null) return NotFound();
        ViewBag.Problem = problem.Name;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return View(await _db.Submissions.AsNoTracking().Include(s => s.Problem).ThenInclude(p => p.Competition)
            .ThenInclude(g => g.YearCompetition).ThenInclude(y => y.Competition)
            .Where(s => s.UserId == userId && s.ProblemId == problemId).OrderByDescending(s => s.Created).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var submission = await _db.Submissions.AsNoTracking().Include(s => s.TestCaseResults)
            .SingleOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        return submission == null ? NotFound() : View(submission);
    }

    public IActionResult Create(int problemId) => RedirectToAction("Details", "Problems", new { id = problemId });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSubmissionRequest request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Code) || !Languages.Contains(request.LanguageId))
            return BadRequest("Provide source code (up to 50,000 characters) and a supported language.");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();
        var problem = await _db.Problems.Include(p => p.Competition).ThenInclude(g => g.YearCompetition)
            .ThenInclude(y => y.Competition).SingleOrDefaultAsync(p => p.Id == request.ProblemId);
        if (problem == null) return NotFound();
        try
        {
            var year = problem.Competition.YearCompetition;
            ProblemStorage.TestPairs(ProblemStorage.DirectoryFor(Path.Combine(_environment.ContentRootPath, "TestCases"),
                year.Competition.Notation, year.Year, problem.ProblemNotation), _options.MaxTestCases);
        }
        catch (Exception e) when (e is IOException or ArgumentException or InvalidOperationException)
        { return BadRequest("This problem does not have a valid test set within the configured test limit."); }

        var now = DateTime.UtcNow;
        var dayStart = now.Date;
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var attempts = await _db.Submissions.Where(s => s.UserId == userId && s.Created >= dayStart).ToListAsync();
        if (attempts.Count >= _options.MaxSubmissionsPerDay ||
            await _db.Submissions.CountAsync(s => s.Created >= dayStart) >= _options.MaxGlobalSubmissionsPerDay ||
            attempts.Any(s => s.Created > now.AddSeconds(-_options.SubmissionCooldownSeconds)))
            return StatusCode(429, "Submission limit reached. Please wait before trying again.");
        var submission = new Submission(request.ProblemId, now, userId)
        { Code = request.Code, LanguageId = request.LanguageId, Status = "Queued" };
        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        try { _jobs.Enqueue<SubmissionProcessor>(processor => processor.ProcessAsync(submission.Id)); }
        catch (Exception e)
        {
            submission.Status = "Failed";
            submission.Error = "The submission could not be queued. No judge request was made.";
            await _db.SaveChangesAsync();
            _logger.LogWarning("Enqueueing submission {SubmissionId} failed ({ExceptionType}).", submission.Id, e.GetType().Name);
        }
        return RedirectToAction(nameof(Details), new { id = submission.Id });
    }

    public async Task<IActionResult> OutputDetails(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _db.TestCaseResults.AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == id && r.Submission.UserId == userId);
        return result == null ? NotFound() : View("OutputDetails", result.Output);
    }
}
