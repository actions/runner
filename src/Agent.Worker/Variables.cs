using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class Variables
    {
        private readonly ConcurrentDictionary<string, string> _store = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly IHostContext _hostContext;
        private readonly TraceSource _trace;

        public Variables(IHostContext hostContext, IDictionary<string, string> copy)
        {
            _hostContext = hostContext;
            _trace = _hostContext.GetTrace(nameof(Variables));
            ArgUtil.NotNull(hostContext, nameof(hostContext));
            ArgUtil.NotNull(copy, nameof(copy));
            foreach (string key in copy.Keys)
            {
                _store[key] = copy[key];
            }
        }

        public BuildCleanOption? Build_Clean { get { return GetEnum<BuildCleanOption>(Constants.Variables.Build.Clean); } }
        public string Build_DefinitionName { get { return Get(Constants.Variables.Build.DefinitionName); } }
        public string System_CollectionId { get { return Get(Constants.Variables.System.CollectionId); } }
        public string System_DefinitionId { get { return Get(Constants.Variables.System.DefinitionId); } }
        public string System_HostType { get { return Get(Constants.Variables.System.HostType); } }
        public string System_TFCollectionUrl { get { return Get(WellKnownDistributedTaskVariables.TFCollectionUrl);  } }

        public string Get(string name)
        {
            string val = _store[name];
            _trace.Verbose($"Get {name}='{val}'");
            return val;
        }

        public bool? GetBoolean(string name)
        {
            bool val;
            if (bool.TryParse(Get(name), out val))
            {
                return val;
            }

            return null;
        }

        public T? GetEnum<T>(string name) where T : struct
        {
            // TODO: Make sure null doesn't blow up.
            T val;
            if (Enum.TryParse(Get(name), ignoreCase: true, result: out val))
            {
                return val;
            }

            return null;
        }

        public void Set(string name, string val)
        {
            // TODO: Determine whether this line should be uncommented again: ArgUtil.NotNull(val, nameof(val));
            // Can variables not be cleared? Can a null variable come across the wire? What if the user does ##setvariable from a script and we interpret as null instead of empty string. This feels brittle.

            _trace.Verbose($"Set {name}='{val}'");
            _store[name] = val;
        }
    }
}