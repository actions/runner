using System;
using GitHub.Services.Content.Common;

namespace GitHub.Services.BlobStore.Common.Telemetry
{
    /// <summary>
    /// Stores various statistics related to BlobStore upload operations.
    /// </summary>
    public class DedupUploadStatistics
    {
        public long ChunksUploaded { get; }
        public long CompressionBytesSaved { get; }
        public long DedupUploadBytesSaved { get; }
        public long LogicalContentBytesUploaded { get; }
        public long PhysicalContentBytesUploaded { get; }
        public long TotalContentBytes { get; }

        private string FormattedUploadStatistics
        {
            get
            {
                return
                    $"Total Content: {NumberConversionHelper.ConvertBytesToMegabytes(TotalContentBytes):N1} MB{Environment.NewLine}" +
                    $"Physical Content Uploaded: {NumberConversionHelper.ConvertBytesToMegabytes(PhysicalContentBytesUploaded):N1} MB{Environment.NewLine}" +
                    $"Logical Content Uploaded: {NumberConversionHelper.ConvertBytesToMegabytes(LogicalContentBytesUploaded):N1} MB{Environment.NewLine}" +
                    $"Compression Saved: {NumberConversionHelper.ConvertBytesToMegabytes(CompressionBytesSaved):N1} MB{Environment.NewLine}" +
                    $"Deduplication Saved: {NumberConversionHelper.ConvertBytesToMegabytes(DedupUploadBytesSaved):N1} MB{Environment.NewLine}" +
                    $"Number of Chunks Uploaded: {ChunksUploaded:N0}{Environment.NewLine}";
            }
        }

        public DedupUploadStatistics(long chunksUploaded, long compressionBytesSaved, long dedupUploadBytesSaved, long logicalContentBytesUploaded, long physicalContentBytesUploaded)
        {
            ChunksUploaded = chunksUploaded;
            CompressionBytesSaved = compressionBytesSaved;
            DedupUploadBytesSaved = dedupUploadBytesSaved;
            LogicalContentBytesUploaded = logicalContentBytesUploaded;
            PhysicalContentBytesUploaded = physicalContentBytesUploaded;
            TotalContentBytes = logicalContentBytesUploaded + dedupUploadBytesSaved;
        }

        /// <summary>
        /// Provides friendly access to Upload Statistics properties.
        /// </summary>
        /// <returns>Formatted upload statistics.</returns>
        public string AsString() => FormattedUploadStatistics;
    }
}
