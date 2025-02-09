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
        [MaxLength(50, ErrorMessage = "Password cannot be more than 50 symbols.")]
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? City { get; set; }
        public ICollection<Submission> Submissions { get; set; }
        public ApplicationUser()
        {
            Submissions = new List<Submission>();
        }
        public ApplicationUser(string name, int age, string city, List<Submission> submissions)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Age = age;
            City = city;
            Submissions = submissions;
        }
    }
}
