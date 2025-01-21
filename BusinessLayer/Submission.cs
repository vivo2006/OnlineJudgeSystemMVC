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
        [Required(ErrorMessage ="Foreign key is required.")]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public int? Points { get; set; }
        public DateTime Created { get; set; }
        public string? Error { get; set; }
        [Required(ErrorMessage = "Time is required.")]
        public DateTime Time { get; set; }
        [Required(ErrorMessage = "Memory is required.")]
        public int Memory { get; set; }
        [Required(ErrorMessage = "Foreign key is required.")]
        public int ProblemId {  get; set; }
        [ForeignKey("ProblemId")]
        public Problem Problem { get; set; }
        public Submission() 
        {

        }
        public Submission(int id, string userId, int points, string? error, DateTime time, int memory, int problemId)
        {
            Id = id;
            UserId = userId;
            Points = points;
            Error = error;
            Time = time;
            Memory = memory;
            ProblemId = problemId;
        }
    }
}
