using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Entities
{
    public class GroupCompetition
    {
        [Key] //Primary key
        public int Id { get; set; }
        [Required(ErrorMessage = "Името е задължително.")] //Data validation using DataAnnotations
        [MaxLength(100, ErrorMessage = "Името не може да надхвърля 100 символа.")]
        public string Group { get; set; } //Name of the group
        public List<Problem>? Problems { get; set; } //A group can have many problems (one-to-many relationship)
        [Required]
        public int YearCompetitionId { get; set; }
        [ForeignKey("YearCompetitionId")] //Foreign key
        public YearCompetition YearCompetition { get; set; }
        public GroupCompetition()
        {
            Problems = new List<Problem>();
        }
        public GroupCompetition(string group, int yearCompetitionId)
        {
            Group = group;
            YearCompetitionId = yearCompetitionId;
        }
    }
}
