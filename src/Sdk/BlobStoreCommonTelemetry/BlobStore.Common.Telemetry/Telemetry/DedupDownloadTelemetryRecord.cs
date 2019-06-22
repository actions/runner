using System;
using GitHub.Services.Content.Common.Telemetry;

namespace GitHub.Services.BlobStore.Common.Telemetry
{
    /// <summary>
    /// Telemetry record used to send BlobStore download statistics.
    /// </summary>
    public class DedupDownloadTelemetryRecord : BlobStoreTelemetryRecord
    {
        public long ChunksDownloaded { get; private set; }
        public long CompressionBytesSaved { get; private set; }
        public long DedupDownloadBytesSaved { get; private set; }
        public long NodesDownloaded { get; private set; }
        public long PhysicalContentBytesDownloaded { get; private set; }
        public long TotalContentBytes { get; private set; }

        public DedupDownloadTelemetryRecord(TelemetryInformationLevel level, Uri clientBaseAddress, string eventNamePrefix, string eventNameSuffix, DedupDownloadStatistics downloadStatistics)
           : base(level, clientBaseAddress, eventNamePrefix, eventNameSuffix)
        {
            if (downloadStatistics == null)
            {
                throw new ArgumentNullException("Download Statistics cannot be null");
            }

            ChunksDownloaded = downloadStatistics.ChunksDownloaded;
            CompressionBytesSaved = downloadStatistics.CompressionBytesSaved;
            DedupDownloadBytesSaved = downloadStatistics.DedupDownloadBytesSaved;
            NodesDownloaded = downloadStatistics.NodesDownloaded;
            PhysicalContentBytesDownloaded = downloadStatistics.PhysicalContentBytesDownloaded;
            TotalContentBytes = downloadStatistics.TotalContentBytes;
        }
    }
}
