using GitHub.Services.Common;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.BlobStore.Common
{
    public static class StreamExtensions
    {
        public static async Task ReadToEntireBufferAsync(this Stream content, ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            int bytesRead = await content.ReadToBufferAsync(buffer,cancellationToken).ConfigureAwait(false);

            if (bytesRead != buffer.Count)
            {
                throw new EndOfStreamException();
            }
        }

        public static async Task<int> ReadToBufferAsync(this Stream content, ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            int bytesReadTotal = 0;
            int bufferOffset = buffer.Offset;
            int bytesToRead = buffer.Count;

            while (bytesToRead > 0)
            {
                // As of 8/20/2018, when called via ManagedParallelBlobDownloader, which uses GetStreamThroughAzureBlobs, the Stream here is a Microsoft.WindowsAzure.Storage.Blob.BlobReadStream.
                int bytesRead = await content.ReadAsync(
                    buffer.Array,
                    bufferOffset,
                    bytesToRead,
                    cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    break;
                }

                bytesReadTotal += bytesRead;
                bufferOffset += bytesRead;
                bytesToRead -= bytesRead;
            }

            return bytesReadTotal;
        }


        public static Stream WrapWithCancellationEnforcement(this Stream content, string name)
        {
            return new EnforcedCancellationStream(content, name);
        }

        private class EnforcedCancellationStream : Stream
        {
            private readonly Stream baseStream;
            private readonly string name;

            public EnforcedCancellationStream(Stream baseStream, string name)
            {
                this.baseStream = baseStream;
                this.name = name;
            }

            public override bool CanRead => baseStream.CanRead;

            public override bool CanSeek => baseStream.CanSeek;

            public override bool CanWrite => baseStream.CanWrite;

            public override long Length => baseStream.Length;

            public override long Position { get => baseStream.Position; set => baseStream.Position = value; }

            public override void Flush()
            {
                baseStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return baseStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return baseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                baseStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                baseStream.Write(buffer, offset, count);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return baseStream.ReadAsync(buffer, offset, count, cancellationToken)
                    .EnforceCancellation(cancellationToken, () => $"Timed out waiting for ReadAsync from '{this.name}'.");
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return baseStream.WriteAsync(buffer, offset, count, cancellationToken)
                    .EnforceCancellation(cancellationToken, () => $"Timed out waiting for WriteAsync from '{this.name}'.");
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                return baseStream.CopyToAsync(destination, bufferSize, cancellationToken)
                    .EnforceCancellation(cancellationToken, () => $"Timed out waiting for CopyToAsync from '{this.name}'.");
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return baseStream.FlushAsync(cancellationToken)
                    .EnforceCancellation(cancellationToken, () => $"Timed out waiting for FlushAsync from '{this.name}'.");
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    baseStream.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
