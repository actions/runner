﻿using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Runner.Common
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
                case "GitHub.Runner.Common.Capabilities.ICapabilitiesProvider":
                    Add<T>(extensions, "GitHub.Runner.Common.Capabilities.AgentCapabilitiesProvider, Runner.Common");
                    break;
                // Listener agent configuration providers
                case "GitHub.Runner.Listener.Configuration.IConfigurationProvider":
                    Add<T>(extensions, "GitHub.Runner.Listener.Configuration.BuildReleasesAgentConfigProvider, Runner.Listener");
                    break;
                // Worker job extensions.
                case "GitHub.Runner.Worker.IJobExtension":
                    Add<T>(extensions, "GitHub.Runner.Worker.Build.BuildJobExtension, Runner.Worker");
                    break;
                // Action command extensions.
                case "GitHub.Runner.Worker.IActionCommandExtension":
                    Add<T>(extensions, "GitHub.Runner.Worker.InternalPluginSetRepoPathCommandExtension, Runner.Worker");
                    Add<T>(extensions, "GitHub.Runner.Worker.SetEnvCommandExtension, Runner.Worker");
                    Add<T>(extensions, "GitHub.Runner.Worker.SetOutputCommandExtension, Runner.Worker");
                    Add<T>(extensions, "GitHub.Runner.Worker.SetSecretCommandExtension, Runner.Worker");
                    Add<T>(extensions, "GitHub.Runner.Worker.AddPathCommandExtension, Runner.Worker");
                    Add<T>(extensions, "GitHub.Runner.Worker.WarningCommandExtension, Runner.Worker");
                    Add<T>(extensions, "GitHub.Runner.Worker.ErrorCommandExtension, Runner.Worker");
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
