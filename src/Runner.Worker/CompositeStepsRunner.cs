using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Expressions;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(StepsRunner))]
    public interface ICompositeStepsRunner : IRunnerService
    {
        Task RunAsync(IExecutionContext Context);
    }


    public sealed class CompositeStepsRunner : RunnerService, ICompositeStepsRunner
    {
        public async Task RunAsync(IExecutionContext actionContext)
        {
            ArgUtil.NotNull(actionContext, nameof(actionContext));
            ArgUtil.NotNull(actionContext.CompositeSteps, nameof(actionContext.CompositeSteps));

            // TODO: Add CompositeSteps attribute to ExecutionContext and replace that with this

            // Add status logic here (start when composite action steps start)
            // End when clean up is done

            // TODO: Remove Composite Action logic from StepsRunner and move it here

            // Namely, we need to move the envContext logic!

    }

}