using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class Competition
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Name is required.")] 
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }
        public ICollection<YearCompetition>? YearCompetitions { get; set; }
        [Required]
        public int FirstYear { get; set; }
        [Required]
        public int LastYear {  get; set; }
        [Required]
        public string Notation {  get; set; }
        public Competition()
        {
            YearCompetitions = new List<YearCompetition>();
        }
        public Competition(int id, string name, ICollection<YearCompetition> yearCompetitions, int firstYear, int lastYear)
        {
            Id = id;
            Name = name;
            YearCompetitions = yearCompetitions;
            FirstYear = firstYear;
            LastYear = lastYear;
        }
    }
}
