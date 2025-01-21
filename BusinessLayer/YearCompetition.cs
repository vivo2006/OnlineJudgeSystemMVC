using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class YearCompetition
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int Year { get; set; }
        public ICollection<GroupCompetition> GroupCompetitions { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        [Required]
        public int CompetitionId { get; set; }
        [ForeignKey("CompetitionId")]
        public Competition Competition { get; set; }
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
