using GitHub.Services.BlobStore.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.BlobStore.WebApi
{
    public interface IDedupManifestArtifactClient
    {
        Task DownloadAsync(
            DedupIdentifier manifestId,
            string targetDirectory,
            CancellationToken cancellationToken);

        Task DownloadAsync(
            DownloadDedupManifestArtifactOptions downloadPipelineArtifactOptions,
            CancellationToken cancellationToken);

        Task DownloadAsyncWithManifestPath(
            string fullManifestPath,
            string targetDirectory,
            Uri proxyUri,
            CancellationToken cancellationToken);

        Task DownloadAsyncWithManifestPath(
            DownloadDedupManifestArtifactOptions downloadPipelineArtifactOptions,
            CancellationToken cancellationToken);

        Task DownloadFileToPathAsync(
            DedupIdentifier dedupId,
            string fullFileOutputPath,
            Uri proxyUri,
            CancellationToken cancellationToken);

        Task<PublishResult> PublishAsync(
            string fullPath,
            ArtifactPublishOptions artifactPublishOptions,
            string manifestFileOutputPath,
            CancellationToken cancellationToken);
    }

    // BuildDropManager was the old name for pipeline artifacts, left here for compatibility, we should eventually deprecate this after a while
    public interface IBuildDropManager : IDedupManifestArtifactClient
    {
    }
}
