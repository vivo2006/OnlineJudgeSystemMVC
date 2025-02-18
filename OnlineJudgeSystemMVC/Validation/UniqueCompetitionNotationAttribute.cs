using DataLayer;
using ServiceLayer;
using System.ComponentModel.DataAnnotations;

namespace OnlineJudgeSystemMVC.Validation
{
    public class UniqueCompetitionNotationAttribute : ValidationAttribute
    {
        private readonly OnlineJudgeDbContext _context;
        public UniqueCompetitionNotationAttribute(OnlineJudgeDbContext context)
        {
            _context = context;
        }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var existingTask = _context.Problems.Any(t => t.ProblemNotation == value.ToString());

            if (existingTask)
            {
                return new ValidationResult("Нотацията на състезание трябва да е уникална.");
            }

            return ValidationResult.Success;
        }
    }
}
