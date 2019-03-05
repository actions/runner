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
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Capabilities.EnvironmentCapabilitiesProvider, Microsoft.VisualStudio.Services.Agent");
#if OS_LINUX || OS_OSX
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Capabilities.NixCapabilitiesProvider, Microsoft.VisualStudio.Services.Agent");
#elif OS_WINDOWS
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Capabilities.PowerShellCapabilitiesProvider, Microsoft.VisualStudio.Services.Agent");
#endif
                    break;
                // Listener agent configuration providers
                case "Microsoft.VisualStudio.Services.Agent.Listener.Configuration.IConfigurationProvider":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Listener.Configuration.BuildReleasesAgentConfigProvider, Agent.Listener");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Listener.Configuration.DeploymentGroupAgentConfigProvider, Agent.Listener");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Listener.Configuration.SharedDeploymentAgentConfigProvider, Agent.Listener");
                    break;
                // Worker job extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.IJobExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildJobExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.ReleaseJobExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.DeploymentJobExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Maintenance.MaintenanceJobExtension, Agent.Worker");
                    break;
                // Worker command extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.IWorkerCommandExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TaskCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.ArtifactCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CodeCoverageCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.ResultsCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Telemetry.TelemetryCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.ReleaseCommandExtension, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.PluginInternalCommandExtension, Agent.Worker");
                    break;
                // Worker build source providers.
                case "Microsoft.VisualStudio.Services.Agent.Worker.Build.ISourceProvider":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.ExternalGitSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.GitHubSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.GitHubEnterpriseSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BitbucketSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.SvnSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsGitSourceProvider, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.TfsVCSourceProvider, Agent.Worker");
                    if (HostContext.RunMode == RunMode.Local)
                    {
                        Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.LocalRunSourceProvider, Agent.Worker");
                    }
                    break;
                // Worker release artifact extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.Release.IArtifactExtension":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.BuildArtifact, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.JenkinsArtifact, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.GitHubArtifact, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.TfsGitArtifact, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.TfsVCArtifact, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.CustomArtifact, Agent.Worker");
                    break;
                // Worker test result readers.
                case "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.IResultReader":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.JUnitResultReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.NUnitResultReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.CTestResultReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.TrxResultReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.TestResults.XUnitResultReader, Agent.Worker");
                    break;
                // Worker code coverage summary reader extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.ICodeCoverageSummaryReader":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.JaCoCoSummaryReader, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage.CoberturaSummaryReader, Agent.Worker");
                    break;
                // Worker maintenance service provider extensions.
                case "Microsoft.VisualStudio.Services.Agent.Worker.Maintenance.IMaintenanceServiceProvider":
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Build.BuildDirectoryManager, Agent.Worker");
                    Add<T>(extensions, "Microsoft.VisualStudio.Services.Agent.Worker.Release.ReleaseDirectoryManager, Agent.Worker");
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