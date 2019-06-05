using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Simple helper class used to break up a stream into smaller streams
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class StreamParser
    {
        public StreamParser(Stream fileStream, int chunkSize)
        {
            m_stream = fileStream;
            m_chunkSize = chunkSize;
        }

        /// <summary>
        /// Returns total length of file.
        /// </summary>
        public long Length
        {
            get
            {
                return m_stream.Length;
            }
        }

        /// <summary>
        /// returns the next substream
        /// </summary>
        /// <returns></returns>
        public SubStream GetNextStream()
        {
            return new SubStream(m_stream, m_chunkSize);
        }

        Stream m_stream;
        int m_chunkSize;
    }

    /// <summary>
    /// Streams a subsection of a larger stream
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SubStream : Stream
    {
        public SubStream(Stream stream, int maxStreamSize)
        {
            m_startingPosition = stream.Position;
            long remainingStream = stream.Length - m_startingPosition;
            m_length = Math.Min(maxStreamSize, remainingStream);
            m_stream = stream;
        }

        public override bool CanRead
        {
            get 
            {
                return m_stream.CanRead && m_stream.Position <= this.EndingPostionOnOuterStream;
            }
        }

        public override bool CanSeek
        {
            get { return m_stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get 
            {
                return m_length;
            }
        }

        public override long Position
        {
            get
            {
                return m_stream.Position - m_startingPosition;
            }
            set
            {
                if (value >= m_length)
                {
                    throw new EndOfStreamException();
                }

                m_stream.Position = m_startingPosition + value;
            }
        }

        /// <summary>
        /// Postion in larger stream where this substream starts
        /// </summary>
        public long StartingPostionOnOuterStream
        {
            get
            {
                return m_startingPosition;
            }
        }

        /// <summary>
        /// Postion in larger stream where this substream ends
        /// </summary>
        public long EndingPostionOnOuterStream
        {
            get
            {
                return m_startingPosition + m_length - 1;
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // check that the read is only in our substream
            count = EnsureLessThanOrEqualToRemainingBytes(count);

            return m_stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // check that the read is only in our substream
            count = EnsureLessThanOrEqualToRemainingBytes(count);

            return m_stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin && 0 <= offset && offset < m_length)
            {
                return m_stream.Seek(offset + m_startingPosition, origin);
            }
            else if (origin == SeekOrigin.End && 0 >= offset && offset > -m_length)
            {
                return m_stream.Seek(offset - ((m_stream.Length-1) - this.EndingPostionOnOuterStream), origin);
            }
            else if (origin == SeekOrigin.Current && (offset + m_stream.Position) >= this.StartingPostionOnOuterStream && (offset + m_stream.Position) < this.EndingPostionOnOuterStream)
            {
                return m_stream.Seek(offset, origin);
            }

            throw new ArgumentException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        private int EnsureLessThanOrEqualToRemainingBytes(int numBytes)
        {
            long remainingBytesInStream = m_length - this.Position;
            if (numBytes > remainingBytesInStream)
            {
                numBytes = Convert.ToInt32(remainingBytesInStream);
            }
            return numBytes;
        }

        private long m_length;
        private long m_startingPosition;
        private Stream m_stream;
    }

}
