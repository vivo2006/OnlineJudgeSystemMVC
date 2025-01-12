using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class Problem
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Name is required.")] 
        [MaxLength(200, ErrorMessage = "Problem name cannot exceed 200 characters.")]
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        [Required(ErrorMessage = "URL is required.")]
        [Url(ErrorMessage = "Invalid URL format.")]
        public string URL { get; set; }
        [Required(ErrorMessage = "Foreign key is required.")]
        public int CompetitionId {  get; set; }
        [ForeignKey("CompetitionId")]
        public Competition Competition { get; set; }
        public ICollection<Submission> Submissions {  get; set; } 
        public Problem() 
        {
            Submissions = new List<Submission>();
        }
        public Problem(int id, string name, DateTime created, DateTime lastModified, string uRL, int competitionId, List<Submission> submissions)
        {
            Id = id;
            Name = name;
            Created = created;
            LastModified = lastModified;
            URL = uRL;
            CompetitionId = competitionId;
            Submissions = submissions;
        }
    }
}
