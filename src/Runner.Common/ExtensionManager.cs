using Runner.Common.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Runner.Common
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
                case "Runner.Common.Capabilities.ICapabilitiesProvider":
                    Add<T>(extensions, "Runner.Common.Capabilities.AgentCapabilitiesProvider, Runner.Common");
                    break;
                // Listener agent configuration providers
                case "Runner.Common.Listener.Configuration.IConfigurationProvider":
                    Add<T>(extensions, "Runner.Common.Listener.Configuration.BuildReleasesAgentConfigProvider, Runner.Listener");
                    break;
                // Worker job extensions.
                case "Runner.Common.Worker.IJobExtension":
                    Add<T>(extensions, "Runner.Common.Worker.Build.BuildJobExtension, Runner.Worker");
                    break;
                // Action command extensions.
                case "Runner.Common.Worker.IActionCommandExtension":
                    Add<T>(extensions, "Runner.Common.Worker.InternalPluginSetRepoPathCommandExtension, Runner.Worker");
                    Add<T>(extensions, "Runner.Common.Worker.SetEnvCommandExtension, Runner.Worker");
                    Add<T>(extensions, "Runner.Common.Worker.SetOutputCommandExtension, Runner.Worker");
                    Add<T>(extensions, "Runner.Common.Worker.SetSecretCommandExtension, Runner.Worker");
                    Add<T>(extensions, "Runner.Common.Worker.AddPathCommandExtension, Runner.Worker");
                    Add<T>(extensions, "Runner.Common.Worker.WarningCommandExtension, Runner.Worker");
                    Add<T>(extensions, "Runner.Common.Worker.ErrorCommandExtension, Runner.Worker");
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
