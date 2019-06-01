using Microsoft.VisualStudio.Services.Common;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Performs a PATCH request to uri passing in the content.
        /// </summary>
        /// <param name="client">HttpClient being extended</param>
        /// <param name="uri">The target uri for the PATCH call</param>
        /// <param name="content">The message content</param>
        /// <returns>A Task which executes the PATCH call with a HttpResponseMessage result</returns>
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, String uri, HttpContent content)
        {
            return PatchAsync(client, uri, content, new CancellationToken(false));
        }

        /// <summary>
        /// Performs a PATCH request to uri passing in the content.
        /// </summary>
        /// <param name="client">HttpClient being extended</param>
        /// <param name="uri">The target uri for the PATCH call</param>
        /// <param name="content">The message content</param>
        /// <param name="cancellationToken">CancellationToken to cancel the task</param>
        /// <returns>A Task which executes the PATCH call with a HttpResponseMessage result</returns>
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, String uri, HttpContent content, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), uri)
            {
                Content = content
            };

            return client.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Performs a DELETE request to uri passing in the content.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="uri"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> DeleteAsync(this HttpClient client, String uri, HttpContent content, CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("DELETE"), uri)
            {
                Content = content
            };

            return client.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Downloads the content of a file and copies it to the specified stream if the request succeeds. 
        /// </summary>
        /// <param name="client">Http client.</param>
        /// <param name="stream">Stream to write file content to.</param>
        /// <param name="requestUri">Download uri.</param>
        /// <returns>Http response message.</returns>
        public async static Task<HttpResponseMessage> DownloadFileFromTfsAsync(this HttpClient client, Uri requestUri, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(stream, "stream");
            ArgumentUtility.CheckForNull(requestUri, "requestUri");

            HttpResponseMessage response = await client.GetAsync(requestUri.ToString(), cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
            {
                Boolean decompress;
                if (VssStringComparer.ContentType.Equals(response.Content.Headers.ContentType.MediaType, "application/octet-stream")
                    || VssStringComparer.ContentType.Equals(response.Content.Headers.ContentType.MediaType, "application/zip")
                    || VssStringComparer.ContentType.Equals(response.Content.Headers.ContentType.MediaType, "application/xml")
                    || VssStringComparer.ContentType.Equals(response.Content.Headers.ContentType.MediaType, "text/plain"))
                {
                    decompress = false;
                }
                else if (VssStringComparer.ContentType.Equals(response.Content.Headers.ContentType.MediaType, "application/gzip"))
                {
                    decompress = true;
                }
                else
                {
                    throw new Exception(WebApiResources.UnsupportedContentType(response.Content.Headers.ContentType.MediaType));
                }

                using (DownloadStream downloadStream = new DownloadStream(stream, decompress, response.Content.Headers.ContentMD5))
                {
                    await response.Content.CopyToAsync(downloadStream).ConfigureAwait(false);
                    downloadStream.ValidateHash();
                }
            }

            return response;
        }

        /// <summary>
        /// Wraps the download stream to provide hash calculation and content decompression.
        /// </summary>
        private class DownloadStream : Stream
        {
            public DownloadStream(Stream stream, Boolean decompress, Byte[] hashValue)
            {
                m_stream = stream;
                m_decompress = decompress;
                m_expectedHashValue = hashValue;

                if (hashValue != null && hashValue.Length == 16)
                {
                    m_expectedHashValue = hashValue;
                    m_hashProvider = MD5Utility.TryCreateMD5Provider();
                }
            }

            public override Boolean CanRead
            {
                get
                {
                    return m_stream.CanRead;
                }
            }

            public override Boolean CanSeek
            {
                get
                {
                    return m_stream.CanSeek;
                }
            }

            public override Boolean CanWrite
            {
                get
                {
                    return m_stream.CanWrite;
                }
            }

            public override Int64 Length
            {
                get
                {
                    return m_stream.Length;
                }
            }

            public override Int64 Position
            {
                get
                {
                    return m_stream.Position;
                }
                set
                {
                    m_stream.Position = value;
                }
            }

            protected override void Dispose(Boolean disposing)
            {
                // Don't dispose the inner stream here because we don't own it!

                if (m_hashProvider != null)
                {
                    m_hashProvider.Dispose();
                    m_hashProvider = null;
                }

                base.Dispose(disposing);
            }

            public override void Flush()
            {
                m_stream.Flush();
            }

            public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
            {
                return m_stream.Read(buffer, offset, count);
            }

            public override Int64 Seek(Int64 offset, SeekOrigin origin)
            {
                return m_stream.Seek(offset, origin);
            }

            public override void SetLength(Int64 value)
            {
                m_stream.SetLength(value);
            }

            public override void Write(Byte[] buffer, Int32 offset, Int32 count)
            {
                Byte[] outputBuffer;
                Int32 outputOffset;
                Int32 outputCount;

                Transform(buffer, offset, count, out outputBuffer, out outputOffset, out outputCount);
                m_stream.Write(outputBuffer, outputOffset, outputCount);
            }

            public override IAsyncResult BeginWrite(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state)
            {
                Byte[] outputBuffer;
                Int32 outputOffset;
                Int32 outputCount;

                Transform(buffer, offset, count, out outputBuffer, out outputOffset, out outputCount);
                return m_stream.BeginWrite(outputBuffer, outputOffset, outputCount, callback, state);
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                m_stream.EndWrite(asyncResult);
            }

            public override Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
            {
                Byte[] outputBuffer;
                Int32 outputOffset;
                Int32 outputCount;

                Transform(buffer, offset, count, out outputBuffer, out outputOffset, out outputCount);
                return m_stream.WriteAsync(outputBuffer, outputOffset, outputCount, cancellationToken);
            }

            public override void WriteByte(Byte value)
            {
                Write(new Byte[] { value }, 0, 1);
            }

            public void ValidateHash()
            {
                if (m_hashProvider != null)
                {
                    m_hashProvider.TransformFinalBlock(new Byte[0], 0, 0);

                    if (!ArrayUtility.Equals(m_hashProvider.Hash, m_expectedHashValue))
                    {
                        throw new Exception(WebApiResources.DownloadCorrupted());
                    }
                }
            }

            private void Transform(
                Byte[] buffer,
                Int32 offset,
                Int32 count,
                out Byte[] outputBuffer,
                out Int32 outputOffset,
                out Int32 outputCount)
            {
                if (m_decompress)
                {
                    // TODO: Consider using one GZipStream to decompress all the buffers to 
                    // improve performance. See Version Control client download implementation.
                    using (MemoryStream ms = new MemoryStream(buffer, offset, count))
                    using (GZipStream gs = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        int decompressedBufferSize = 4096;
                        Byte[] decompressedBuffer = new Byte[decompressedBufferSize];

                        Int32 bytesRead = 0;

                        using (MemoryStream decompressedOutput = new MemoryStream())
                        {
                            do
                            {
                                bytesRead = gs.Read(decompressedBuffer, 0, decompressedBufferSize);
                                if (bytesRead > 0)
                                {
                                    decompressedOutput.Write(decompressedBuffer, 0, bytesRead);
                                }
                            }
                            while (bytesRead > 0);

                            outputBuffer = decompressedOutput.ToArray();
                            outputOffset = 0;
                            outputCount = outputBuffer.Length;
                        }
                    }
                }
                else
                {
                    outputBuffer = buffer;
                    outputOffset = offset;
                    outputCount = count;
                }

                if (m_hashProvider != null && outputCount > 0)
                {
                    m_hashProvider.TransformBlock(outputBuffer, outputOffset, outputCount, null, 0);
                }
            }

            private Stream m_stream;
            private Boolean m_decompress;
            private MD5 m_hashProvider;
            private Byte[] m_expectedHashValue;
        }
    }
}
