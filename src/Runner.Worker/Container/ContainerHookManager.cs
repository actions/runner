using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Container
{
    [ServiceLocator(Default = typeof(ContainerHookManager))]
    public interface IContainerHookManager : IRunnerService
    {
        Task<int> JobPrepareAsync(IExecutionContext context);
        Task<int> JobCleanupAsync(IExecutionContext context, List<ContainerInfo> containers);
        Task<int> StepContainerAsync(IExecutionContext context);
        Task<int> StepScriptAsync(IExecutionContext context);
    }

    public class ContainerHookManager : RunnerService, IContainerHookManager
    {
        public async Task<int> JobCleanupAsync(IExecutionContext context, List<ContainerInfo> containers)
        {
            Trace.Entering();
            await Task.FromResult(0);
            throw new NotImplementedException();
        }

        public async Task<int> JobPrepareAsync(IExecutionContext context)
        {
            Trace.Entering();
            await Task.FromResult(0);
            throw new NotImplementedException();
        }

        public async Task<int> StepContainerAsync(IExecutionContext context)
        {
            Trace.Entering();
            await Task.FromResult(0);
            throw new NotImplementedException();
        }

        public async Task<int> StepScriptAsync(IExecutionContext context)
        {
            Trace.Entering();
            await Task.FromResult(0);
            throw new NotImplementedException();
        }
    }
}