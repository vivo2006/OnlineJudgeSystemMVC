using BusinessLayer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class IdentityContext
    {
        OnlineJudgeDbContext context;
        UserManager<ApplicationUser> userManager;
        RoleManager<IdentityRole> roleManager;

        public IdentityContext(OnlineJudgeDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        #region  Seeding Data with project

        public async Task SeedDataAsync(string adminEmail, string adminPassword)
        {
            int userRoles = await context.UserRoles.CountAsync();
            if (userRoles == 0)
            {
                await ConfigureAdminAccountAsync(adminEmail, adminPassword);
            }
        }

        public async Task ConfigureAdminAccountAsync(string email, string password)
        {
            ApplicationUser adminIdentityUser = await context.Users.FirstAsync();
            if (adminIdentityUser != null)
            {
                await userManager.AddToRoleAsync(adminIdentityUser, Role.Admin.ToString());
                await userManager.AddPasswordAsync(adminIdentityUser, password);
                await userManager.SetEmailAsync(adminIdentityUser, email);
            }
        }
        #endregion Seeding

        #region CRUD
        public async Task<Tuple<IdentityResult, ApplicationUser>> CreateUserAsync(string username, string password, string email, string firstName, string lastName, Role role)
        {
            try
            {
                ApplicationUser user = new ApplicationUser(username, email, firstName, lastName);
                if(role == Role.Admin)
                {
                    user.EmailConfirmed = true;
                }
                IdentityResult result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    return new Tuple<IdentityResult, ApplicationUser>(result, user);
                }

                // Assign role
                var addToRoleResult = await userManager.AddToRoleAsync(user, role.ToString());
                if (!addToRoleResult.Succeeded)
                {
                    return new Tuple<IdentityResult, ApplicationUser>(addToRoleResult, user);
                }

                return new Tuple<IdentityResult, ApplicationUser>(IdentityResult.Success, user);
            }
            catch (Exception ex)
            {
                IdentityError er = new IdentityError()
                {
                    Code = "Registration",
                    Description = ex.Message
                };
                IdentityResult result = IdentityResult.Failed(er);

                return new Tuple<IdentityResult, ApplicationUser>(result, null);
            }
        }

        public async Task<ApplicationUser> LogInUserAsync(string username, string password)
        {
            try
            {
                ApplicationUser userFromDb = await ReadUserByNameAsync(username);

                if (userFromDb == null)
                {
                    return null;
                }
                else
                {
                    IdentityResult result = await userManager.PasswordValidators[0].ValidateAsync(userManager, userFromDb, password);

                    if (result.Succeeded)
                    {
                        return await context.Users.FindAsync(userFromDb.Id);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ApplicationUser> ReadUserAsync(string key, bool useNavigationalProporties = false)
        {
            try
            {
                IQueryable<ApplicationUser> query = context.Users;
                if (useNavigationalProporties)
                {
                    query = query.Include(u => u.Submissions);
                }
                return await query.FirstOrDefaultAsync(u => u.Id == key);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ICollection<ApplicationUser>> ReadAllUsersAsync(bool useNavigationalProporties = false)
        {
            try
            {
                IQueryable<ApplicationUser> query = context.Users;
                if (useNavigationalProporties)
                {
                    query = query.Include(u => u.Submissions);
                }
                    return await query.ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task UpdateUserAsync(ApplicationUser newUser, bool useNavigationalProperties)
        {
            try
            {
                if (!String.IsNullOrEmpty(newUser.UserName))
                {
                    ApplicationUser user = await ReadUserAsync(newUser.Id);

                    if (user != null)
                    {
                        user.FirstName = newUser.FirstName;
                        user.LastName = newUser.LastName;
                        user.Email = newUser.Email;
                        user.UserName = newUser.UserName;
                        if(useNavigationalProperties) user.Submissions = newUser.Submissions;
                        await userManager.UpdateAsync(user);
                    }
                    else
                    {
                        throw new InvalidOperationException("User not found for update!");
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task DeleteUserByNameAsync(string username)
        {
            try
            {
                ApplicationUser user = await ReadUserByNameAsync(username);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found for deletion!");
                }
                await userManager.DeleteAsync(user);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ApplicationUser> ReadUserByNameAsync(string username)
        {
            try
            {
                return await userManager.FindByNameAsync(username);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        #endregion Crud
    }
}
