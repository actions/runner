using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent
{

    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ServiceLocatorAttribute : Attribute
    {
        public Type Default { get; set; }

        public static readonly String DefaultPropertyName = "Default";
    }

    public interface IAgentService
    {
        void Initialize(IHostContext context);
    }

    public abstract class AgentService
    {
        public static string Version { get { return "2.0.0"; }}
        
        protected IHostContext HostContext { get; private set; }
        protected TraceSource Trace { get; private set; }

        public string TraceName 
        {
            get 
            {
                return this.GetType().Name; 
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