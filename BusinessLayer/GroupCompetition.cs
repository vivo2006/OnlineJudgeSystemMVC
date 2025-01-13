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
        [Required(ErrorMessage = "Foreign key is required.")]
        public int CompetitionId {  get; set; }
        [ForeignKey("CompetitionId")]
        public Competition Competition { get; set; }
        public GroupCompetition() 
        {
            Problems = new List<Problem>();
        }
        public GroupCompetition(int id, string group, List<Problem> problems, int competitionId)
        {
            Id = id;
            Group = group;
            Problems = problems;
            CompetitionId = competitionId;
        }
    }
}
