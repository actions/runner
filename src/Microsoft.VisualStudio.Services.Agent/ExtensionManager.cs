using System.Diagnostics;
using System.Collections.Generic;
using System;
using Microsoft.VisualStudio.Services.Agent.Build;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(ExtensionManager))]
    public interface IExtensionManager : IAgentService
    {
        List<IExtension> GetExtensions(Type extensionType);
    }

    public class ExtensionManager : AgentService, IExtensionManager
    {
        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            Trace.Verbose("Load all extensions.");
            LoadExtensions();
        }
        
        public List<IExtension> GetExtensions(Type extensionType)
        {
            if(extensionType == null)
            {
                throw new ArgumentNullException(nameof(extensionType));
            }

            Trace.Info("Get all {0} extensions.", extensionType);
            if(_extensionCache.ContainsKey(extensionType))
            {
                return _extensionCache[extensionType];
            }
            else
            {
                Trace.Verbose("Does not find extensions for extensionType {0}.", extensionType);
                return null;
            }
        }

        //////////////////////////////////////////////////////////////
        // We will load extensions from assembly
        // once AssemblyLoadContext.Resolving event is able to 
        // resolve dependency recursively
        //////////////////////////////////////////////////////////////
        private void LoadExtensions()
        {
            Trace.Verbose("Register BuildJobExtension.");
            BuildJobExtension buildJobExtension = new BuildJobExtension();
            AddExtensionToCache(buildJobExtension.ExtensionType, buildJobExtension);

            Trace.Verbose("Register BuildCommandExtension.");
            BuildCommandExtension buildCommandExtension = new BuildCommandExtension();
            AddExtensionToCache(buildCommandExtension.ExtensionType, buildCommandExtension);
        }

        private void AddExtensionToCache(Type extensionType, IExtension extension)
        {
            if (!_extensionCache.ContainsKey(extensionType))
            {
                _extensionCache[extensionType] = new List<IExtension>();
            }

            _extensionCache[extensionType].Add(extension);
        }

        private readonly Dictionary<Type, List<IExtension>> _extensionCache = new Dictionary<Type, List<IExtension>>();
    }
}