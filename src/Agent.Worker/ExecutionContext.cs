using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(ExecutionContext))]
    public interface IExecutionContext : IAgentService
    {
        CancellationToken CancellationToken { get; }
        List<ServiceEndpoint> Endpoints { get; }
        Variables Variables { get; }
        bool WriteDebug { get; set; }

        IExecutionContext CreateChild();
        void InitializeEnvironment(JobRequestMessage message);
        void Write(string tag, string message);
    }

    public sealed class ExecutionContext : AgentService, IExecutionContext
    {
        private IWebConsoleLogger _console;
        private IPagingLogger _logger;

        public CancellationToken CancellationToken { get; private set; }
        public List<ServiceEndpoint> Endpoints { get; private set; }
        // TODO: Fix casing on "TimelineId".
        public Guid TimeLineId { get; private set; }
        public Variables Variables { get; private set; }
        public bool WriteDebug { get; set; }

        public IExecutionContext CreateChild()
        {
            Trace.Entering();
            var child = new ExecutionContext();
            child.Initialize(HostContext, parent: this);
            return child;
        }

        public override void Initialize(IHostContext hostContext)
        {
            Initialize(hostContext, parent: null);
        }

        public void InitializeEnvironment(JobRequestMessage message)
        {
            // Validate/store parameters.
            Trace.Entering();
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Environment, nameof(message.Environment));
            ArgUtil.NotNull(message.Environment.Endpoints, nameof(message.Environment.Endpoints));
            ArgUtil.NotNull(message.Environment.Variables, nameof(message.Environment.Variables));

            // Initialize the environment.
            Endpoints = message.Environment.Endpoints;
            Variables = new Variables(HostContext, message.Environment.Variables);
        }

        // Do not add a format string overload. In general, execution context messages are user facing and
        // therefore should be localized. Use the Loc methods from the StringUtil class. The exception to
        // the rule is command messages - which should be crafted using strongly typed wrapper methods.
        public void Write(string tag, string message)
        {
            string msg = $"{tag}{message}";
            _logger.Write(msg);
            _console.Write(msg);
        }

        private void Initialize(IHostContext hostContext, ExecutionContext parent)
        {
            base.Initialize(hostContext);
            TimeLineId = Guid.NewGuid();
            CancellationToken parentToken = parent != null ? parent.CancellationToken : HostContext.CancellationToken;
            CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(parentToken).Token;
            _console = HostContext.CreateService<IWebConsoleLogger>();
            _console.TimeLineId = TimeLineId;
            _logger = HostContext.CreateService<IPagingLogger>();
            _logger.TimeLineId = TimeLineId;
            if (parent != null)
            {
                Endpoints = parent.Endpoints;
                Variables = parent.Variables;
            }
        }
    }

    public static class ExecutionContextExtension
    {
        public static void Error(this IExecutionContext context, Exception ex)
        {
            context.Error(ex.Message);
            context.Debug(ex.ToString());
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Error(this IExecutionContext context, string message)
        {
            context.Write(WellKnownTags.Error, message);
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Warning(this IExecutionContext context, string message)
        {
            context.Write(WellKnownTags.Warning, message);
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Output(this IExecutionContext context, string message)
        {
            context.Write(null, message);
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Command(this IExecutionContext context, string message)
        {
            context.Write(WellKnownTags.Command, message);
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Section(this IExecutionContext context, string message)
        {
            context.Write(WellKnownTags.Section, message);
        }

        //
        // Verbose output is enabled by setting System.Debug
        // It's meant to help the end user debug their definitions.
        // Why are my inputs not working?  It's not meant for dev debugging which is diag
        //
        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Debug(this IExecutionContext context, string message)
        {
            if (context.WriteDebug)
            {
                context.Write(WellKnownTags.Debug, message);
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