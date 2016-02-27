using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{        
    public interface IExecutionContext
    {
        bool WriteDebug { get; set; }
        CancellationToken CancellationToken { get; }
        void Error(string format, params Object[] args);
        void Warning(string format, params Object[] args);
        void Output(string format, params Object[] args);
        void Debug(string format, params Object[] args);
    }
    
    public class ExecutionContext: IExecutionContext
    {
        private IWebConsoleLogger _console;
        private IPagingLogger _logger;
        private TraceSource _trace;
        
        public ExecutionContext(IHostContext hostContext, Guid timeLineId) {
            _trace = hostContext.GetTrace("ExecutionContext");
            
            _trace.Info("Constructor");
            TimeLineId = timeLineId;
            CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(hostContext.CancellationToken).Token;
            
            _trace.Info("Creating console");
            _console = hostContext.CreateService<IWebConsoleLogger>();
            _console.TimeLineId = timeLineId;
            
            _trace.Info("Creating logger");
            _logger = hostContext.CreateService<IPagingLogger>();
            _logger.TimeLineId = timeLineId;
        }
        
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
        
        private void Write(string tag, String format, params Object[] args)
        {
            string prefix = tag != null ? StringUtil.Format("##[{0}] ", tag) : String.Empty;
            string msg = String.Format(CultureInfo.InvariantCulture, "{0}{1}", prefix, StringUtil.Format(format, args));
            _logger.Write(msg);
            _console.Write(msg);
        }              
    }
}