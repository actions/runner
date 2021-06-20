using System;

namespace GitHub.Runner.Common
{

    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ServiceLocatorAttribute : Attribute
    {
        public static readonly string DefaultPropertyName = "Default";
        public static readonly string WindowsPropertyName = "Windows";
        public static readonly string OSXPropertyName = "OSX";
        public static readonly string LinuxPropertyName = "Linux";

        public Type Default { get; set; }

        public Type Windows { get; set; }
        public Type OSX { get; set; }
        public Type Linux { get; set; }
    }

    public interface IRunnerService
    {
        void Initialize(IHostContext context);
    }

    public abstract class RunnerService
    {
        protected IHostContext HostContext { get; private set; }
        protected Tracing Trace { get; private set; }

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
