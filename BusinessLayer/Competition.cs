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
        public ICollection<GroupCompetition> GroupCompetitions { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public Competition()
        {

        }
        public Competition(int id, string name, ICollection<GroupCompetition> groupCompetitions, DateTime created, DateTime lastModified)
        {
            Id = id;
            Name = name;
            GroupCompetitions = groupCompetitions;
            Created = created;
            LastModified = lastModified;
        }
    }
}
