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
        void Write(string tag, String format, params Object[] args);
        IExecutionContext CreateChild();
    }

    public sealed class ExecutionContext : AgentService, IExecutionContext
    {
        private IWebConsoleLogger _console;
        private IPagingLogger _logger;

        public bool WriteDebug { get; set; }
        public Guid TimeLineId { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public void Write(string tag, String format, params Object[] args)
        {
            if (tag == null)
            {
                tag = String.Empty;
            }

            string msg = String.Format(CultureInfo.InvariantCulture, "{0}{1}", tag, StringUtil.Format(format, args));
            _logger.Write(msg);
            _console.Write(msg);
        }

        public IExecutionContext CreateChild()
        {
            var child = new ExecutionContext();
            child.Initialize(HostContext, parent: this);
            return child;
        }

        public override void Initialize(IHostContext hostContext)
        {
            Initialize(hostContext, parent: null);
        }

        private void Initialize(IHostContext hostContext, ExecutionContext parent)
        {
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
    }

    public static class ExecutionContextExtension
    {
        public static void Error(this IExecutionContext context, Exception ex)
        {
            context.Error(ex.Message);
            context.Debug(ex.ToString());
        }

        public static void Error(this IExecutionContext context, String format, params Object[] args)
        {
            context.Write(WellKnownTags.Error, format, args);
        }

        public static void Warning(this IExecutionContext context, String format, params Object[] args)
        {
            context.Write(WellKnownTags.Warning, format, args);
        }

        public static void Output(this IExecutionContext context, String format, params Object[] args)
        {
            context.Write(null, format, args);
        }

        public static void Command(this IExecutionContext context, String format, params Object[] args)
        {
            context.Write(WellKnownTags.Command, format, args);
        }

        public static void Section(this IExecutionContext context, String format, params Object[] args)
        {
            context.Write(WellKnownTags.Section, format, args);
        }

        //
        // Verbose output is enabled by setting System.Debug
        // It's meant to help the end user debug their definitions.
        // Why are my inputs not working?  It's not meant for dev debugging which is diag
        //
        public static void Debug(this IExecutionContext context, String format, params Object[] args)
        {
            if (context.WriteDebug)
            {
                context.Write(WellKnownTags.Debug, format, args);
            }
        }
    }

    public static class WellKnownTags
    {
        public static readonly String Section = "##[section]";
        public static readonly String Command = "##[command]";
        public static readonly String Error = "##[error]";
        public static readonly String Warning = "##[warning]";
        public static readonly String Debug = "##[debug]";
    }
}