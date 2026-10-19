using System.Security.Claims;
using BusinessLayer.API;
using BusinessLayer.Entities;
using DataLayer.Contexts;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OnlineJudgeSystemMVC.Controllers;
using OnlineJudgeSystemMVC.Models;
using OnlineJudgeSystemMVC.Services;
using ServiceLayer.EntityServices;
using ServiceLayer.ExternalServices;
using ServiceLayer.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;

namespace TestingLayer;

[TestFixture]
public class SecurityAndJudgingTests
{
    private SqliteConnection _connection = null!;
    private OnlineJudgeDbContext _db = null!;
    private string _root = null!;
    private Mock<IWebHostEnvironment> _environment = null!;
    private JudgingOptions _options = null!;
    private Submission _submission = null!;

    [SetUp]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _db = new OnlineJudgeDbContext(new DbContextOptionsBuilder<OnlineJudgeDbContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        var competition = new Competition { Name = "Demo", Notation = "DEMO" };
        var year = new YearCompetition { Year = 2026, Competition = competition };
        var group = new GroupCompetition { Group = "A", YearCompetition = year };
        var problem = new Problem { Name = "Sum", URL = "/Problems/demo.pdf", ProblemNotation = "A1", Competition = group, YearCompetition = year, TimeLimit = 1, MemoryLimit = 64 };
        var user = new ApplicationUser("demo", "demo@example.test", "Demo", "User");
        _submission = new Submission { User = user, Problem = problem, Code = "print(3)", LanguageId = 71, Created = DateTime.UtcNow };
        _db.Add(_submission);
        await _db.SaveChangesAsync();
        _root = Path.Combine(Path.GetTempPath(), "codexpert-tests-" + Guid.NewGuid());
        var directory = ProblemStorage.DirectoryFor(Path.Combine(_root, "TestCases"), "DEMO", 2026, "A1");
        Directory.CreateDirectory(Path.Combine(directory, "Input"));
        Directory.CreateDirectory(Path.Combine(directory, "Output"));
        File.WriteAllText(Path.Combine(directory, "Input", "sum.01.in"), "1 2");
        File.WriteAllText(Path.Combine(directory, "Output", "sum.01.out"), "3");
        _environment = new Mock<IWebHostEnvironment>();
        _environment.SetupGet(e => e.ContentRootPath).Returns(_root);
        _options = new JudgingOptions { ResultDelaySeconds = 0 };
    }

    [TearDown]
    public void Cleanup()
    {
        _db.Dispose();
        _connection.Dispose();
        // Only remove the unique test-owned temporary directory.
        if (_root.StartsWith(Path.Combine(Path.GetTempPath(), "codexpert-tests-"), StringComparison.Ordinal))
            Directory.Delete(_root, true);
    }

    private Mock<IJudge0Service> Judge(int status)
    {
        var judge = new Mock<IJudge0Service>(MockBehavior.Strict);
        judge.Setup(j => j.CreatePostQueryAsync(It.IsAny<string[]>(), It.IsAny<string[]>(), 71, "print(3)", 1, 65536))
            .ReturnsAsync("request");
        judge.Setup(j => j.SubmitBatchAsync("request")).ReturnsAsync(new List<PostResponse> { new() { Token = "fake-token" } });
        judge.Setup(j => j.GetBatchResultsAsync(It.IsAny<List<PostResponse>>())).ReturnsAsync(new BatchSubmissionResponse
        { Submissions = new List<Judge0Result> { new() { Status = new Status { id = status, description = status == 3 ? "Accepted" : "Other" }, Time = "0.1", Memory = 1024, Stdout = "3" } } });
        return judge;
    }

    private SubmissionProcessor Processor(IJudge0Service judge) => new(_db, judge, _environment.Object,
        Options.Create(_options), NullLogger<SubmissionProcessor>.Instance);

