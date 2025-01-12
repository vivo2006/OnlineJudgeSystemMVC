using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BusinessLayer
{
    public class Student : User
    {   
        public ICollection<Teacher>? Teachers { get; set; }
        public ICollection<Problem>? SolvedProblems { get; set; }

        public Student()
        {
            Teachers = new List<Teacher>();
            SolvedProblems = new List<Problem>();
        }

        public Student(string name, int age, string city, int points, List<Submission> submissions, List<Teacher> teachers, List<Problem> solvedProblems) : base(name, age, city, points, submissions)
        { 
            Teachers = teachers;
            SolvedProblems = solvedProblems;
        }
    }
}
