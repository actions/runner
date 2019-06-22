using System;

namespace GitHub.Services.BlobStore.Common
{
    // subset of Blob2FileMapping. Should this be what is passed around in public apis? Looking for better comment from Artifact crew.
    // Or should this be ignored and alwasy use blogtofilemapping so we can
    public interface IDropFile
    {
        string RelativePath { get; }

        /// <summary>
        /// Size of file in bytes
        /// </summary>
        long? FileSize { get; }

        BlobIdentifier BlobIdentifier { get; }
    }

    public interface IDropFileWithDownloadUri : IDropFile
    {
        Uri DownloadUri { get; }
    }
}
