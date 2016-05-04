using Microsoft.VisualStudio.Services.Agent.Util;
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
                // Worker command extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.ICommandExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TaskCommands, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildCommands, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.ArtifactCommands, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.ResultsCommands, Agent.Worker");
                    break;
                // Worker job extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.IJobExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildJobExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.ReleaseJobExtension, Agent.Worker");
                    break;
                // Worker build source provider extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.Build.ISourceProvider":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.GitSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.GitHubSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.SvnSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsGitSourceProvider, Agent.Worker");
#if WINDOWS
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsVCWindowsSourceProvider, Agent.Worker");
#elif OS_LINUX || OS_OSX
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsVCTeeSourceProvider, Agent.Worker");
#endif
                    break;
                // Worker release artifact extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.Release.IArtifactExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.BuildArtifact, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.JenkinsArtifact, Agent.Worker");
                    break;
                // Worker test result reader extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.IResultReader":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.JUnitResultReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.NUnitResultReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.TrxResultReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.XUnitResultReader, Agent.Worker");
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