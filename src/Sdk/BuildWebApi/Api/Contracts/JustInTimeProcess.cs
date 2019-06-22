using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    [DataContract]
    public class JustInTimeProcess : BuildProcess
    {
        public JustInTimeProcess()
            : this(null)
        {
        }

        internal JustInTimeProcess(
            ISecuredObject securedObject)
            : base(ProcessType.JustInTime, securedObject)
        {
        }
    }
}
