using DataLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using OnlineJudgeSystemMVC.Areas.Identity.Pages.Account;
using BusinessLayer.API;
using BusinessLayer.Email;
using ServiceLayer.EmailSender;
using ServiceLayer.ErrorDescriber;
using ServiceLayer.EntityServices;
using DataLayer.Contexts;
using BusinessLayer.Entities;
using Hangfire;
using Hangfire.SqlServer;
using ServiceLayer.ExternalServices;
using OnlineJudgeSystemMVC.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false).AddEnvironmentVariables();

builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddRazorPages(options => options.Conventions.AuthorizeAreaFolder("Identity", "/Admin", "Administrator"));
builder.Services.AddAuthorization(options => options.AddPolicy("Administrator", policy => policy.RequireRole("Admin")));
builder.Services.AddDbContext<OnlineJudgeDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddRoles<IdentityRole>().AddEntityFrameworkStores<OnlineJudgeDbContext>().AddErrorDescriber<CustomIdentityErrorDescriber>();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedAccount = true; 
    options.Password.RequiredLength = 12;
    options.User.RequireUniqueEmail = true;
});

builder.Services.AddScoped<ProblemsManager, ProblemsManager>();
builder.Services.AddScoped<IDb<Problem, int>, ProblemContext>();
builder.Services.AddScoped<ProblemContext, ProblemContext>();

builder.Services.AddScoped<CompetitionsManager, CompetitionsManager>();
builder.Services.AddScoped<IDb<Competition, int>, CompetitionContext>();
builder.Services.AddScoped<CompetitionContext, CompetitionContext>();


builder.Services.AddScoped<GroupCompetitionsManager, GroupCompetitionsManager>();
builder.Services.AddScoped<IDb<GroupCompetition, int>, GroupCompetitionContext>();
builder.Services.AddScoped<GroupCompetitionContext, GroupCompetitionContext>();

builder.Services.AddScoped<YearCompetitionsManager, YearCompetitionsManager>();
builder.Services.AddScoped<IDb<YearCompetition, int>, YearCompetitionContext>();
builder.Services.AddScoped<YearCompetitionContext, YearCompetitionContext>();

builder.Services.AddScoped<SubmissionsManager, SubmissionsManager>();
builder.Services.AddScoped<IDb<Submission, int>, SubmissionContext>();
builder.Services.AddScoped<SubmissionContext, SubmissionContext>();

builder.Services.AddScoped<TestCaseResultsManager, TestCaseResultsManager>();
builder.Services.AddScoped<IDb<TestCaseResult, int>, TestCaseResultContext>();
builder.Services.AddScoped<TestCaseResultContext, TestCaseResultContext>();

builder.Services.AddScoped<IdentityManager, IdentityManager>();
builder.Services.AddScoped<IdentityContext, IdentityContext>();

builder.Services.AddScoped<IJudge0Service, Judge0Service>();
builder.Services.AddScoped<SubmissionProcessor>();
builder.Services.AddOptions<JudgingOptions>().Bind(builder.Configuration.GetSection("Judging"))
    .Validate(o => o.ResultDelaySeconds >= 1 && o.ResultDelaySeconds <= 120 && o.MaxTestCases >= 1 && o.MaxTestCases <= 20 &&
        o.MaxSubmissionsPerDay >= 1 && o.MaxGlobalSubmissionsPerDay >= 1 && o.SubmissionCooldownSeconds >= 1, "Invalid judging quota settings.").ValidateOnStart();


builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<APIdata>(builder.Configuration.GetSection("APIdata"));

builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
          {
              CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
              SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
              QueuePollInterval = TimeSpan.FromSeconds(15),
              UseRecommendedIsolationLevel = true,
              UsePageLocksOnDequeue = true,
              DisableGlobalLocks = true 
          });
});

builder.Services.AddHangfireServer();

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.LoginPath = "/Identity/Account/Login"; // Login path
    options.LogoutPath = "/Identity/Account/Logout"; // Logout path
    options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Access denied path
    options.SlidingExpiration = true; // Reset the expiration time on each request
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.IsEssential = true; // Ensures cookie is used even in case of GDPR compliance
});


builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50 MB
});



var app = builder.Build();

// Upgrade the old local demo placement once the operator has applied the existing schema migrations.
// The same sample row is reused, so submission history and daily-challenge links stay valid.
if (app.Environment.IsDevelopment())
{
    await using var sampleScope = app.Services.CreateAsyncScope();
    var sampleDb = sampleScope.ServiceProvider.GetRequiredService<OnlineJudgeDbContext>();
    if (!(await sampleDb.Database.GetPendingMigrationsAsync()).Any())
        await DemoProblemSetup.EnsureAsync(sampleDb, app.Environment.ContentRootPath);
}

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures("en")
    .AddSupportedUICultures("en"));


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new[] { new AdminDashboardAuthorization() } });

app.UseHttpsRedirection();


app.MapControllers();
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


