using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Services.Agent.Build;
using Microsoft.VisualStudio.Services.Agent.Common;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(ExtensionManager))]
    public interface IExtensionManager : IAgentService
    {
        List<T> GetExtensions<T>() where T : class, IExtension;
    }

    public class ExtensionManager : AgentService, IExtensionManager
    {
        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            LoadExtensions();
        }
        
        public List<T> GetExtensions<T>() where T : class, IExtension 
        {
            Trace.Info("Get all '{0}' extensions.", typeof(T).Name);
            List<IExtension> extensions;
            if (_cache.TryGetValue(typeof(T), out extensions))
            {
                return extensions.Select(x => x as T).ToList();
            }

            Trace.Verbose("No extensions found.");
            return null;
        }

        //
        // We will load extensions from assembly
        // once AssemblyLoadContext.Resolving event is able to 
        // resolve dependency recursively
        //
        private void LoadExtensions()
        {
            lock (_cacheLock)
            {
                if (_cache.Count > 0) { return; }
                AddExtension("Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildJob, Agent.Worker");
                AddExtension(new BuildCommands());
            }
        }

        private void AddExtension(string assemblyQualifiedName)
        {
            Trace.Verbose($"Creating instance: {assemblyQualifiedName}");
            Type type = Type.GetType(assemblyQualifiedName, throwOnError: true);
            AddExtension(Activator.CreateInstance(type) as IExtension);
        }

        private void AddExtension(IExtension extension)
        {
            extension.Initialize(HostContext);
            Trace.Verbose($"Registering extension: {extension.GetType().FullName}");
            List<IExtension> list = _cache.GetOrAdd(extension.ExtensionType, new List<IExtension>());
            list.Add(extension);
        }

        private readonly ConcurrentDictionary<Type, List<IExtension>> _cache = new ConcurrentDictionary<Type, List<IExtension>>();
        private readonly object _cacheLock = new object();
    }
}