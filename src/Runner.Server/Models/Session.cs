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
        private Job job;
        public Session() {
            source = new CancellationTokenSource();
            job = null;
            MessageLock = new SemaphoreSlim(1, 1);
        }
        public TaskAgentSession TaskAgentSession {get; set;}

        public Agent Agent {get; set;}

        // public bool RunsJob { get; set;}

        public Aes Key {get;set;}

        public CancellationToken JobRunningToken { get => source.Token; }
        public Job Job { get => job; set {
                if(job != null) {
                    job.SessionId = Guid.Empty;
                    try {
                        job.CleanUp?.Invoke();
                        job.CleanUp = null;
                    } catch {

                    } finally {
                        
                    }
                }
                job = value;
                if(job == null) {
                    source.Cancel();
                } else {
                    FirstJobReceived = true;
                    source.Cancel();
                    source.Dispose();
                    source = new CancellationTokenSource();
                }
            }
        }
        public DateTime? DoNotCancelBefore {get; set;}

        public System.Timers.Timer Timer {get; set;}
        public System.Timers.Timer JobTimer {get; set;}
        public Action<string> DropMessage { get; set; }
        public SemaphoreSlim MessageLock { get; }
    }
}