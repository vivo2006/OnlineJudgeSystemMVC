using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using BusinessLayer.Entities;
using DataLayer.Contexts;
using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OnlineJudgeSystemMVC.Controllers;
using OnlineJudgeSystemMVC.Presentation;
using ServiceLayer.EntityServices;
using ServiceLayer.ErrorDescriber;

namespace TestingLayer;

[TestFixture]
public class EnglishContentTests
{
    private const string Archive = "/Problems/\u041d\u0415\u0422\u0418/2011/E3.pdf";

    [TestCase("/Problems/%D0%9D%D0%95%D0%A2%D0%98/2011/E3.pdf")]
    [TestCase("https://localhost:7197/Problems/%D0%9D%D0%95%D0%A2%D0%98/2011/E3.pdf?download=1")]
    [TestCase("/Problems/\u041d\u0415\u0422\u0418/2011/E3.pdf#page=1")]
    public void LegacyArchiveLinksOpenEnglishAndKeepStoredContent(string source)
    {
        var problem = new Problem { Name = "\u041f\u043e\u0434\u0430\u0440\u044a\u043a", URL = source };
        Assert.That(EnglishContent.StatementUrl(source), Is.EqualTo("/Problems/english/neti-2011-e3.html"));
        Assert.That(EnglishContent.ProblemName(problem), Is.EqualTo("Gift"));
        Assert.That(problem.URL, Is.EqualTo(source));
        Assert.That(problem.Name, Is.EqualTo("\u041f\u043e\u0434\u0430\u0440\u044a\u043a"));
    }

    [Test]
    public void UnknownUploadedContentIsNotRewritten()
    {
        var problem = new Problem { Name = "My original problem", URL = "/Problems/CUSTOM/2026/A1.pdf" };
        Assert.That(EnglishContent.StatementUrl(problem.URL), Is.EqualTo(problem.URL));
        Assert.That(EnglishContent.ProblemName(problem), Is.EqualTo(problem.Name));
        Assert.That(EnglishContent.CompetitionCode("DEMO"), Is.EqualTo("DEMO"));
    }

    [TestCase("\u041f\u0440\u043e\u043b\u0435\u0442\u0435\u043d \u0442\u0443\u0440\u043d\u0438\u0440 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430", "Spring Informatics Tournament")]
    [TestCase("\u041b\u0435\u0442\u0435\u043d \u0442\u0443\u0440\u043d\u0438\u0440 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430", "Summer Informatics Tournament")]
    [TestCase("\u041d\u0430\u0446\u0438\u043e\u043d\u0430\u043b\u043d\u0430 \u043e\u043b\u0438\u043c\u043f\u0438\u0430\u0434\u0430 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430 - \u043e\u0431\u043b\u0430\u0441\u0442\u0435\u043d \u043a\u0440\u044a\u0433", "National Olympiad in Informatics - Regional Round")]
    [TestCase("\u041d\u0430\u0446\u0438\u043e\u043d\u0430\u043b\u043d\u0430 \u043e\u043b\u0438\u043c\u043f\u0438\u0430\u0434\u0430 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430 - \u043d\u0430\u0446\u0438\u043e\u043d\u0430\u043b\u0435\u043d \u043a\u0440\u044a\u0433", "National Olympiad in Informatics - National Round")]
    public void FullTournamentNamesTranslateEvenWithCustomIdentifiers(string original, string expected)
    {
        var competition = new Competition { Name = original, Notation = "custom-code" };
        Assert.That(EnglishContent.CompetitionName(competition), Is.EqualTo(expected));
        Assert.That(EnglishContent.CompetitionCode(competition), Does.Not.Match("[\\p{IsCyrillic}]"));
        Assert.That(competition.Name, Is.EqualTo(original));
    }

    [Test]
    public void CompetitionLabelsAndPasswordErrorsAreEnglish()
    {
        var competition = new Competition { Name = "Original", Notation = "\u041d\u0415\u0422\u0418" };
        Assert.That(EnglishContent.CompetitionName(competition), Is.EqualTo("National Autumn Informatics Tournament"));
        Assert.That(EnglishContent.CompetitionCode(competition.Notation), Is.EqualTo("NETI"));
        var errors = new CustomIdentityErrorDescriber();
        foreach (var error in new[] { errors.PasswordTooShort(12), errors.PasswordRequiresDigit(), errors.PasswordRequiresNonAlphanumeric(), errors.PasswordRequiresLower(), errors.PasswordRequiresUpper(), errors.PasswordMismatch() })
        {
            Assert.That(error.Description, Does.Match("^[^\\p{IsCyrillic}]+$"));
            Assert.That(error.Description, Does.Contain("password").IgnoreCase);
        }
    }

