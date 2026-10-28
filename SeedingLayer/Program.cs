using BusinessLayer.Entities;
using DataLayer.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EntityServices;

var appDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../OnlineJudgeSystemMVC"));
var configuration = new ConfigurationBuilder().SetBasePath(appDirectory)
    .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables().Build();
var services = new ServiceCollection();
services.AddLogging();
services.AddDbContext<OnlineJudgeDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
services.AddIdentityCore<ApplicationUser>(options => { options.Password.RequiredLength = 12; options.User.RequireUniqueEmail = true; })
    .AddRoles<IdentityRole>().AddEntityFrameworkStores<OnlineJudgeDbContext>();
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<OnlineJudgeDbContext>();
// Migrations are applied explicitly by the operator, never automatically to an existing database.
if ((await db.Database.GetPendingMigrationsAsync()).Any())
    throw new InvalidOperationException("Apply database migrations before running the seeder.");
var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
foreach (var role in new[] { "Admin", "User" })
{
    if (!await roles.RoleExistsAsync(role))
    {
        var result = await roles.CreateAsync(new IdentityRole(role));
        if (!result.Succeeded) throw new InvalidOperationException("Could not create role: " + role);
    }
}
var username = configuration["SeedAdmin:Username"];
var email = configuration["SeedAdmin:Email"];
var password = configuration["SeedAdmin:Password"];
if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
{
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var admin = await users.FindByNameAsync(username);
    if (admin == null)
    {
        admin = new ApplicationUser(username, email, "Demo", "Administrator") { EmailConfirmed = true };
        var created = await users.CreateAsync(admin, password);
        if (!created.Succeeded) throw new InvalidOperationException("Admin creation failed: " + string.Join(", ", created.Errors.Select(e => e.Code)));
        var assigned = await users.AddToRoleAsync(admin, "Admin");
        if (!assigned.Succeeded) throw new InvalidOperationException("Could not assign administrator role.");
    }
    else if (!await users.IsInRoleAsync(admin, "Admin"))
        throw new InvalidOperationException("The configured username already belongs to a non-admin account. Refusing to elevate it.");
}
else Console.WriteLine("Roles created. Set SeedAdmin__Username, SeedAdmin__Email and SeedAdmin__Password to create an admin.");

if (configuration.GetValue<bool>("SeedDemoData"))
{
    var placement = await DemoProblemSetup.EnsureAsync(db, appDirectory, createIfMissing: true);
    Console.WriteLine($"Sample problem placed in {placement!.CompetitionNotation}, {placement.Year}.");
}

// Repair historical repeat-submission score inflation from stored best results.
foreach (var user in await db.Users.Include(u => u.Submissions).ThenInclude(s => s.Problem).ToListAsync())
    user.Points = SubmissionScoring.Calculate(user.Submissions);
await db.SaveChangesAsync();
Console.WriteLine("Seeding completed; leaderboard totals rebuilt from best per-problem scores.");
