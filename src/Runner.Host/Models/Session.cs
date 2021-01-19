using System.Security.Cryptography;
using GitHub.DistributedTask.WebApi;
using Runner.Host.Controllers;

namespace Runner.Host.Models
{
    public class Session
    {
        public TaskAgentSession TaskAgentSession {get; set;}

        public Agent Agent {get; set;}

        // public bool RunsJob { get; set;}

        public Aes Key {get;set;}
        public MessageController.Job Job { get; set; }
    }
}