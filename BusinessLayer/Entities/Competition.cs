using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Entities
{
    public class Competition
    {
        [Key] 
        public int Id { get; set; }
        [Required(ErrorMessage = "A name is required.")]
        [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
        public string Name { get; set; } 
        public ICollection<YearCompetition>? YearCompetitions { get; set; } //A competition can have many years (one-to-many relationship)
        [Required(ErrorMessage = "The first year is required.")]
        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
        public int FirstYear { get; set; } 
        [Required(ErrorMessage = "The last year is required.")]
        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
        public int LastYear { get; set; } 
        [Required(ErrorMessage = "The competition identifier is required.")]
        [RegularExpression(@"^[\p{L}\p{Nd}_-]+$", ErrorMessage = "Use letters, numbers, hyphens or underscores for identifiers.")]
        public string Notation { get; set; } 
        public Competition()
        {
            YearCompetitions = new List<YearCompetition>();
        }
        public Competition(int id, string name, ICollection<YearCompetition> yearCompetitions, int firstYear, int lastYear, string notation)
        {
            Id = id;
            Name = name;
            YearCompetitions = yearCompetitions;
            FirstYear = firstYear;
            LastYear = lastYear;
            Notation = notation;
        }
    }
}
