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
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Името е задължително.")]
        [MaxLength(100, ErrorMessage = "Името не може да надхвърля 100 символа.")]
        public string Group { get; set; }
        public List<Problem>? Problems { get; set; }
        [Required]
        public int YearCompetitionId { get; set; }
        [ForeignKey("YearCompetitionId")]
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
