using System.Security.Cryptography;
using System.Timers;
using GitHub.DistributedTask.WebApi;
using Runner.Server.Controllers;

namespace Runner.Server.Models
{
    public class Session
    {
        public TaskAgentSession TaskAgentSession {get; set;}

        public Agent Agent {get; set;}

        // public bool RunsJob { get; set;}

        public Aes Key {get;set;}
        public MessageController.Job Job { get; set; }

        public Timer Timer {get; set;}
        public Timer JobTimer {get; set;}
    }
}