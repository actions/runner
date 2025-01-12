using System.Collections.Generic;
using GitHub.DistributedTask.WebApi;

namespace Sdk.Pipelines
{
    public interface JobItemFacade {
        TaskResult? Status { get; }
        bool Success { get; }
        bool SucceededOrFailed { get; }
        bool Failure { get; }
        bool TryGetDependency(string name, out JobItemFacade jobItem);
    }
}