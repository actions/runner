using System.Threading;

namespace Sdk.Pipelines
{
    class ExecutionContext
    {
        public CancellationToken Cancelled { get; set; }
        public JobItemFacade JobContext { get; set; }
    }
}
