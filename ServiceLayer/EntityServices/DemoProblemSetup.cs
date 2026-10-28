using System.Data;
using BusinessLayer.Entities;
using DataLayer.Contexts;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Storage;

namespace ServiceLayer.EntityServices;

/// <summary>Places the original sample in the archive without changing its ID or submissions.</summary>
public static class DemoProblemSetup
{
    public const string ProblemNotation = "E_DEMO_SUM";
    public const string StatementUrl = "/demo/sum.html";
    public sealed record Placement(string CompetitionNotation, int Year, int ProblemId);

    public static async Task<Placement?> EnsureAsync(OnlineJudgeDbContext db, string contentRoot, bool createIfMissing = false)
    {
        var problem = await db.Problems.Include(p => p.Competition).ThenInclude(g => g.YearCompetition)
            .ThenInclude(y => y.Competition).SingleOrDefaultAsync(p => p.ProblemNotation == ProblemNotation && p.URL == StatementUrl);
        if (problem == null && !createIfMissing) return null;
        var oldCompetition = problem?.Competition.YearCompetition.Competition;
        var alreadyInArchive = oldCompetition != null && oldCompetition.Notation != "DEMO";
        var competition = alreadyInArchive ? oldCompetition : await db.Competitions.Where(c => c.Notation != "DEMO")
            .OrderByDescending(c => c.Notation == "\u041d\u0415\u0422\u0418" || c.Notation == "NETI")
            .ThenBy(c => c.Id).FirstOrDefaultAsync();
        if (competition == null)
        {
            competition = new Competition { Name = "National Autumn Informatics Tournament", Notation = "\u041d\u0415\u0422\u0418", FirstYear = 2011, LastYear = 2011 };
            db.Competitions.Add(competition);
        }
        var year = alreadyInArchive ? problem!.Competition.YearCompetition : competition.Id == 0 ? null : await db.YearCompetitions.Where(y => y.CompetitionId == competition.Id)
            .OrderByDescending(y => y.Year == 2011).ThenBy(y => y.Year).ThenBy(y => y.Id).FirstOrDefaultAsync();
        if (year == null)
        {
            year = new YearCompetition { Competition = competition, Year = 2011 };
            db.YearCompetitions.Add(year);
        }
        var alreadyPlaced = problem != null && problem.YearCompetitionId == year.Id && alreadyInArchive;
        var group = year.Id == 0 ? null : await db.GroupCompetitions.FirstOrDefaultAsync(g => g.YearCompetitionId == year.Id && g.Group == "E");
        if (group == null)
        {
            group = new GroupCompetition { Group = "E", YearCompetition = year };
            db.GroupCompetitions.Add(group);
        }
        var existingProblemId = problem?.Id ?? 0;
        if (year.Id != 0 && await db.Problems.AnyAsync(p => p.YearCompetitionId == year.Id && p.ProblemNotation == ProblemNotation && p.Id != existingProblemId))
            throw new InvalidOperationException("The selected archive year already contains another problem with the sample identifier.");

        // Keep the original sample files as an immutable source and copy them to the normal archive path.
        // Complete filesystem validation before changing the database placement.
        var source = ProblemStorage.DirectoryFor(Path.Combine(contentRoot, "TestCases"), "DEMO", 2026, ProblemNotation);
        var destination = ProblemStorage.DirectoryFor(Path.Combine(contentRoot, "TestCases"), competition.Notation, year.Year, ProblemNotation);
        var pairs = ProblemStorage.TestPairs(source, 20);
        foreach (var (files, folder) in new[] { (pairs.Inputs, "Input"), (pairs.Outputs, "Output") })
        {
            Directory.CreateDirectory(Path.Combine(destination, folder));
            foreach (var file in files)
            {
                var target = Path.Combine(destination, folder, Path.GetFileName(file));
                if (File.Exists(target))
                {
                    if (!(await File.ReadAllBytesAsync(target)).SequenceEqual(await File.ReadAllBytesAsync(file)))
                        throw new InvalidOperationException("Refusing to overwrite different existing sample test data.");
                }
                else File.Copy(file, target);
            }
        }
        var destinationPairs = ProblemStorage.TestPairs(destination, 20);
        if (destinationPairs.Inputs.Length != pairs.Inputs.Length)
            throw new InvalidOperationException("The destination contains unexpected sample tests.");

        if (alreadyPlaced) return new Placement(competition.Notation, year.Year, problem!.Id);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (problem == null)
        {
            problem = new Problem { Name = "Sum two integers", ProblemNotation = ProblemNotation, URL = StatementUrl, TimeLimit = 1, MemoryLimit = 64 };
            db.Problems.Add(problem);
        }
        problem.Competition = group;
        problem.YearCompetition = year;
        await db.SaveChangesAsync();
        // Remove only our now-empty demo shell. Any other problems in that competition are retained.
        if (oldCompetition?.Notation == "DEMO" && !await db.Problems.AnyAsync(p =>
            p.Competition.YearCompetition.CompetitionId == oldCompetition.Id || p.YearCompetition.CompetitionId == oldCompetition.Id))
        {
            db.Competitions.Remove(oldCompetition);
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        return new Placement(competition.Notation, year.Year, problem.Id);
    }
}
