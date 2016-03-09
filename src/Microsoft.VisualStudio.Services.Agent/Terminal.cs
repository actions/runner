using Microsoft.VisualStudio.Services.Agent.Util;
using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    //
    // Abstracts away interactions with the terminal which allows:
    // (1) Console writes also go to trace for better context in the trace
    // (2) Reroute in tests
    //
    [ServiceLocator(Default = typeof(Terminal))]
    public interface ITerminal: IAgentService
    {
        bool Silent { get; set; }
        void WriteLine();
        void WriteLine(string line, params object[] args);
        void WriteError(string line, params object[] args);
    }
    
    public class Terminal: AgentService, ITerminal
    {
        public bool Silent { get; set; }
        public void WriteLine()
        {
            if (!Silent)
            {
                Console.WriteLine();    
            }
        }
        
        public void WriteLine(string line, params object[] args)
        {
            var msg = StringUtil.Format(line, args);

            Trace.Info("term: {0}", msg);
            if (!Silent)
            {
                Console.WriteLine(msg);    
            }
        }

        public void WriteError(string line, params object[] args)
        {
            var msg = StringUtil.Format(line, args);
            Trace.Error("term: {0}", msg);
            if (!Silent)
            {
                Console.Error.WriteLine(msg);   
            }
        }
    }
}