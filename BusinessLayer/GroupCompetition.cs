using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class GroupCompetition
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Group name is required.")] 
        [MaxLength(100, ErrorMessage = "Group name cannot exceed 100 characters.")]
        public string Group { get; set; }
        public List<Problem>? Problems { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        [Required(ErrorMessage = "Foreign key is required.")]
        public int YearCompetitionId {  get; set; }
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
