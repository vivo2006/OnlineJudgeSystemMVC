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

        } // constructor for migrations
        public OnlineJudgeDbContext(DbContextOptions options)
            : base(options)
        {

        } // constructor for dependency injection

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(Connection.conString);
            }
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) //Setting up relationships and restrictions
        {
            modelBuilder.Entity<Competition>().HasIndex(u => u.Notation).IsUnique();
            modelBuilder.Entity<Problem>().HasIndex(p => new { p.YearCompetitionId, p.ProblemNotation }).IsUnique();
            modelBuilder.Entity<Problem>().HasOne(p => p.YearCompetition).WithMany().HasForeignKey(p => p.YearCompetitionId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Submission>().Property(s => s.Status).HasDefaultValue("Completed");
            modelBuilder.Entity<Submission>().Property(s => s.LanguageId).HasDefaultValue(0);
            modelBuilder.Entity<Submission>().Property(s => s.ChallengeBonus).HasDefaultValue(0);
            modelBuilder.Entity<Submission>().HasIndex(s => new { s.UserId, s.Created });
            modelBuilder.Entity<Submission>().HasIndex(s => s.UserId);
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<IdentityUserLogin<string>>().Property(p => p.LoginProvider).HasMaxLength(128);
            modelBuilder.Entity<IdentityUserLogin<string>>().Property(p => p.ProviderKey).HasMaxLength(128);
            modelBuilder.Entity<IdentityUserToken<string>>().Property(p => p.LoginProvider).HasMaxLength(128);
            modelBuilder.Entity<IdentityUserToken<string>>().Property(p => p.Name).HasMaxLength(128);
        }
        //DbSets refer to the tables in the database
        public virtual DbSet<Competition> Competitions { get; set; }
        public DbSet<GroupCompetition> GroupCompetitions { get; set; }
        public DbSet<YearCompetition> YearCompetitions { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<TestCaseResult> TestCaseResults { get; set; }
    }
}
