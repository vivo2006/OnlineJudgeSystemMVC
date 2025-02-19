using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Entities
{
    public class TestCaseResult
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Foreign key is required.")]
        public int SubmissionId { get; set; }
        [ForeignKey("SubmissionId")]
        public Submission Submission { get; set; }
        public string State { get; set; }
        public int Memory { get; set; }
        public string? Output { get; set; }
        public string? Time { get; set; }
        public TestCaseResult()
        {

        }
        public TestCaseResult(int submissionId, string state, int memory, string output, string time)
        {
            SubmissionId = submissionId;
            State = state;
            Memory = memory;
            Output = output;
            Time = time;
        }
    }
}
