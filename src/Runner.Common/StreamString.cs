// Defines the data protocol for reading and writing strings on our stream
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Runner.Common
{
    public class StreamString
    {
        private Stream _ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            _ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public async Task<Int32> ReadInt32Async(CancellationToken cancellationToken)
        {
            byte[] readBytes = new byte[sizeof(Int32)];
            int dataread = 0;
            while (sizeof(Int32) - dataread > 0 && (!cancellationToken.IsCancellationRequested))
            {
                Task<int> op = _ioStream.ReadAsync(readBytes, dataread, sizeof(Int32) - dataread, cancellationToken);
                int newData = 0;
                newData = await op.WithCancellation(cancellationToken);
                dataread += newData;
                if (0 == newData)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return BitConverter.ToInt32(readBytes, 0);
        }

        public async Task WriteInt32Async(Int32 value, CancellationToken cancellationToken)
        {
            byte[] int32Bytes = BitConverter.GetBytes(value);
            Task op = _ioStream.WriteAsync(int32Bytes, 0, sizeof(Int32), cancellationToken);
            await op.WithCancellation(cancellationToken);
        }

        const int MaxStringSize = 50 * 1000000;

        public async Task<string> ReadStringAsync(CancellationToken cancellationToken)
        {
            Int32 len = await ReadInt32Async(cancellationToken);
            if (len == 0)
            {
                return string.Empty;
            }
            if (len < 0 || len > MaxStringSize)
            {
                throw new InvalidDataException();
            }

            byte[] inBuffer = new byte[len];
            int dataread = 0;
            while (len - dataread > 0 && (!cancellationToken.IsCancellationRequested))
            {
                Task<int> op = _ioStream.ReadAsync(inBuffer, dataread, len - dataread, cancellationToken);
                int newData = 0;
                newData = await op.WithCancellation(cancellationToken);
                dataread += newData;
                if (0 == newData)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            return streamEncoding.GetString(inBuffer);
        }

        public async Task WriteStringAsync(string outString, CancellationToken cancellationToken)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            Int32 len = outBuffer.Length;
            if (len > MaxStringSize)
            {
                throw new ArgumentOutOfRangeException();
            }

            await WriteInt32Async(len, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Task op = _ioStream.WriteAsync(outBuffer, 0, len, cancellationToken);
            await op.WithCancellation(cancellationToken);
            op = _ioStream.FlushAsync(cancellationToken);
            await op.WithCancellation(cancellationToken);
        }
    }
}
