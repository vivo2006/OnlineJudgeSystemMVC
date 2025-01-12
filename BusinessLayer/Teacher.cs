using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class Teacher : IdentityUser
    {
        public ICollection<Student>? Students { get; set; }
        public Teacher() 
        {
            Students = new List<Student>();
        }
        public Teacher(List<Student> students)
        {
            Students = students;
        }
    }
}
