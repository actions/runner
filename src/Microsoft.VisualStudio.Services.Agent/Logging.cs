using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Globalization;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface ILogWriter
    {
        Guid TimeLineId { get; set; }
        void Write(string message);
    }

    [ServiceLocator(Default = typeof(PagingLogger))]
    public interface IPagingLogger : IAgentService, ILogWriter
    {
    }

    public class PagingLogger : AgentService, IPagingLogger
    {
        IJobServer _jobServer;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _jobServer = hostContext.GetService<IJobServer>();
        }

        public Guid TimeLineId { get; set; }

        //
        // Write a metadata file with id etc, point to pages on disk.
        // Each page is a guid_#.  As a page rolls over, it events it's done
        // and the consumer queues it for upload
        // Ensure this is lazy.  Create a page on first write
        //
        public void Write(string message)
        {
            if (TimeLineId == Guid.Empty)
            {
                throw new InvalidOperationException("TimeLineId must be set");
            }

            Console.WriteLine(StringUtil.Format("LOG: {0} {1}", TimeLineId.ToString(), message));
        }
    }
}