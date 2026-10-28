using BusinessLayer.Entities;
using DataLayer.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntityServices;
using ServiceLayer.Storage;

namespace TestingLayer;

[TestFixture]
public class DemoProblemSetupTests
{
    private SqliteConnection _connection = null!;
    private OnlineJudgeDbContext _db = null!;
    private string _root = null!;

    [SetUp]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _db = new OnlineJudgeDbContext(new DbContextOptionsBuilder<OnlineJudgeDbContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        _root = Path.Combine(Path.GetTempPath(), "codexpert-placement-" + Guid.NewGuid().ToString("N"));
        var source = ProblemStorage.DirectoryFor(Path.Combine(_root, "TestCases"), "DEMO", 2026, DemoProblemSetup.ProblemNotation);
        Directory.CreateDirectory(Path.Combine(source, "Input"));
        Directory.CreateDirectory(Path.Combine(source, "Output"));
        File.WriteAllText(Path.Combine(source, "Input", "sum.01.in"), "2 3");
        File.WriteAllText(Path.Combine(source, "Output", "sum.01.out"), "5");
    }

    [TearDown]
    public async Task Cleanup()
    {
        await _db.DisposeAsync(); await _connection.DisposeAsync();
        if (Path.GetFullPath(_root).StartsWith(Path.Combine(Path.GetTempPath(), "codexpert-placement-"), StringComparison.OrdinalIgnoreCase) && Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private YearCompetition ArchiveYear(int year = 2011)
    {
        var competition = new Competition { Name = "Autumn tournament", Notation = "NETI", FirstYear = 2010, LastYear = 2015 };
        var entry = new YearCompetition { Competition = competition, Year = year };
        _db.Add(entry); return entry;
    }

    private Problem OldSample()
    {
        var demo = new Competition { Name = "English demo", Notation = "DEMO", FirstYear = 2026, LastYear = 2026 };
        var year = new YearCompetition { Competition = demo, Year = 2026 };
        var group = new GroupCompetition { Group = "E", YearCompetition = year };
        var problem = new Problem { Name = "Sum two integers", URL = DemoProblemSetup.StatementUrl, ProblemNotation = DemoProblemSetup.ProblemNotation,
            Competition = group, YearCompetition = year, TimeLimit = 1, MemoryLimit = 64 };
        _db.Add(problem); return problem;
    }

    [Test]
    public async Task ExistingSampleMovesWithoutChangingItsIdOrSubmissionHistory()
    {
        var target = ArchiveYear(); var sample = OldSample();
        var user = new ApplicationUser("demo-user", "test@example.test", "Demo", "User");
        var submission = new Submission { Problem = sample, User = user, Code = "print(5)", Created = DateTime.UtcNow, Status = "Completed", Points = 100 };
        _db.Add(submission); await _db.SaveChangesAsync();
        int originalId = sample.Id, submissionId = submission.Id;
        var moved = await DemoProblemSetup.EnsureAsync(_db, _root);
        Assert.That(moved!.Year, Is.EqualTo(2011));
        Assert.That(moved.ProblemId, Is.EqualTo(originalId));
        Assert.That(sample.YearCompetitionId, Is.EqualTo(target.Id));
        Assert.That((await _db.Submissions.SingleAsync(s => s.Id == submissionId)).ProblemId, Is.EqualTo(originalId));
        Assert.That(await _db.Competitions.AnyAsync(c => c.Notation == "DEMO"), Is.False);
        var directory = ProblemStorage.DirectoryFor(Path.Combine(_root, "TestCases"), "NETI", 2011, DemoProblemSetup.ProblemNotation);
        Assert.That(ProblemStorage.TestPairs(directory, 20).Inputs.Length, Is.EqualTo(1));
        await DemoProblemSetup.EnsureAsync(_db, _root, createIfMissing: true);
        Assert.That(await _db.Problems.CountAsync(p => p.ProblemNotation == DemoProblemSetup.ProblemNotation), Is.EqualTo(1));
    }

    [Test]
    public async Task FreshSeedUsesAnArchiveCompetitionAndNoDemoCompetition()
    {
        var placement = await DemoProblemSetup.EnsureAsync(_db, _root, createIfMissing: true);
        Assert.That(placement!.CompetitionNotation, Is.Not.EqualTo("DEMO"));
        Assert.That(placement.Year, Is.EqualTo(2011));
        Assert.That(await _db.Competitions.CountAsync(), Is.EqualTo(1));
        Assert.That(await _db.Problems.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task AbsentSampleIsNotCreatedDuringNormalStartup()
    {
        Assert.That(await DemoProblemSetup.EnsureAsync(_db, _root), Is.Null);
        Assert.That(await _db.Competitions.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task OtherProblemsInTheOldCompetitionArePreserved()
    {
        ArchiveYear(); var sample = OldSample();
        var other = new Problem { Name = "User problem", URL = "/demo/other.html", ProblemNotation = "E_OTHER", Competition = sample.Competition, YearCompetition = sample.YearCompetition };
        _db.Add(other); await _db.SaveChangesAsync();
        int otherId = other.Id;
        await DemoProblemSetup.EnsureAsync(_db, _root);
        Assert.That(await _db.Competitions.AnyAsync(c => c.Notation == "DEMO"), Is.True);
        Assert.That((await _db.Problems.Include(p => p.Competition).ThenInclude(g => g.YearCompetition).ThenInclude(y => y.Competition).SingleAsync(p => p.Id == otherId)).Competition.YearCompetition.Competition!.Notation, Is.EqualTo("DEMO"));
    }

    [Test]
    public async Task ExistingArchivePlacementIsRespected()
    {
        var year = ArchiveYear(2015); var group = new GroupCompetition { Group = "E", YearCompetition = year };
        var sample = new Problem { Name = "Sum two integers", URL = DemoProblemSetup.StatementUrl, ProblemNotation = DemoProblemSetup.ProblemNotation, Competition = group, YearCompetition = year };
        _db.Add(sample); await _db.SaveChangesAsync();
        var placement = await DemoProblemSetup.EnsureAsync(_db, _root);
        Assert.That(placement!.Year, Is.EqualTo(2015));
        Assert.That(await _db.YearCompetitions.CountAsync(), Is.EqualTo(1));
        var directory = ProblemStorage.DirectoryFor(Path.Combine(_root, "TestCases"), "NETI", 2015, DemoProblemSetup.ProblemNotation);
        Assert.That(Directory.Exists(directory), Is.True);
    }

    [Test]
    public async Task MissingFixturesCannotMoveAnExistingSample()
    {
        ArchiveYear(); var sample = OldSample(); await _db.SaveChangesAsync();
        int oldYear = sample.YearCompetitionId;
        var isolated = Path.Combine(_root, "missing-fixtures");
        Assert.ThrowsAsync<DirectoryNotFoundException>(() => DemoProblemSetup.EnsureAsync(_db, isolated));
        _db.ChangeTracker.Clear();
        Assert.That((await _db.Problems.SingleAsync(p => p.Id == sample.Id)).YearCompetitionId, Is.EqualTo(oldYear));
    }
}
