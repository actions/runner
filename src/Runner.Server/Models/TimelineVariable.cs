
using GitHub.DistributedTask.WebApi;

namespace Runner.Server.Models
{
    public class TimelineVariable {
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

        public string Name
        {
            get;
            set;
        }

        public string Value
        {
            get;
            set;
        }
    }
}