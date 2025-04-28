using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required] //Data validation using DataAnnotations
        [MaxLength(25, ErrorMessage = "Името не може да е повече от 25 символа.")] 
        public string FirstName { get; set; } //First name of the user
        [Required]
        [MaxLength(25, ErrorMessage = "Фамилията не може да е повече от 25 символа.")]
        public string LastName { get; set; } //Last name of the user
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>(); //A user can have many submissions (one-to-many relationship)
        public int? LastChallengeTaskId { get; set; } 
        public DateTime? LastChallengeDate { get; set; } 
        public double? BestTime { get; set; } 
        public int? BestMemory { get; set; } 
        public int[] Points { get; set; } = new int[] { 0, 0, 0, 0, 0 }; //The points of the user for each competition group
        public ApplicationUser() { } //Constructor without parameters for Entity Framework
        public ApplicationUser(string username, string email, string firstName, string lastName)
        {
            Id = Guid.NewGuid().ToString();
            UserName = username;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
        } //Constructor with parameters
    }
}
