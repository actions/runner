using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(ExtensionManager))]
    public interface IExtensionManager : IAgentService
    {
        List<T> GetExtensions<T>() where T : class, IExtension;
    }

    public sealed class ExtensionManager : AgentService, IExtensionManager
    {
        private readonly ConcurrentDictionary<Type, List<IExtension>> _cache = new ConcurrentDictionary<Type, List<IExtension>>();

        public List<T> GetExtensions<T>() where T : class, IExtension
        {
            Trace.Info("Getting extensions for interface: '{0}'", typeof(T).FullName);
            List<IExtension> extensions = _cache.GetOrAdd(
                key: typeof(T),
                valueFactory: (Type key) =>
                {
                    return LoadExtensions<T>();
                });
            return extensions.Select(x => x as T).ToList();
        }

        //
        // We will load extensions from assembly
        // once AssemblyLoadContext.Resolving event is able to
        // resolve dependency recursively
        //
        private List<IExtension> LoadExtensions<T>() where T : class, IExtension
        {
            var extensions = new List<IExtension>();
            switch (typeof(T).FullName)
            {
                // Listener capabilities providers.
                case "Microsoft.VisualStudio.Services.Agent.Capabilities.ICapabilitiesProvider":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Capabilities.AgentCapabilitiesProvider, Microsoft.VisualStudio.Services.Agent");
                    break;
                // Listener agent configuration providers
                case "Microsoft.VisualStudio.Services.Agent.Listener.Configuration.IConfigurationProvider":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Listener.Configuration.BuildReleasesAgentConfigProvider, Agent.Listener");
                    break;
                // Worker job extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.IJobExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildJobExtension, Agent.Worker");
                    break;
                // Action command extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.IActionCommandExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.InternalPluginSetRepoPathCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.SetEnvCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.SetOutputCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.SetSecretCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.AddPathCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.WarningCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.ErrorCommandExtension, Agent.Worker");
                    break;
                default:
                    // This should never happen.
                    throw new NotSupportedException($"Unexpected extension type: '{typeof(T).FullName}'");
            }

            return extensions;
        }

        private void Add<T>(List<IExtension> extensions, string assemblyQualifiedName) where T : class, IExtension
        {
            Trace.Info($"Creating instance: {assemblyQualifiedName}");
            Type type = Type.GetType(assemblyQualifiedName, throwOnError: true);
            var extension = Activator.CreateInstance(type) as T;
            ArgUtil.NotNull(extension, nameof(extension));
            extension.Initialize(HostContext);
            extensions.Add(extension);
        }
    }
}