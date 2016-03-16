using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent
{

    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ServiceLocatorAttribute : Attribute
    {
        public static readonly string DefaultPropertyName = "Default";

        public Type Default { get; set; }
    }

    public interface IAgentService
    {
        void Initialize(IHostContext context);
    }

    public abstract class AgentService
    {
        protected IHostContext HostContext { get; set; }
        protected TraceSourceWrapper Trace { get; private set; }

        public string TraceName 
        {
            get 
            {
                return GetType().Name;
            }
        }

        public virtual void Initialize(IHostContext hostContext)
        {
            HostContext = hostContext;
            Trace = HostContext.GetTrace(TraceName);
            Trace.Entering();
        }
    }
}