    [TestCase(3, "Completed", 100)]
    [TestCase(4, "Completed", 0)]
    [TestCase(5, "Completed", 0)]
    [TestCase(6, "Completed", 0)]
    [TestCase(1, "Incomplete", 0)]
    [TestCase(2, "Incomplete", 0)]
    public async Task JudgeUsesOnePostAndOneGetAndOnlyScoresTerminalResults(int status, string state, int points)
    {
        var judge = Judge(status);
        await Processor(judge.Object).ProcessAsync(_submission.Id);
        Assert.That(_submission.Status, Is.EqualTo(state));
        Assert.That(_submission.Points, Is.EqualTo(points));
        judge.Verify(j => j.SubmitBatchAsync(It.IsAny<string>()), Times.Once);
        judge.Verify(j => j.GetBatchResultsAsync(It.IsAny<List<PostResponse>>()), Times.Once);
        if (state == "Incomplete") Assert.That(_submission.TestCaseResults, Is.Empty);
        // Re-delivering the same job must never submit it to the provider a second time.
        await Processor(judge.Object).ProcessAsync(_submission.Id);
        judge.Verify(j => j.SubmitBatchAsync(It.IsAny<string>()), Times.Once);
        Assert.That((await _db.Users.SingleAsync()).Points[0], Is.EqualTo(points));
    }

    [Test]
    public async Task ProviderFailureDoesNotRetryOrAwardPoints()
    {
        var judge = Judge(3);
        judge.Setup(j => j.SubmitBatchAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException("provider unavailable"));
        await Processor(judge.Object).ProcessAsync(_submission.Id);
        await Processor(judge.Object).ProcessAsync(_submission.Id);
        var stored = await _db.Submissions.FindAsync(_submission.Id);
        Assert.That(stored!.Status, Is.EqualTo("Failed"));
        Assert.That(stored.Points, Is.Zero);
        judge.Verify(j => j.SubmitBatchAsync(It.IsAny<string>()), Times.Once);
        judge.Verify(j => j.GetBatchResultsAsync(It.IsAny<List<PostResponse>>()), Times.Never);
    }

    [Test]
    public async Task BestScoreDoesNotIncreaseForRepeatedAcceptedSolutions()
    {
        _submission.Status = "Completed";
        _submission.Points = 100;
        await _db.SaveChangesAsync();
        var second = new Submission(_submission.ProblemId, DateTime.UtcNow, _submission.UserId)
        { Code = "print(3)", LanguageId = 71 };
        _db.Add(second);
        await _db.SaveChangesAsync();
        await Processor(Judge(3).Object).ProcessAsync(second.Id);
        Assert.That((await _db.Users.SingleAsync()).Points[0], Is.EqualTo(100));
    }

