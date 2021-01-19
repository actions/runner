using Microsoft.EntityFrameworkCore;

namespace Runner.Host.Models
{
    public class SqLiteDb : DbContext
    {

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder
            .UseSqlite(@"Data Source=Agents.db;");
        }

        public DbSet<AgentReference> Agents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder){

        }
    }
}