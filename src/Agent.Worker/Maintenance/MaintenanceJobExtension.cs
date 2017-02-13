using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Maintenance
{
    public interface IMaintenanceServiceProvider : IExtension
    {
        string MaintenanceDescription { get; }
        void RunMaintenanceOperation(IExecutionContext context);
    }

    public sealed class MaintenanceJobExtension : AgentService, IJobExtension
    {
        public Type ExtensionType => typeof(IJobExtension);
        public string HostType => "poolmaintenance";
        public IStep PrepareStep { get; private set; }
        public IStep FinallyStep { get; private set; }

        public MaintenanceJobExtension()
        {
            PrepareStep = new JobExtensionRunner(
                runAsync: PrepareAsync,
                continueOnError: false,
                critical: true,
                displayName: StringUtil.Loc("Maintenance"),
                enabled: true,
                @finally: false);
        }

        public string GetRootedPath(IExecutionContext context, string path)
        {
            return path;
        }

        public void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath)
        {
            sourcePath = localPath;
            repoName = string.Empty;
        }

        private Task PrepareAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(PrepareStep, nameof(PrepareStep));
            ArgUtil.NotNull(PrepareStep.ExecutionContext, nameof(PrepareStep.ExecutionContext));
            IExecutionContext executionContext = PrepareStep.ExecutionContext;
            var extensionManager = HostContext.GetService<IExtensionManager>();
            var maintenanceServiceProviders = extensionManager.GetExtensions<IMaintenanceServiceProvider>();
            if (maintenanceServiceProviders != null && maintenanceServiceProviders.Count > 0)
            {
                foreach (var maintenanceProvider in maintenanceServiceProviders)
                {
                    // all maintenance operations should be best effort.
                    executionContext.Section(StringUtil.Loc("StartMaintenance", maintenanceProvider.MaintenanceDescription));
                    try
                    {
                        maintenanceProvider.RunMaintenanceOperation(executionContext);
                    }
                    catch (Exception ex)
                    {
                        executionContext.Error(ex);
                    }

                    executionContext.Section(StringUtil.Loc("FinishMaintenance", maintenanceProvider.MaintenanceDescription));
                }
            }

            return Task.CompletedTask;
        }
    }
}