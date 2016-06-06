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
                case "Microsoft.VisualStudio.Services.Agent.Listener.Capabilities.ICapabilitiesProvider":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Listener.Capabilities.AgentCapabilitiesProvider, Agent.Listener");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Listener.Capabilities.EnvironmentCapabilitiesProvider, Agent.Listener");
#if OS_LINUX || OS_OSX
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Listener.Capabilities.NixCapabilitiesProvider, Agent.Listener");
#elif OS_WINDOWS
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Listener.Capabilities.PowerShellCapabilitiesProvider, Agent.Listener");
#endif
                    break;
                // Worker job extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.IJobExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildJobExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.ReleaseJobExtension, Agent.Worker");
                    break;
                // Worker command extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.IWorkerCommandExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TaskCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.ArtifactCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CodeCoverageCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.ResultsCommandExtension, Agent.Worker");
                    break;
                // Worker build source providers.
                case "Microsoft.VisualStudio.Services.Agent.Worker.Build.ISourceProvider":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.GitSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.GitHubSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.SvnSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsGitSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsVCSourceProvider, Agent.Worker");
                    break;
                // Worker release artifact extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.Release.IArtifactExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.BuildArtifact, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.JenkinsArtifact, Agent.Worker");
                    break;
                // Worker test result readers.
                case "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.IResultReader":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.JUnitResultReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.NUnitResultReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.TrxResultReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.XUnitResultReader, Agent.Worker");
                    break;
                // Worker code coverage enabler extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.ICodeCoverageEnabler":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CodeCoverageEnablerForCoberturaAnt, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CodeCoverageEnablerForCoberturaGradle, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CodeCoverageEnablerForCoberturaMaven, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CodeCoverageEnablerForJacocoAnt, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CodeCoverageEnablerForJacocoGradle, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CodeCoverageEnablerForJacocoMaven, Agent.Worker");
                    break;
                // Worker code coverage summary reader extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.ICodeCoverageSummaryReader":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.JaCoCoSummaryReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CoberturaSummaryReader, Agent.Worker");
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