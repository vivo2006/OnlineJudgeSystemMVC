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
        [Required(ErrorMessage = "Името е задължително.")] 
        [MaxLength(200, ErrorMessage = "Името не може да надхвърля 200 символа.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Времевото ограничение е задължително.")]
        [Range(0.1, 10, ErrorMessage ="Времевото ограничение трябва да приема стойности между 0.1 и 10 сек.")]
        public double TimeLimit {  get; set; }
        [Required(ErrorMessage = "Ограничението за паметта е задължително.")]
        [Range(1, 500, ErrorMessage = "Ограничението за паметта трябва да приема стойности между 1 и 500 МБ.")]
        public int MemoryLimit { get; set; }
        [Required(ErrorMessage = "URL е задължителен.")]
        [Url(ErrorMessage = "Невалиден URL формат.")]
        public string URL { get; set; }
        [Required(ErrorMessage ="Нотацията на задачата е задължителна.")]
        public string ProblemNotation { get; set; }
        public int CompetitionId {  get; set; }
        [ForeignKey("CompetitionId")]
        public GroupCompetition Competition { get; set; }
        public ICollection<Submission> Submissions {  get; set; } 
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
