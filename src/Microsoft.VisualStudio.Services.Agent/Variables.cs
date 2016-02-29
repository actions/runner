using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface IVariables
    {
        string Get(string name);
        void Set(string name, string val);    
    }
    
    public sealed class Variables: IVariables
    {
        private Dictionary<string, string> _store;
        private TraceSource _trace;
        
        public Variables(IHostContext hostContext)
        {
            _trace = hostContext.GetTrace("CommandLineParser");
            _store = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);    
        }
                
        public string Get(string name)
        {
            string val = _store[name];
            _trace.Verbose("Get {0}={1}", name, val);
            return val;            
        }
        
        public void Set(string name, string val)
        {
            if (val == null)
            {
                throw new ArgumentNullException("val");
            }
            
            _trace.Info("Set {0}={1}", name, val);
            _store[name] = val;
        }
    }
}