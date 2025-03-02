using System;
using System.Text.RegularExpressions;

namespace Runner.Server.Services
{
    public class TraceWriter2 : GitHub.DistributedTask.ObjectTemplating.ITraceWriter, GitHub.DistributedTask.Expressions2.ITraceWriter
    {
        private Action<string> callback;
        private Regex regex;
        private int verbosity;
        public TraceWriter2(Action<string> callback, int verbosity = 0) {
            this.callback = callback;
            regex = new Regex("\r?\n");
            this.verbosity = verbosity;
        }

        public void Callback(string lines) {
            foreach(var line in regex.Split(lines)) {
                callback(line);
            }
        }

        public void Error(string format, params object[] args)
        {
            if(args?.Length == 1 && args[0] is Exception ex) {
                Callback(string.Format("{0} {1}", format, ex.Message));
                return;
            }
            try {
                Callback(args?.Length > 0 ? string.Format(format, args) : format);
            } catch {
                Callback(format);
            }
        }

        public void Info(string format, params object[] args)
        {
            if(verbosity <= 2) {
                try {
                    Callback(args?.Length > 0 ? string.Format(format, args) : format);
                } catch {
                    Callback(format);
                }
            }
        }

        public void Info(string message)
        {
            if(verbosity <= 2) {
                Callback(message);
            }
        }
        public void Verbose(string format, params object[] args)
        {
            if(verbosity <= 1) {
                try {
                    Callback(args?.Length > 0 ? string.Format(format, args) : format);
                } catch {
                    Callback(format);
                }
            }
        }

        public void Verbose(string message)
        {
            if(verbosity <= 1) {
                Callback(message);
            }
        }

        public void Trace(string message)
        {
            if(verbosity <= 0) {
                Callback(message);
            }
        }
    }

}
