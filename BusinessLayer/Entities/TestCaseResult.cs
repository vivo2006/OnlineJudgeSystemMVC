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
        [Key] //Primary key
        public int Id { get; set; }
        [Required(ErrorMessage = "Foreign key is required.")]
        public int SubmissionId { get; set; }
        [ForeignKey("SubmissionId")] //Foreign key to Submission
        public Submission Submission { get; set; }
        public string State { get; set; } //State of the test case
        public int Memory { get; set; } //Memory used by the test case
        public string? Output { get; set; } //Output of the test case
        public string? Time { get; set; } //Time used by the test case
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
