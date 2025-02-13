using BusinessLayer;
using DataLayer;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer
{
    public class IdentityManager
    {
        private readonly IdentityContext _context;

        public IdentityManager(IdentityContext context)
        {
            _context = context;
        }

        public async Task<Tuple<IdentityResult, ApplicationUser>> CreateUserAsync(string username, string password, string email, string firstName, string lastName, Role role)
        {
            return await _context.CreateUserAsync(username, password, email, firstName, lastName, role);
        }

        public async Task<ApplicationUser> LogInUserAsync(string username, string password)
        {
            return await _context.LogInUserAsync(username, password);
        }

        public async Task<ApplicationUser> ReadUserAsync(string key, bool useNavigationalProperties = false)
        {
            return await _context.ReadUserAsync(key, useNavigationalProperties);
        }

        public async Task<IEnumerable<ApplicationUser>> ReadAllUsersAsync(bool useNavigationalProperties = false)
        {
            return await _context.ReadAllUsersAsync(useNavigationalProperties);
        }

        public async Task UpdateUserAsync(ApplicationUser newUser, bool useNavigationalProperties = false)
        {
            await _context.UpdateUserAsync(newUser, useNavigationalProperties);
        }

        public async Task DeleteUserByNameAsync(string username)
        {
            await _context.DeleteUserByNameAsync(username);
        }

        public async Task<ApplicationUser> ReadUserByNameAsync(string username)
        {
            return await _context.ReadUserByNameAsync(username);
        }
    }
}
