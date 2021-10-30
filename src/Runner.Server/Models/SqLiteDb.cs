using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using Microsoft.EntityFrameworkCore;

namespace Runner.Server.Models
{
    public class SqLiteDb : DbContext {
        public SqLiteDb(DbContextOptions<SqLiteDb> opt) : base(opt) {

        }

        public DbSet<Agent> Agents { get; set; }
        public DbSet<TaskAgent> TaskAgents { get; set; }
        
        public DbSet<Pool> Pools { get; set; }
        public DbSet<Owner> Owner { get; set; }
        public DbSet<Workflow> Workflows { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder){
            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgent>().Ignore(agent => agent.Properties);
            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgent>().Ignore(agent => agent.AssignedRequest);
            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgent>().Ignore(agent => agent.LastCompletedRequest);
            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgentReference>().Ignore(agent => agent.Links);


            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgent>().Ignore(agent => agent.Authorization);
            
            // modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgentAuthorization>().HasKey(a => a.ClientId);
            // modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgentPublicKey>().HasKey(a => new { a.Exponent, a.Modulus });

            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TaskAgentPool>().Ignore(agent => agent.Properties);
            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TimelineRecord>().Ignore(agent => agent.Issues);
            modelBuilder.Entity<GitHub.DistributedTask.WebApi.TimelineRecord>().Ignore(agent => agent.PreviousAttempts)
            .Ignore(agent => agent.Variables);
            
            
            
            // modelBuilder.Entity<GitHub.DistributedTask.WebApi.JobCompletedEvent>().Ignore(e => e.Outputs);
            // modelBuilder.Entity<GitHub.DistributedTask.WebApi.JobCompletedEvent>().Ignore(e => e.ActionsEnvironment);
            // modelBuilder.Entity<GitHub.DistributedTask.WebApi.JobCompletedEvent>().Ignore(e => e.ActionsStepsTelemetry);

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