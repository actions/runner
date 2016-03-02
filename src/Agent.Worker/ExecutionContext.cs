using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(ExecutionContext))]
    public interface IExecutionContext : IAgentService
    {
        bool WriteDebug { get; set; }
        CancellationToken CancellationToken { get; }
        void Error(string format, params Object[] args);
        void Warning(string format, params Object[] args);
        void Output(string format, params Object[] args);
        void Debug(string format, params Object[] args);
        IExecutionContext CreateChild();
    }

    public sealed class ExecutionContext : AgentService, IExecutionContext
    {
        private IWebConsoleLogger _console;
        private IPagingLogger _logger;

        public bool WriteDebug { get; set; }
        public Guid TimeLineId { get; private set; }
        public CancellationToken CancellationToken { get; private set;}
        
        public void Error(Exception ex)
        {
            Error(ex.Message);
            Debug(ex.ToString());
        }

        public void Error(string format, params Object[] args)
        {
            Write("Error", format, args);
        }

        public void Warning(String format, params Object[] args)
        {
            Write("Warning", format, args);
        }

        public void Output(String format, params Object[] args)
        {
            Write(null, format, args);
        }

        //
        // Verbose output is enabled by setting System.Debug
        // It's meant to help the end user debug their definitions.
        // Why are my inputs not working?  It's not meant for dev debugging which is diag
        //
        public void Debug(String format, params Object[] args)
        {
            if (WriteDebug)
            {
                Write("Debug", format, args);   
            }
        }

        public IExecutionContext CreateChild()
        {
            var child = new ExecutionContext();
            child.Initialize(HostContext, parent: this);
            return child;
        }

        public override void Initialize(IHostContext hostContext) {
            Initialize(hostContext, parent: null);
        }

        private void Initialize(IHostContext hostContext, ExecutionContext parent) {
            base.Initialize(hostContext);
            TimeLineId = Guid.NewGuid();
            CancellationToken parentToken = parent != null ? parent.CancellationToken : HostContext.CancellationToken;
            CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(parentToken).Token;
            Trace.Verbose("Creating console logger.");
            _console = HostContext.CreateService<IWebConsoleLogger>();
            _console.TimeLineId = TimeLineId;
            Trace.Verbose("Creating logger.");
            _logger = HostContext.CreateService<IPagingLogger>();
            _logger.TimeLineId = TimeLineId;
        }

        private void Write(string tag, String format, params Object[] args)
        {
            string prefix = tag != null ? StringUtil.Format("##[{0}] ", tag) : String.Empty;
            string msg = String.Format(CultureInfo.InvariantCulture, "{0}{1}", prefix, StringUtil.Format(format, args));
            _logger.Write(msg);
            _console.Write(msg);
        }              
    }
}