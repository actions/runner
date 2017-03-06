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
        public IStep PreJobStep { get; private set; }
        public IStep ExecutionStep { get; private set; }
        public IStep PostJobStep { get; private set; }

        public MaintenanceJobExtension()
        {
            ExecutionStep = new JobExtensionRunner(
                runAsync: MaintainAsync,
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

        private Task MaintainAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(PreJobStep, nameof(PreJobStep));
            ArgUtil.NotNull(PreJobStep.ExecutionContext, nameof(PreJobStep.ExecutionContext));
            IExecutionContext executionContext = PreJobStep.ExecutionContext;
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