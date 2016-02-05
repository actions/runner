using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class MockExecutionContext : IExecutionContext
    {
        public Action<Exception> _Error_E { get; set; }
        public Action<String> _Error_M { get; set; }
        public Action<String, Object[]> _Error_F_A { get; set; }

        public MockExecutionContext()
        {
        }

        public CancellationToken CancellationToken { get; set;}
        
        public void Error(Exception ex)
        {
            LogMessage(LogLevel.Error, ex.ToString());
            if (this._Error_E != null) { this._Error_E(ex); }
        }

        public void Error(String message)
        {
            LogMessage(LogLevel.Error, message);
            if (this._Error_M != null) { this._Error_M(message); }
        }

        public void Error(String format, params Object[] args)
        {
            LogMessage(LogLevel.Error, format, args);
            if (this._Error_F_A != null) { this._Error_F_A(format, args); }
        }

        public void LogMessage(LogLevel level, String format, params Object[] args)
        {
            // TODO: Consider logging this to somewhere for the test execution. Writing to console causes
            // the output to print to out in the middle of the xunit summary info. This would likely be
            // an issue since xunit executes tests in parallel.
            //Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "[{0}] {1}", level, message));
        }
    }
}
