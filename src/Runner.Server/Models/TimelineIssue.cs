
using GitHub.DistributedTask.WebApi;

namespace Runner.Server.Models
{
    public class TimelineIssue {
        public long Id
        {
            get;
            set;
        }
        
        public TimelineRecord Record
        {
            get;
            set;
        }
        
        public IssueType Type
        {
            get;
            set;
        }

        public string Category
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public bool? IsInfrastructureIssue
        {
            get;
            set;
        }

        public string Data
        {
            get;
            set;
        }
    }
}