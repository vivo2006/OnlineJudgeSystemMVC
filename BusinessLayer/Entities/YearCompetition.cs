using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Entities
{
    public class YearCompetition
    {
        [Key] //Primary key
        public int Id { get; set; } 
        [Required]
        public int Year { get; set; } //Year of the competition
        public ICollection<GroupCompetition> GroupCompetitions { get; set; } //A year competition can have many group competitions (one-to-many relationship)
        [Required]
        public int CompetitionId { get; set; }
        [ForeignKey("CompetitionId")] //Foreign key
        public Competition? Competition { get; set; }
        public YearCompetition()
        {
            GroupCompetitions = new List<GroupCompetition>();
        }
        public YearCompetition(int year, int competitionId)
        {
            Year = year;
            CompetitionId = competitionId;
        }
    }
}
