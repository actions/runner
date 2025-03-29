using System;

namespace GitHub.Runner.Common
{
    public class AuthMigrationEventArgs : EventArgs
    {
        public AuthMigrationEventArgs(string trace)
        {
            Trace = trace;
        }
        public string Trace { get; private set; }
    }
}
