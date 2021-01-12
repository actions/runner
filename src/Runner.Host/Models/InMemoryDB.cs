using GitHub.DistributedTask.WebApi;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Runner.Host.Models
{
    public class InMemoryDB : DbContext
    {
        public InMemoryDB(DbContextOptions<InMemoryDB> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TimelineRecord>();
            var issue = modelBuilder.Entity<Issue>();
            issue.HasKey(issue => new { issue.Category, issue.Type, issue.Message});
            var timelineAttempt = modelBuilder.Entity<TimelineAttempt>();
            timelineAttempt.HasKey(timelineAttempt => timelineAttempt.Identifier);
            
        }
    }
}