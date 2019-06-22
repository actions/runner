using System;
using GitHub.Services.Content.Common;

namespace GitHub.Services.BlobStore.Common.Telemetry
{
    /// <summary>
    /// Stores various statistics related to BlobStore download operations.
    /// </summary>
    public class DedupDownloadStatistics
    {
        public long ChunksDownloaded { get; set; }
        public long CompressionBytesSaved { get; set; }
        public long DedupDownloadBytesSaved { get; set; }
        public long NodesDownloaded { get; set; }
        public long PhysicalContentBytesDownloaded { get; set; }
        public long TotalContentBytes { get; set; }

        private string FormattedDownloadStatistics
        {
            get
            {
                return
                    $"Total Content: {NumberConversionHelper.ConvertBytesToMegabytes(TotalContentBytes):N1} MB{Environment.NewLine}" +
                    $"Physical Content Downloaded: {NumberConversionHelper.ConvertBytesToMegabytes(PhysicalContentBytesDownloaded):N1} MB{Environment.NewLine}" +
                    $"Compression Saved: {NumberConversionHelper.ConvertBytesToMegabytes(CompressionBytesSaved):N1} MB{Environment.NewLine}" +
                    $"Local Caching Saved: {NumberConversionHelper.ConvertBytesToMegabytes(DedupDownloadBytesSaved):N1} MB{Environment.NewLine}" +
                    $"Chunks Downloaded: {ChunksDownloaded:N0}{Environment.NewLine}" +
                    $"Nodes Downloaded: {NodesDownloaded:N0}{Environment.NewLine}";
            }
        }

        public DedupDownloadStatistics(long chunksDownloaded = 0, long compressionBytesSaved = 0, long dedupDownloadBytesSaved = 0, long nodesDownloaded = 0, long physicalContentBytesDownloaded = 0)
        {
            ChunksDownloaded = chunksDownloaded;
            CompressionBytesSaved = compressionBytesSaved;
            DedupDownloadBytesSaved = dedupDownloadBytesSaved;
            NodesDownloaded = nodesDownloaded;
            PhysicalContentBytesDownloaded = physicalContentBytesDownloaded;
            TotalContentBytes = physicalContentBytesDownloaded + compressionBytesSaved + dedupDownloadBytesSaved;
        }

        /// <summary>
        /// Provides friendly access to Download Statistics properties.
        /// </summary>
        /// <returns>Formatted download statistics.</returns>
        public string AsString() => FormattedDownloadStatistics;

        /// <summary>
        /// Merges additional instances DownloadStatistics with the current instance.
        /// </summary>
        /// <param name="currentStatistics">DownloadStatistics to be concatenated.</param>
        public void ConcatenateStatistics(DedupDownloadStatistics currentStatistics)
        {
            ChunksDownloaded += currentStatistics.ChunksDownloaded;
            CompressionBytesSaved += currentStatistics.CompressionBytesSaved;
            DedupDownloadBytesSaved += currentStatistics.DedupDownloadBytesSaved;
            NodesDownloaded += currentStatistics.NodesDownloaded;
            PhysicalContentBytesDownloaded += currentStatistics.PhysicalContentBytesDownloaded;
            TotalContentBytes += currentStatistics.TotalContentBytes;
        }
    }
}