    [Test]
    public void EveryBundledArchiveHasAnEnglishStatement()
    {
        var root = Path.Combine(FindWebRoot(), "wwwroot");
        foreach (var pdf in Directory.EnumerateFiles(Path.Combine(root, "Problems"), "*.pdf", SearchOption.AllDirectories))
        {
            var original = "/" + Path.GetRelativePath(root, pdf).Replace('\\', '/');
            var translated = EnglishContent.StatementUrl(original);
            Assert.That(translated, Does.StartWith("/Problems/english/"), original);
            var page = Path.Combine(root, translated.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(page), Is.True, page);
            Assert.That(File.ReadAllText(page), Does.Contain("<html lang=\"en\">"));
        }
    }

    [Test]
    public async Task AccountAdminAndArchivePagesRenderInEnglishWithoutExternalServices()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var webRoot = FindWebRoot();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(HomeController).Assembly.GetName().Name, ContentRootPath = webRoot, EnvironmentName = "Testing" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddDbContext<OnlineJudgeDbContext>(o => o.UseSqlite(connection));
        builder.Services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<OnlineJudgeDbContext>();
        builder.Services.AddAuthentication(o => { o.DefaultAuthenticateScheme = "EnglishTest"; o.DefaultChallengeScheme = "EnglishTest"; }).AddScheme<AuthenticationSchemeOptions, EnglishTestAuthenticationHandler>("EnglishTest", _ => { });
        builder.Services.AddControllersWithViews();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddRazorPages();
        builder.Services.AddSingleton(Mock.Of<IEmailSender>());
        builder.Services.AddSingleton(Mock.Of<IBackgroundJobClient>());
        builder.Services.AddScoped<IdentityContext>();
        builder.Services.AddScoped<IdentityManager>();
        builder.Services.AddScoped<SubmissionContext>();
        builder.Services.AddScoped<SubmissionsManager>();
        builder.Services.AddScoped<CompetitionContext>();
        builder.Services.AddScoped<IDb<Competition, int>, CompetitionContext>();
        builder.Services.AddScoped<CompetitionsManager>();
        builder.Services.AddScoped<YearCompetitionContext>();
        builder.Services.AddScoped<IDb<YearCompetition, int>, YearCompetitionContext>();
        builder.Services.AddScoped<YearCompetitionsManager>();
        builder.Services.AddScoped<GroupCompetitionContext>();
        builder.Services.AddScoped<IDb<GroupCompetition, int>, GroupCompetitionContext>();
        builder.Services.AddScoped<GroupCompetitionsManager>();
        builder.Services.AddScoped<ProblemContext>();
        builder.Services.AddScoped<IDb<Problem, int>, ProblemContext>();
        builder.Services.AddScoped<ProblemsManager>();
        await using var app = builder.Build();
        int competitionId, yearId, groupId, problemId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OnlineJudgeDbContext>();
            await db.Database.EnsureCreatedAsync();
            var user = new ApplicationUser("english-demo", "demo@example.test", "Demo", "User") { Id = "english-test-user", EmailConfirmed = true, NormalizedUserName = "ENGLISH-DEMO", NormalizedEmail = "DEMO@EXAMPLE.TEST" };
            user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, "SampleTestOnly123!");
            var competition = new Competition { Name = "\u041d\u0430\u0446\u0438\u043e\u043d\u0430\u043b\u0435\u043d \u0442\u0443\u0440\u043d\u0438\u0440", Notation = "\u041d\u0415\u0422\u0418", FirstYear = 2011, LastYear = 2011 };
            var year = new YearCompetition { Year = 2011, Competition = competition };
            var group = new GroupCompetition { Group = "E", YearCompetition = year };
            var problem = new Problem { Name = "\u041f\u043e\u0434\u0430\u0440\u044a\u043a", URL = Archive, ProblemNotation = "E3", Competition = group, YearCompetition = year, TimeLimit = 1, MemoryLimit = 64 };
            db.Add(user); db.Add(problem);
            db.Competitions.Add(new Competition { Name = "\u041f\u0440\u043e\u043b\u0435\u0442\u0435\u043d \u0442\u0443\u0440\u043d\u0438\u0440 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430", Notation = "CUSTOM-0", FirstYear = 2011, LastYear = 2011 });
            db.Competitions.Add(new Competition { Name = "\u041b\u0435\u0442\u0435\u043d \u0442\u0443\u0440\u043d\u0438\u0440 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430", Notation = "CUSTOM-1", FirstYear = 2011, LastYear = 2011 });
            db.Competitions.Add(new Competition { Name = "\u041d\u0430\u0446\u0438\u043e\u043d\u0430\u043b\u043d\u0430 \u043e\u043b\u0438\u043c\u043f\u0438\u0430\u0434\u0430 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430 - \u043e\u0431\u043b\u0430\u0441\u0442\u0435\u043d \u043a\u0440\u044a\u0433", Notation = "CUSTOM-2", FirstYear = 2011, LastYear = 2011 });
            db.Competitions.Add(new Competition { Name = "\u041d\u0430\u0446\u0438\u043e\u043d\u0430\u043b\u043d\u0430 \u043e\u043b\u0438\u043c\u043f\u0438\u0430\u0434\u0430 \u043f\u043e \u0438\u043d\u0444\u043e\u0440\u043c\u0430\u0442\u0438\u043a\u0430 - \u043d\u0430\u0446\u0438\u043e\u043d\u0430\u043b\u0435\u043d \u043a\u0440\u044a\u0433", Notation = "CUSTOM-3", FirstYear = 2011, LastYear = 2011 });
            await db.SaveChangesAsync();
            (competitionId, yearId, groupId, problemId) = (competition.Id, year.Id, group.Id, problem.Id);
        }
        app.UseDeveloperExceptionPage();
        app.UseRequestLocalization(new RequestLocalizationOptions().SetDefaultCulture("en").AddSupportedCultures("en").AddSupportedUICultures("en"));
        app.UseStaticFiles();
        app.UseAuthentication(); app.UseAuthorization();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}"); app.MapRazorPages();
        await app.StartAsync();
        using var http = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
        http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("bg-BG,bg;q=0.9");
        var pages = new Dictionary<string, string>
        {
            ["/"] = "Welcome to CodeXpert",
            ["/Identity/Account/Login"] = "Sign in",
            ["/Identity/Account/Register"] = "Register",
            ["/Identity/Account/ForgotPassword"] = "Forgot password",
            ["/Identity/Account/ForgotPasswordConfirmation"] = "Please check your inbox",
            ["/Identity/Account/ResetPassword?code=test"] = "Reset your password",
            ["/Identity/Account/ResetPasswordConfirmation"] = "Your password has been reset",
            ["/Identity/Account/AccessDenied"] = "Access denied",
            ["/Home/ChallengeMeInfo"] = "Daily challenge",
            ["/Identity/Account/Manage/ChangePassword"] = "Change password",
            ["/Identity/Account/Manage/PersonalData"] = "Personal data",
            ["/Identity/Account/Manage/DeletePersonalData"] = "Delete account",
            ["/Identity/Admin/AdminIndex"] = "Accounts",
            ["/Identity/Admin/DeleteUser?id=english-demo"] = "delete this account",
            ["/Competitions/AdministratorIndex"] = "National Autumn Informatics Tournament",
            [$"/GroupCompetitions/Index?competitionId={competitionId}&yearId={yearId}"] = "Gift",
            [$"/Problems/Details/{problemId}"] = "/Problems/english/neti-2011-e3.html",
            [$"/Problems/Create?id={groupId}"] = "Choose a PDF file",
            [$"/YearCompetitions/Create?id={competitionId}"] = "National Autumn Informatics Tournament",
            [$"/YearCompetitions/Edit/{yearId}"] = "National Autumn Informatics Tournament",
            ["/Problems/english/neti-2011-e3.html"] = "WKN387"
        };
        foreach (var (url, expected) in pages)
        {
            var response = await http.GetAsync(url);
            var body = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.That(response.IsSuccessStatusCode, Is.True, url + " returned " + response.StatusCode + "\n" + (body.Length > 6000 ? body[..6000] : body));
            Assert.That(body, Does.Contain(expected), url);
            if (url == "/Competitions/AdministratorIndex")
            {
                foreach (var title in new[] { "Spring Informatics Tournament", "Summer Informatics Tournament", "National Olympiad in Informatics - Regional Round", "National Olympiad in Informatics - National Round" })
                    Assert.That(body, Does.Contain(title));
            }
            if (url == "/")
            {
                Assert.That(body, Does.Contain(">Log out</button>"));
                Assert.That(body, Does.Contain("white-space:nowrap"));
            }
            Assert.That(body, Does.Not.Match("[\\p{IsCyrillic}]"), url + " contains untranslated displayed text");
        }
        await app.StopAsync();
    }

    private static string FindWebRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "OnlineJudgeSystemMVC");
            if (File.Exists(Path.Combine(candidate, "OnlineJudgeSystemMVC.csproj"))) return candidate;
        }
        throw new DirectoryNotFoundException("Could not find the web project for the rendering test.");
    }
}

public sealed class EnglishTestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public EnglishTestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "english-test-user"), new Claim(ClaimTypes.Role, "Admin") }, IdentityConstants.ApplicationScheme);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
