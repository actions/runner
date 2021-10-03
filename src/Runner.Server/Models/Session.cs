using System;
using System.Security.Cryptography;
using System.Timers;
using System.Threading;
using GitHub.DistributedTask.WebApi;
using Runner.Server.Controllers;

namespace Runner.Server.Models
{
    public class Session
    {
        public bool FirstJobReceived {get; private set;}
        private CancellationTokenSource source;
        private MessageController.Job job;
        public Session() {
            source = new CancellationTokenSource();
            job = null;
        }
        public TaskAgentSession TaskAgentSession {get; set;}

        public Agent Agent {get; set;}

        // public bool RunsJob { get; set;}

        public Aes Key {get;set;}

        public CancellationToken JobRunningToken { get => source.Token; }
        public MessageController.Job Job { get => job; set {
                job = value;
                if(job == null) {
                    Console.WriteLine("Job finished on session xx");
                    source.Cancel();
                } else {
                    FirstJobReceived = true;
                    source.Cancel();
                    source.Dispose();
                    source = new CancellationTokenSource();
                }
            }
        }

        public System.Timers.Timer Timer {get; set;}
        public System.Timers.Timer JobTimer {get; set;}
        public Action DropMessage { get; set; }
    }
}