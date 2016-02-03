using System;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class MockContext : IContext
    {
        public Action<Exception> _Error_E { get; set; }
        public Action<String> _Error_M { get; set; }
        public Action<String, Object[]> _Error_F_A { get; set; }

        public void Error(Exception ex)
        {
            if (this._Error_E != null) { this._Error_E(ex); }
        }

        public void Error(String message)
        {
            if (this._Error_M != null) { this._Error_M(message); }
        }

        public void Error(String format, params Object[] args)
        {
            if (this._Error_F_A != null) { this._Error_F_A(format, args); }
        }

        public void Warning(String message)
        {
        }

        public void Warning(String format, params Object[] args)
        {
        }

        public void Info(String message)
        {
        }

        public void Info(String format, params Object[] args)
        {
        }

        public void Verbose(String message)
        {
        }

        public void Verbose(String format, params Object[] args)
        {
        }
    }
}
