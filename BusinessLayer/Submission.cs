using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class Submission
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        public double? Points { get; set; }
        public DateTime Created { get; set; }
        public string? Error { get; set; }
        public double Time { get; set; }
        public int Memory { get; set; }
        [Required(ErrorMessage = "Foreign key is required.")]
        public int ProblemId {  get; set; }
        [ForeignKey("ProblemId")]
        public Problem Problem { get; set; }
        public string Code { get; set; }
        public ICollection<TestCaseResult> TestCaseResults { get; set; } 
        public Submission() 
        {
            TestCaseResults = new List<TestCaseResult>();
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
        public Submission(int problemId, DateTime created, string userId)
        {
            ProblemId = problemId;
            Created = created;
            UserId = userId;
            Code = "";
        }
    }
}