    private SubmissionsController Controller(string userId)
    {
        var controller = new SubmissionsController(_db, Mock.Of<IBackgroundJobClient>(), Options.Create(_options),
            _environment.Object, NullLogger<SubmissionsController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, "User") }, "test")) } };
        return controller;
    }

    [Test]
    public async Task OtherUsersCannotReadCodeOrTestOutput()
    {
        var result = new TestCaseResult(_submission.Id, "Accepted", 1, "private", "0.1");
        _db.Add(result);
        await _db.SaveChangesAsync();
        Assert.That(await Controller("another-user").Details(_submission.Id), Is.TypeOf<NotFoundResult>());
        Assert.That(await Controller("another-user").OutputDetails(result.Id), Is.TypeOf<NotFoundResult>());
        Assert.That(await Controller(_submission.UserId).Details(_submission.Id), Is.TypeOf<ViewResult>());
        Assert.That(await Controller(_submission.UserId).OutputDetails(int.MaxValue), Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task UnsupportedLanguagesAndSubmissionCooldownRejectBeforeEnqueue()
    {
        var controller = Controller(_submission.UserId);
        Assert.That(await controller.Create(new CreateSubmissionRequest { ProblemId = _submission.ProblemId, Code = "code", LanguageId = 999 }), Is.TypeOf<BadRequestObjectResult>());
        var limited = await controller.Create(new CreateSubmissionRequest { ProblemId = _submission.ProblemId, Code = "code", LanguageId = 71 });
        Assert.That(((ObjectResult)limited).StatusCode, Is.EqualTo(429));
        Assert.That(await _db.Submissions.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void InternalControllerHelpersAreNotPublicActions()
    {
        Assert.That(typeof(SubmissionsController).GetMethod("ProcessSubmission"), Is.Null);
        Assert.That(typeof(SubmissionsController).GetMethod("GetTestFiles"), Is.Null);
        Assert.That(typeof(ProblemsController).GetMethod("UpdateChallenge"), Is.Null);
        Assert.That(typeof(ProblemsController).GetMethod("UploadFolder"), Is.Null);
        foreach (var type in new[] {
            typeof(OnlineJudgeSystemMVC.Areas.Identity.Pages.Admin.AdminIndex.AdminIndexModel),
            typeof(OnlineJudgeSystemMVC.Areas.Identity.Pages.Admin.DeleteUser.DeleteUserModel) })
            Assert.That(type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Any(a => a.Roles == "Admin"), Is.True);
    }

    [Test]
    public async Task HttpAuthorizationAndAntiforgeryRejectUnauthorizedMutations()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(SubmissionsController).Assembly.GetName().Name,
            ContentRootPath = _root,
            EnvironmentName = "Testing"
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        builder.Services.AddRazorPages();
        builder.Services.AddAuthentication("RegressionTest").AddScheme<AuthenticationSchemeOptions, RegressionAuthenticationHandler>("RegressionTest", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(_db);
        builder.Services.AddSingleton(Mock.Of<IBackgroundJobClient>());
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var http = new HttpClient { BaseAddress = new Uri(address) };
        var adminRoutes = new[] { "/Identity/Admin/AdminIndex", "/Identity/Admin/DeleteUser?id=demo" };
        foreach (var route in adminRoutes)
            Assert.That((int)(await http.GetAsync(route)).StatusCode, Is.EqualTo(401));
        http.DefaultRequestHeaders.Add("X-Regression-Role", "User");
        foreach (var route in adminRoutes)
            Assert.That((int)(await http.GetAsync(route)).StatusCode, Is.EqualTo(403));
        Assert.That((int)(await http.GetAsync("/Submissions/ProcessSubmission")).StatusCode, Is.EqualTo(404));
        Assert.That((int)(await http.GetAsync("/Problems/UpdateChallenge")).StatusCode, Is.EqualTo(404));
        Assert.That((int)(await http.PostAsync("/Submissions/Create", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode, Is.EqualTo(400));
        http.DefaultRequestHeaders.Remove("X-Regression-Role");
        http.DefaultRequestHeaders.Add("X-Regression-Role", "Admin");
        Assert.That((int)(await http.PostAsync("/TestCases/UploadFolder", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode, Is.EqualTo(400));
        Assert.That(await _db.Submissions.CountAsync(), Is.EqualTo(1));
        await app.StopAsync();
    }

    [TestCase("../outside")]
    [TestCase("C:\\outside")]
    [TestCase("..")]
    [TestCase("a/b")]
    public void StorageRejectsTraversal(string segment) => Assert.Throws<ArgumentException>(() => ProblemStorage.Segment(segment));

    [Test]
    public void PairsMatchByNameAndNeverSilentlyTruncate()
    {
        var directory = ProblemStorage.DirectoryFor(Path.Combine(_root, "TestCases"), "DEMO", 2026, "A1");
        File.WriteAllText(Path.Combine(directory, "Input", "sum.00.in"), "2 2");
        File.WriteAllText(Path.Combine(directory, "Output", "sum.00.out"), "4");
        var pairs = ProblemStorage.TestPairs(directory, 20);
        Assert.That(pairs.Inputs.Select(Path.GetFileNameWithoutExtension), Is.EqualTo(pairs.Outputs.Select(Path.GetFileNameWithoutExtension)));
        Assert.Throws<InvalidOperationException>(() => ProblemStorage.TestPairs(directory, 1));
        File.Delete(Path.Combine(directory, "Output", "sum.00.out"));
        Assert.Throws<InvalidOperationException>(() => ProblemStorage.TestPairs(directory, 20));
    }

    [Test]
    public void UploadsRejectPathsAndMismatchedPairs()
    {
        IFormFile File(string name) => new FormFile(new MemoryStream(new byte[] { 1 }), 0, 1, "files", name);
        Assert.Throws<ArgumentException>(() => TestCaseUploads.Validate(new[] { File("../x.in"), File("x.out") }, 20));
        Assert.Throws<ArgumentException>(() => TestCaseUploads.Validate(new[] { File("x.in"), File("y.out") }, 20));
        Assert.Throws<ArgumentException>(() => TestCaseUploads.Validate(new[] { File("x.in"), File("x.out"), File("x.sol") }, 20));
        Assert.DoesNotThrow(() => TestCaseUploads.Validate(new[] { File("x.in"), File("x.out") }, 20));
    }

    [Test]
    public async Task InvalidReplacementPreservesExistingTestFiles()
    {
        var directory = ProblemStorage.DirectoryFor(Path.Combine(_root, "TestCases"), "DEMO", 2026, "A1");
        var invalid = new FormFile(new MemoryStream(new byte[] { 1 }), 0, 1, "files", "unpaired.in");
        Assert.ThrowsAsync<ArgumentException>(() => TestCaseUploads.SaveAsync(directory, new[] { invalid }, 20));
        Assert.That(System.IO.File.ReadAllText(Path.Combine(directory, "Input", "sum.01.in")), Is.EqualTo("1 2"));
        IFormFile File(string name) => new FormFile(new MemoryStream(new byte[] { 49 }), 0, 1, "files", name);
        await TestCaseUploads.SaveAsync(directory, new[] { File("new.IN"), File("new.out") }, 20);
        var pairs = ProblemStorage.TestPairs(directory, 20);
        Assert.That(pairs.Inputs.Length, Is.EqualTo(1));
        Assert.That(Path.GetFileName(pairs.Inputs[0]), Is.EqualTo("new.IN"));
        Assert.That(System.IO.File.Exists(Path.Combine(directory, "Input", "sum.01.in")), Is.False);
    }

    [Test]
    public async Task ProblemIdentifiersCanRepeatAcrossYearsButNotWithinOneYear()
    {
        var current = _submission.Problem;
        var year = new YearCompetition { Year = 2027, Competition = current.YearCompetition.Competition };
        var group = new GroupCompetition { Group = "A", YearCompetition = year };
        _db.Problems.Add(new Problem { Name = "Another year", URL = "/demo/sum.html", ProblemNotation = current.ProblemNotation,
            Competition = group, YearCompetition = year });
        await _db.SaveChangesAsync();
        _db.Problems.Add(new Problem { Name = "Duplicate", URL = "/demo/sum.html", ProblemNotation = current.ProblemNotation,
            Competition = current.Competition, YearCompetition = current.YearCompetition });
        Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Test]
    public void ChallengeBonusUsesBestSuccessfulResultAndOnlyOncePerDay()
    {
        var user = _submission.User;
        user.LastChallengeTaskId = _submission.ProblemId;
        user.LastChallengeDate = _submission.Created;
        _submission.Points = 100;
        _submission.Time = 2;
        _submission.Memory = 20;
        var best = new Submission { Id = 999, ProblemId = _submission.ProblemId, Status = "Completed", Points = 100, Time = 1, Memory = 10, Created = DateTime.UtcNow.AddDays(-1) };
        Assert.That(SubmissionScoring.EarnsChallengeBonus(_submission, user, new[] { best }), Is.False);
        _submission.Time = 0.5;
        Assert.That(SubmissionScoring.EarnsChallengeBonus(_submission, user, new[] { best }), Is.True);
        best.ChallengeBonus = 10;
        best.Created = _submission.Created;
        Assert.That(SubmissionScoring.EarnsChallengeBonus(_submission, user, new[] { best }), Is.False);
    }

    [Test]
    public void SqlServerMigrationCanGenerateUpgradeScriptWithoutConnecting()
    {
        using var sql = new OnlineJudgeDbContext(new DbContextOptionsBuilder<OnlineJudgeDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=unused;Trusted_Connection=True").Options);
        var script = sql.GetService<IMigrator>().GenerateScript("20250220204514_Initial", "20261019000000_SecurityAndJudging");
        Assert.That(script, Does.Contain("[Status]"));
        Assert.That(script, Does.Contain("IX_Submissions_UserId_Created"));
        Assert.That(sql.Database.HasPendingModelChanges(), Is.False, "Migration snapshot must match the runtime model.");
    }
}

public sealed class RegressionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public RegressionAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers["X-Regression-Role"].ToString();
        if (string.IsNullOrEmpty(role)) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-user"), new Claim(ClaimTypes.Role, role) }, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
