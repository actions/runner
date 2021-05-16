using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using Microsoft.EntityFrameworkCore;

namespace Runner.Server.Models
{
    public class SqLiteDb : DbContext {
        public SqLiteDb(DbContextOptions<SqLiteDb> opt) : base(opt) {
            try {
                Database.Migrate();
            } catch {
                // This allways throws, if using the InMemory Database
            }
        }

        public DbSet<Agent> Agents { get; set; }
        public DbSet<TaskAgent> TaskAgents { get; set; }
        
        public DbSet<Pool> Pools { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder){
            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgent>().Ignore(agent => agent.Properties);
            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgent>().Ignore(agent => agent.AssignedRequest);
            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgent>().Ignore(agent => agent.LastCompletedRequest);
            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgentReference>().Ignore(agent => agent.Links);


            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgent>().Ignore(agent => agent.Authorization);
            
            // modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgentAuthorization>().HasKey(a => a.ClientId);
            // modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgentPublicKey>().HasKey(a => new { a.Exponent, a.Modulus });

            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgentPool>().Ignore(agent => agent.Properties);

        }

        // public override void Dispose() {
        //     Database.CloseConnection();
        //     base.Dispose();
        // }

        // public override async ValueTask DisposeAsync() {
        //     await Database.CloseConnectionAsync();
        //     await base.DisposeAsync();
        // }
    }

    
}