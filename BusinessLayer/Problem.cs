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
        [Required]
        public double TimeLimit {  get; set; }
        [Required]
        public int MemoryLimit { get; set; }
        [Required(ErrorMessage = "URL е задължителен.")]
        [Url(ErrorMessage = "Невалиден URL формат.")]
        public string URL { get; set; }
        [Required]
        public string ProblemNotation { get; set; }
        public int CompetitionId {  get; set; }
        [ForeignKey("CompetitionId")]
        public GroupCompetition Competition { get; set; }
        public ICollection<Submission> Submissions {  get; set; } 
        public Problem() 
        {
            Submissions = new List<Submission>();
        }
        public Problem(int id, string name, string uRL, int competitionId, List<Submission> submissions, double timeLimit, int memoryLimit)
        {
            Id = id;
            Name = name;
            URL = uRL;
            CompetitionId = competitionId;
            Submissions = submissions;
            TimeLimit = timeLimit;
            MemoryLimit = memoryLimit;
        }
    }
}
