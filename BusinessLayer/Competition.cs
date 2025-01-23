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
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        [Required]
        public string CompetitionAnnotation {  get; set; }
        public Competition()
        {

        }
        public Competition(int id, string name, ICollection<YearCompetition> yearCompetitions, int groupNumber, DateTime created, DateTime lastModified, int firstYear, int lastYear)
        {
            Id = id;
            Name = name;
            YearCompetitions = yearCompetitions;
            Created = created;
            LastModified = lastModified;
            FirstYear = firstYear;
            LastYear = lastYear;
        }
    }
}
