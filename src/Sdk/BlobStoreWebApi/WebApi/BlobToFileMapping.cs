using GitHub.Services.BlobStore.Common;
using System;

namespace GitHub.Services.BlobStore.WebApi
{
    /// <summary>
    /// Helper class to capture the mapping between a local file path and a blob id
    /// </summary>
    public class BlobToFileMapping
    {
        /// <summary>
        /// The path of the represented item in its associated container, if any. Mostly used for error reporting.
        /// </summary>
        public string ItemPath { get; set; }

        /// <summary>
        /// The path of the represented item on disk in the client system.  This is the source or the destination
        /// depending on the scenario (upload or download, respectively).
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The globally unique descriptor of the blob associated with this item
        /// </summary>
        public BlobIdentifier BlobId { get; set; }

        /// <summary>
        /// Gets or sets the type of the BLOB.
        /// </summary>
        /// <value>
        /// The type of the BLOB.
        /// </value>
        public FileBlobType BlobType { get; set; } = FileBlobType.File;

        /// <summary>
        /// URI from which the blob may be downloaded.
        /// </summary>
        public Uri DownloadUri { get; set; }

        /// <summary>
        /// The length of this file.
        /// </summary>
        public long? FileLength { get; set; }

        /// <summary>
        /// A lambda function that can be called to retrieve the content - possibly from the local cache.
        /// </summary>
        public GetDedupAsyncFunc DedupGetter { get; set; }
    }
}
