using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(JobServerQueue))]
    public interface IJobServerQueue : IAgentService
    {
        void QueueWebConsoleLine(Guid timelineId, Guid timelineRecordId, string line);
        void QueueFileUpload(Guid timelineId, Guid timelineRecordId, string type, string name, string path, bool deleteSource);
        void QueueTimelineRecordUpdate(Guid timelineId, TimelineRecord timelineRecord);
    }

    public sealed class JobServerQueue : AgentService, IJobServerQueue
    {
        private ConcurrentQueue<String> _consoleLines = new ConcurrentQueue<String>();
        private ConcurrentDictionary<Guid, TimelineInfo> _timelines = new ConcurrentDictionary<Guid, TimelineInfo>();
        private Dictionary<Guid, List<TimelineRecord>> _bufferedRetryRecords = new Dictionary<Guid, List<TimelineRecord>>();
        private ConcurrentQueue<UploadFileInfo> _uploadFiles = new ConcurrentQueue<UploadFileInfo>();

        public void QueueWebConsoleLine(Guid timelineId, Guid timelineRecordId, string line)
        {
            // TODO: queue line
            Console.WriteLine(StringUtil.Format("Console: {0}", line));
        }

        public void QueueFileUpload(Guid timelineId, Guid timelineRecordId, string type, string name, string path, bool deleteSource)
        {
            throw new NotImplementedException();
        }

        public void QueueTimelineRecordUpdate(Guid timelineId, TimelineRecord timelineRecord)
        {
            throw new NotImplementedException();
        }
    }

    internal class TimelineInfo
    {
        public Guid TimelineId { get; set; }
        public Guid? ParentTimelineId { get; set; }
        public Guid? ParentTimelineRecordId { get; set; }
        public Boolean Created { get; set; }
        public ConcurrentQueue<TimelineRecord> Records { get; set; }
    }

    internal class UploadFileInfo
    {
        public Guid TimelineId { get; set; }
        public Guid TimelineRecordId { get; set; }
        public String Type { get; set; }
        public String Name { get; set; }
        public String Path { get; set; }
        public Boolean DeleteSource { get; set; }
    }
}