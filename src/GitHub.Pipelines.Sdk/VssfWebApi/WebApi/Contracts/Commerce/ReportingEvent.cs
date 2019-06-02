using System;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Commerce
{
    public class ReportingEvent
    {
        public ReportingEvent()
        {
            Properties = new SerializableDictionary<string, string>();
        }

        public string EventId { get; set; }

        public DateTime EventTime { get; set; }

        public string EventName { get; set; }

        public Guid OrganizationId { get; set; }

        public string OrganizationName { get; set; }

        public Guid CollectionId { get; set; }

        public string CollectionName { get; set; }

        public string Environment { get; set; }

        public Guid UserIdentity { get; set; }

        public Guid ServiceIdentity { get; set; }

        public string Version { get; set; }

        public SerializableDictionary<string, string> Properties { get; set; }
    }
}