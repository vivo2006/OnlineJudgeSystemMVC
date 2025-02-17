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
        [Required(ErrorMessage = "Името е задължително.")] 
        [MaxLength(100, ErrorMessage = "Името не може да надхвърля 100 символа.")]
        public string Name { get; set; }
        public ICollection<YearCompetition>? YearCompetitions { get; set; }
        [Required(ErrorMessage ="Началната година е задължителна")]
        [Range(2000, 2030, ErrorMessage ="Годината трябва да е между 2000 и 2030.")]
        public int FirstYear { get; set; }
        [Required(ErrorMessage = "Крайната година е задължителна")]
        [Range(2000, 2030, ErrorMessage = "Годината трябва да е между 2000 и 2030.")]
        public int LastYear {  get; set; }
        [Required(ErrorMessage ="Нотацията на състезанието е задължителна.")]
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
