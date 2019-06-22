namespace GitHub.Services.BlobStore.Common
{
    /// <summary>
    /// File Blob Type.
    /// </summary>
    public enum FileBlobType
    {
        /// <summary>
        /// Moniker for the file blob type.
        /// </summary>
        File,

        /// <summary>
        /// Moniker for emptydirectory blob type.
        /// </summary>
        EmptyDirectory,
    }

    /// <summary>
    /// Class encapsulates all the descriptor related constants that may appear in the manifest etc.
    /// </summary>
    public static class FileBlobDescriptorConstants
    {
        public const long MaxFileSizeFileDedup = 1024L * 1024 * 1024 * 100; // 100 GB.

        public const char PathIdentifierSeperator = '?';

        public static readonly string EmptyDirectoryEndingPattern = $"{System.IO.Path.DirectorySeparatorChar}.";

        public const string EmptyDirectoryUriEndingPattern = "/.";      

        public static readonly BlobIdentifier EmptyDirectoryChunkBlobIdentifier = ChunkBlobHasher.Instance.OfNothing;
    }
}
