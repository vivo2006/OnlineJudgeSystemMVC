using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Entities
{
    public class Problem
    {
        [Key] 
        public int Id { get; set; }
        [Required(ErrorMessage = "A name is required.")]
        [MaxLength(200, ErrorMessage = "The name cannot exceed 200 characters.")]
        public string Name { get; set; } 
        [Required(ErrorMessage = "The time limit is required.")]
        [Range(0.1, 10, ErrorMessage = "The time limit must be between 0.1 and 10 seconds.")]
        public double TimeLimit { get; set; } 
        [Required(ErrorMessage = "The memory limit is required.")]
        [Range(1, 500, ErrorMessage = "The memory limit must be between 1 and 500 MB.")]
        public int MemoryLimit { get; set; } 
        [Required(ErrorMessage = "A statement URL is required.")]
        [RegularExpression(@"^/Problems/[^?#]+\.pdf$|^/demo/[a-z-]+\.html$", ErrorMessage = "Use a local statement URL.")]
        public string URL { get; set; } 
        [Required(ErrorMessage = "The problem identifier is required.")]
        [RegularExpression(@"^[\p{L}\p{Nd}_-]+$", ErrorMessage = "Use letters, numbers, hyphens or underscores for identifiers.")]
        public string ProblemNotation { get; set; } 
        public int CompetitionId { get; set; }
        public int YearCompetitionId { get; set; }
        public YearCompetition YearCompetition { get; set; } = null!;
        [ForeignKey("CompetitionId")] //Foreign key
        public GroupCompetition Competition { get; set; }
        public ICollection<Submission> Submissions { get; set; } //A problem can have many submissions (one-to-many relationship)
        public Problem()
        {
            Submissions = new List<Submission>();
        }
        public Problem(string name, string uRL, int competitionId, List<Submission> submissions, double timeLimit, int memoryLimit)
        {
            Name = name;
            URL = uRL;
            CompetitionId = competitionId;
            Submissions = submissions;
            TimeLimit = timeLimit;
            MemoryLimit = memoryLimit;
        }
        public Problem(string name, double timeLimit, int memoryLimit, string problemNotation, int competitionId)
        {
            Name = name;
            TimeLimit = timeLimit;
            MemoryLimit = memoryLimit;
            ProblemNotation = problemNotation;
            CompetitionId = competitionId;
        }
    }
}
