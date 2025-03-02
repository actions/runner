using System.Threading;

namespace Sdk.Pipelines
{
    public class ExecutionContext
    {
        public CancellationToken Cancelled { get; set; }
        public JobItemFacade JobContext { get; set; }
    }
}
