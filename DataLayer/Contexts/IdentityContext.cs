using BusinessLayer.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Contexts
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

        #region CRUD
        //Method for creating a new user
        public async Task<Tuple<IdentityResult, ApplicationUser>> CreateUserAsync(string username, string password, string email, string firstName, string lastName, Role role)
        {
            try
            {
                ApplicationUser user = new ApplicationUser(username, email, firstName, lastName);
                if (role == Role.Admin) //If the user is an admin, set the email as confirmed
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
            catch (Exception ex) //Handle exceptions and return an error result
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
        //Method for logging in a user
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
                    IdentityResult result = await userManager.PasswordValidators[0].ValidateAsync(userManager, userFromDb, password); //Validate the password

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
        //Method for reading a user by id
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
        //Method for reading all users
        public async Task<ICollection<ApplicationUser>> ReadAllUsersAsync(bool useNavigationalProporties = false)
        {
            try
            {
                IQueryable<ApplicationUser> query = context.Users.Where(u => u.UserName != "admin"); //Exclude admin user
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
        //Method for updating a user
        public async Task UpdateUserAsync(ApplicationUser newUser, bool useNavigationalProperties)
        {
            try
            {
                if (!string.IsNullOrEmpty(newUser.UserName))
                {
                    ApplicationUser user = await ReadUserAsync(newUser.Id);

                    if (user != null) //Update the user if it exists
                    {
                        user.FirstName = newUser.FirstName;
                        user.LastName = newUser.LastName;
                        user.Email = newUser.Email;
                        user.UserName = newUser.UserName;
                        if (useNavigationalProperties) user.Submissions = newUser.Submissions;
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
