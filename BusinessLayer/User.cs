using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class User : IdentityUser
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(50, ErrorMessage = "Password cannot be more than 50 symbols.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Age is required.")]
        public int Age { get; set; }
        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; }
        public int? Points { get; set; }

        public ICollection<Submission> Submissions { get; set; }
        public User()
        {
            Submissions = new List<Submission>();
        }
        public User(string name, int age, string city, int points, List<Submission> submissions)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Age = age;
            City = city;
            Points = points;
            Submissions = submissions;
        }
    }
}
