using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public class BuildJob : AgentService, IJobExtension
    {
        public Type ExtensionType => typeof(IJobExtension);
        public string HostType => "build";

        public IStep PrepareStep
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IStep FinallyStep
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}