using BusinessLayer;
using DataLayer;
using Microsoft.EntityFrameworkCore;
using ServiceLayer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("OnlineJudgeDbContextConnection") ?? throw new InvalidOperationException("Connection string 'OnlineJudgeDbContextConnection' not found.");
//var connectionString = builder.Configuration.GetConnectionString("OnlineJudgeDbContextConnection") ?? throw new InvalidOperationException("Connection string 'OnlineJudgeDbContextConnection' not found.");

builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<OnlineJudgeDbContext>(options => options.UseSqlServer(Connection.conString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<OnlineJudgeDbContext>();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedAccount = true; 
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

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Persistent cookie expiration
    options.LoginPath = "/Identity/Account/Login"; // Login path
    options.LogoutPath = "/Identity/Account/Logout"; // Logout path
    options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Access denied path
    options.SlidingExpiration = true; // Reset the expiration time on each request
});


builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50 MB
});



var app = builder.Build();



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

app.UseAuthorization();
//app.UseAuthentication();

app.UseHttpsRedirection();


app.MapControllers();
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


