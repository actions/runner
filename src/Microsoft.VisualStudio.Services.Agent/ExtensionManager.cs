using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(ExtensionManager))]
    public interface IExtensionManager : IAgentService
    {
        List<T> GetExtensions<T>() where T : class, IExtension;
    }

    public class ExtensionManager : AgentService, IExtensionManager
    {
        private readonly object _cacheLock = new object();
        private IDictionary<Type, List<IExtension>> _cache;

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
            if (_cache == null)
            {
                lock (_cacheLock)
                {
                    if (_cache == null)
                    {
                        var instance = new ConcurrentDictionary<Type, List<IExtension>>();

                        // Add job extensions:
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildJobExtension, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Release.ReleaseJobExtension, Agent.Worker");

                        // Add command extensions:
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.TaskCommands, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildCommands, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Build.ArtifactCommands, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.ResultsCommands, Agent.Worker");

                        // Add source provider extensions:
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Build.GitSourceProvider, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Build.GitHubSourceProvider, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Build.SvnSourceProvider, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsGitSourceProvider, Agent.Worker");
#if WINDOWS
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsVCWindowsSourceProvider, Agent.Worker");
#elif OS_LINUX || OS_OSX
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsVCTeeSourceProvider, Agent.Worker");
#endif
                        // Add Release Artifact extensions:
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.BuildArtifact, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.JenkinsArtifact, Agent.Worker");

                        // Add Result reader extensions:
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.JUnitResultReader, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.NUnitResultReader, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.TrxResultReader, Agent.Worker");
                        Add(instance, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.XUnitResultReader, Agent.Worker");

                        _cache = instance;
                    }
                }
            }
        }

        private void Add(ConcurrentDictionary<Type, List<IExtension>> extensions, string assemblyQualifiedName)
        {
            Trace.Verbose($"Creating instance: {assemblyQualifiedName}");
            Type type = Type.GetType(assemblyQualifiedName, throwOnError: true);
            Add(extensions, Activator.CreateInstance(type) as IExtension);
        }

        private void Add(ConcurrentDictionary<Type, List<IExtension>> extensions, IExtension extension)
        {
            Trace.Verbose($"Registering extension: {extension.GetType().FullName}");
            List<IExtension> list = extensions.GetOrAdd(extension.ExtensionType, new List<IExtension>());
            extension.Initialize(HostContext);
            list.Add(extension);
        }
    }
}