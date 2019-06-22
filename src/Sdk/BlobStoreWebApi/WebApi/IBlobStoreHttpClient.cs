using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.Common;
using GitHub.Services.Content.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    public interface IBlobStoreHttpClient : IArtifactHttpClient, IDisposable
    {
        /// <summary>
        /// Get a file from the content service using a the supplied blob identifier.
        /// </summary>
        /// <param name="blobId">The globally unique identifier for the blob to download</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that returns the stream of bytes requested</returns>
        Task<Stream> GetBlobAsync(BlobIdentifier blobId, CancellationToken cancellationToken);

        Task<PreauthenticatedUri> GetDownloadUriAsync(
            BlobIdWithHeaders blobId,
            CancellationToken cancellationToken);

        Task<IDictionary<BlobIdentifier, PreauthenticatedUri>> GetDownloadUrisAsync(
            IEnumerable<BlobIdentifier> blobIds,
            EdgeCache edgeCache,
            CancellationToken cancellationToken,
            DateTimeOffset? expiryTime = null);

        Task PutBlobBlockAsync(
            BlobIdentifier blobId,
            byte[] blockBuffer,
            int blockLength,
            CancellationToken cancellationToken);

        Task PutBlobBlockAsync(
            BlobIdentifier blobId,
            BlobBlockHash blockHash,
            byte[] blockBuffer,
            int blockLength,
            CancellationToken cancellationToken);

        Task PutSingleBlockBlobAndReferenceAsync(
            BlobIdentifier blobId,
            byte[] blockBuffer,
            int blockLength,
            BlobReference reference,
            CancellationToken cancellationToken);

        Task RemoveReferencesAsync(IDictionary<BlobIdentifier, IEnumerable<IdBlobReference>> referencesGroupedByBlobIds);

        Task<IDictionary<BlobIdentifier, IEnumerable<BlobReference>>> TryReferenceAsync(
            IDictionary<BlobIdentifier, IEnumerable<BlobReference>> referencesGroupedByBlobIds,
            CancellationToken cancellationToken);

        Task<IDictionary<BlobIdentifier, IEnumerable<BlobReference>>> TryReferenceWithBlocksAsync(
            IDictionary<BlobIdentifierWithBlocks, IEnumerable<BlobReference>> referencesGroupedByBlobIds,
            CancellationToken cancellationToken);
        
        Task<BlobIdentifierWithBlocks> UploadBlocksForBlobAsync(BlobIdentifier blobId, Stream blobStream, CancellationToken cancellationToken);

        Task<IEnumerable<BlobIdentifierWithBlocks>> UploadBlocksForBlobsAsync(
            IEnumerable<BlobToUriMapping> pathToUriMappings,
            CancellationToken cancellationToken);
    }

    public static class IBlobStoreHttpClientExtensions
    {
        private static readonly Task CompletedTask = Task.FromResult(0);

        public static async Task<bool> TryReferenceWithBlocksAsync(this IBlobStoreHttpClient blobStore, BlobIdentifierWithBlocks blobIdAndBlocks, BlobReference reference, CancellationToken cancellationToken)
        {
            IDictionary<BlobIdentifier, IEnumerable<BlobReference>> failedReferences = await blobStore.TryReferenceWithBlocksAsync(
                new Dictionary<BlobIdentifierWithBlocks, IEnumerable<BlobReference>>()
                {
                    { blobIdAndBlocks, new[] { reference } }
                },
                cancellationToken).ConfigureAwait(false);

            return !failedReferences.Any();
        }

        public static async Task<BlobIdentifierWithBlocks> UploadFileAsync(this IBlobStoreHttpClient client, BlobIdentifier blobIdentifier, String filename, CancellationToken cancellationToken)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(filename, "filename");

            using (var stream = FileStreamUtils.OpenFileStreamForAsync(filename, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            {
                return await client.UploadBlocksForBlobAsync(blobIdentifier, stream, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
