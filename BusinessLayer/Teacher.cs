using Azure;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class Teacher : User
    {
        public ICollection<Student>? Students { get; set; }
        public Teacher() 
        {
            Students = new List<Student>();
        }
        public Teacher(string name, int age, string city, int points, List<Submission> submissions, List<Student> students) : base(name, age, city, points, submissions)
        {
            Students = students;
        }
    }
}
