using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Entities
{
    public class Submission
    {
        [Key] 
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")] 
        public ApplicationUser User { get; set; }
        public int Points { get; set; } 
        public DateTime Created { get; set; } 
        public string? Error { get; set; }
        public double Time { get; set; }
        public int Memory { get; set; }
        [MaxLength(30)]
        public string Status { get; set; } = "Queued";
        public int LanguageId { get; set; }
        public int ChallengeBonus { get; set; }
        [Required(ErrorMessage = "Foreign key is required.")]
        public int ProblemId { get; set; }
        [ForeignKey("ProblemId")] //Foreign key to Problem
        public Problem Problem { get; set; }
        public string Code { get; set; } //Code of the submission
        public ICollection<TestCaseResult> TestCaseResults { get; set; } //A submission can have many test case results (one-to-many relationship)
        public Submission()
        {
            TestCaseResults = new List<TestCaseResult>();
        }
        public Submission(int problemId, DateTime created, string userId)
        {
            ProblemId = problemId;
            Created = created;
            UserId = userId;
            Code = "";
        }
        public Submission(string userId, int points, string? error, double time, int memory, int problemId)
        {
            /*UserId = userId;*/
            Points = points;
            Error = error;
            Time = time;
            Memory = memory;
            ProblemId = problemId;
            TestCaseResults = new List<TestCaseResult>();
        }
    }
}
