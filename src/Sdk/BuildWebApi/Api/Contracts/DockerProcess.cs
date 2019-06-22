using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    [DataContract]
    public class DockerProcess : BuildProcess
    {
        public DockerProcess()
            : this(null)
        {
        }

        internal DockerProcess(
            ISecuredObject securedObject)
            : base(ProcessType.Docker, securedObject)
        {
        }

        [DataMember(EmitDefaultValue = false)]
        public DockerProcessTarget Target { get; set; }
    }
}
