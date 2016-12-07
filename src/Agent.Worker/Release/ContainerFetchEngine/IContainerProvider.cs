using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    // NOTE: FetchEngine specific interface shouldn't take dependency on Agent code.
    public interface IContainerProvider
    {
        Task<IEnumerable<ContainerItem>> GetItemsAsync();
        Task<Stream> GetFileTask(ContainerItem ticketedItem, CancellationToken token);
    }
}
