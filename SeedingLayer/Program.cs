using BusinessLayer.Entities;
using DataLayer;
using DataLayer.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

namespace SeedingLayer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                IdentityOptions options = new IdentityOptions();
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;

                DbContextOptionsBuilder builder = new DbContextOptionsBuilder();
                builder.UseSqlServer(Connection.conString);

                OnlineJudgeDbContext dbContext = new OnlineJudgeDbContext(builder.Options);

                // Ensure database is migrated
                await dbContext.Database.MigrateAsync();

                // Create UserManager
                UserManager<ApplicationUser> userManager = new UserManager<ApplicationUser>(
                    new UserStore<ApplicationUser>(dbContext),
                    Options.Create(options),
                    new PasswordHasher<ApplicationUser>(),
                    new List<IUserValidator<ApplicationUser>>() { new UserValidator<ApplicationUser>() },
                    new List<IPasswordValidator<ApplicationUser>>() { new PasswordValidator<ApplicationUser>() },
                    new UpperInvariantLookupNormalizer(),
                    new IdentityErrorDescriber(),
                    new ServiceCollection().BuildServiceProvider(),
                    new Logger<UserManager<ApplicationUser>>(new LoggerFactory())
                );

                // Create RoleManager
                var roleStore = new RoleStore<IdentityRole>(dbContext);
                var roleManager = new RoleManager<IdentityRole>(
                    roleStore,
                    new List<IRoleValidator<IdentityRole>>() { new RoleValidator<IdentityRole>() },
                    new UpperInvariantLookupNormalizer(),
                    new IdentityErrorDescriber(),
                    new Logger<RoleManager<IdentityRole>>(new LoggerFactory())
                );

                // Add roles if they don't exist
                var adminRoleExists = await roleManager.RoleExistsAsync(Role.Admin.ToString());
                if (!adminRoleExists)
                {
                    await roleManager.CreateAsync(new IdentityRole(Role.Admin.ToString()));
                }

                var userRoleExists = await roleManager.RoleExistsAsync(Role.User.ToString());
                if (!userRoleExists)
                {
                    await roleManager.CreateAsync(new IdentityRole(Role.User.ToString()));
                }

                Console.WriteLine("Roles Added");

                // Verify roles
                var roles = await dbContext.Roles.ToListAsync();
                foreach (var role in roles)
                {
                    Console.WriteLine($"Role: {role.Name}");
                }

                // Create admin user
                IdentityContext identityContext = new IdentityContext(dbContext, userManager, roleManager);
                Tuple<IdentityResult, ApplicationUser> result = await identityContext.CreateUserAsync("admin", "Admin1234_", "vivo2006@abv.bg", "Велислав", "Борисов", Role.Admin);

                if (result.Item1.Succeeded)
                {
                    Console.WriteLine("Admin account added successfully!");
                }
                else
                {
                    Console.WriteLine("Admin account failed to be created!");
                    foreach (var error in result.Item1.Errors)
                    {
                        Console.WriteLine($"Error: {error.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
