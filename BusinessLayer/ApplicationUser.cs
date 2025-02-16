using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public ICollection<Submission> Submissions { get; set; }
        public int Points { get; set; }
        public ApplicationUser()
        {
            Submissions = new List<Submission>();
        }
        public ApplicationUser(string username, string email, string firstName, string lastName)
        {
            Id = Guid.NewGuid().ToString();
            UserName = username;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            Submissions = new List<Submission>();
        }
    }
}
