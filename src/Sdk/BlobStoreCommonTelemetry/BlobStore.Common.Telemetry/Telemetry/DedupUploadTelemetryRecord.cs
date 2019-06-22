using System;
using GitHub.Services.Content.Common.Telemetry;

namespace GitHub.Services.BlobStore.Common.Telemetry
{
    /// <summary>
    /// Telemetry record used to send BlobStore upload statistics.
    /// </summary>
    public class DedupUploadTelemetryRecord : BlobStoreTelemetryRecord
    {
        public long ChunksUploaded { get; private set; }
        public long CompressionBytesSaved { get; private set; }
        public long DedupUploadBytesSaved { get; private set; }
        public long LogicalContentBytesUploaded { get; private set; }
        public long TotalContentBytes { get; private set; }
        public long PhysicalContentBytesUploaded { get; private set; }

        public DedupUploadTelemetryRecord(TelemetryInformationLevel level, Uri clientBaseAddress, string eventNamePrefix, string eventNameSuffix, DedupUploadStatistics uploadStatistics)
           : base(level, clientBaseAddress, eventNamePrefix, eventNameSuffix)
        {
            if (uploadStatistics == null)
            {
                throw new ArgumentNullException("Upload Statistics cannot be null");
            }

            ChunksUploaded = uploadStatistics.ChunksUploaded;
            CompressionBytesSaved = uploadStatistics.CompressionBytesSaved;
            DedupUploadBytesSaved = uploadStatistics.DedupUploadBytesSaved;
            LogicalContentBytesUploaded = uploadStatistics.LogicalContentBytesUploaded;
            PhysicalContentBytesUploaded = uploadStatistics.PhysicalContentBytesUploaded;
            TotalContentBytes = uploadStatistics.TotalContentBytes;
        }
    }
}
