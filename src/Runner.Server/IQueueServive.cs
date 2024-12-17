using System.Threading;
using GitHub.DistributedTask.Pipelines;

namespace Runner.Server
{
    public interface IQueueService
    {
        void PickJob(AgentJobRequestMessage message, CancellationToken token, string[] labels);
    }
}
