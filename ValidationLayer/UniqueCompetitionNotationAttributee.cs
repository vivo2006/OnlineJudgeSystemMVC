using DataLayer;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OnlineJudgeSystemMVC.Validation
{
    public class UniqueCompetitionNotationAttribute : ValidationAttribute
    {
        private readonly OnlineJudgeDbContext context;
        public UniqueCompetitionNotationAttribute(OnlineJudgeDbContext context)
        {
            this.context = context;
        }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var existingTask = context.Competitions.Any(t => t.Notation == value.ToString());

            if (existingTask)
            {
                return new ValidationResult("The competition identifier must be unique.");
            }

            return ValidationResult.Success;
        }
    }
}
