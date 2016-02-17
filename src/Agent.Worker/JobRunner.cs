using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public class JobRunner
    {
        public JobRunner(IHostContext hostContext, StreamTransport transport) {
            m_transport = transport;
            m_hostContext = hostContext;
            m_trace = hostContext.Trace["JobRunner"];
            m_transport.PacketReceived += M_transport_PacketReceived;
        }

        private async Task M_transport_PacketReceived(object sender, IPCPacket e)
        {
            if (1 == e.MessageType)
            {
                var message = JsonUtility.FromString<TaskAgentMessage>(e.Body);
                await Run(message);
            }
        }

        public async Task Run(TaskAgentMessage message)
        {
            ExecutionContext context = new ExecutionContext(m_hostContext);
            m_trace.Verbose("Prepare");
            context.LogInfo("Prepare...");
            context.LogVerbose("Preparing...");
            
            m_trace.Verbose("Run");
            context.LogInfo("Run...");
            context.LogVerbose("Running...");
            
            m_trace.Verbose("Finish");
            context.LogInfo("Finish...");
            context.LogVerbose("Finishing...");

            context.LogInfo("Message id {0}", message.MessageId);

            m_finished = true;
        }

        public async Task WaitToFinish()
        {
            while (!m_finished)
            {
                await Task.Delay(100);
            }
            m_transport.PacketReceived -= M_transport_PacketReceived;
        }

        private IHostContext m_hostContext;
        private readonly TraceSource m_trace;
        private readonly StreamTransport m_transport;
        private volatile bool m_finished = false;
    }
}
