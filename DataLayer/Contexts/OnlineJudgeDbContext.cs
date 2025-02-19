using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.AspNetCore.Identity;
using BusinessLayer.Entities;

namespace DataLayer.Contexts
{
    public class OnlineJudgeDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public OnlineJudgeDbContext() : base()
        {

        }
        public OnlineJudgeDbContext(DbContextOptions options)
            : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(Connection.conString);
            }
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Competition> Competitions { get; set; }
        public DbSet<GroupCompetition> GroupCompetitions { get; set; }
        public DbSet<YearCompetition> YearCompetitions { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<TestCaseResult> TestCaseResults { get; set; }
    }
}
