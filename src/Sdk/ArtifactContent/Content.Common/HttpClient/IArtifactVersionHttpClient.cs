using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public interface IArtifactVersionHttpClient
    {
        Task<string> GetVersionAsync(CancellationToken cancellationToken);

        Task<Stream> GetClientNuPkgAsync(CancellationToken cancellationToken);

        Task<Stream> GetRemotableClientNupkgAsync(CancellationToken cancellationToken);
    }
}
