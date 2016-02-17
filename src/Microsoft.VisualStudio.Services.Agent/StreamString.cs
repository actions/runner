// Defines the data protocol for reading and writing strings on our stream
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    public class StreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public async Task<Int32> ReadInt32Async(CancellationToken cancellationToken)
        {
            byte[] readBytes = new byte[4];
            int dataread = 0;            
            while (4 - dataread > 0 && (!cancellationToken.IsCancellationRequested))
            {
                Task<int> op = ioStream.ReadAsync(readBytes, dataread, 4 - dataread, cancellationToken);
                int newData = 0;
                try
                {
                    newData = await op.WithCancellation(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                dataread += newData;
                if (0 == newData)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(readBytes);
            }
            return BitConverter.ToInt32(readBytes, 0);
        }

        public async Task WriteInt32Async(Int32 value, CancellationToken cancellationToken)
        {
            byte[] int32Bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(int32Bytes);
            }
            Task op = ioStream.WriteAsync(int32Bytes, 0, 4, cancellationToken);            
            try
            {
                await op.WithCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw new TaskCanceledException();
            }
        }

        const Int32 MAX_STRING_SIZE = 50*1000000;

        public async Task<string> ReadStringAsync(CancellationToken cancellationToken)
        {            
            Int32 len = await ReadInt32Async(cancellationToken);            
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            if (len <= 0 || len > MAX_STRING_SIZE)
            {                
                throw new InvalidDataException();
            }

            byte[] inBuffer = new byte[len];
            int dataread = 0;
            while (len - dataread > 0 && (!cancellationToken.IsCancellationRequested))
            {
                Task<int> op = ioStream.ReadAsync(inBuffer, dataread, len - dataread, cancellationToken);
                int newData = 0;
                try
                {
                    newData = await op.WithCancellation(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                dataread += newData;
                if (0 == newData)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            return streamEncoding.GetString(inBuffer);
        }

        public async Task<int> WriteStringAsync(string outString, CancellationToken cancellationToken)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            Int32 len = outBuffer.Length;
            if (len > MAX_STRING_SIZE)
            {
                throw new ArgumentOutOfRangeException();
            }
            await WriteInt32Async(len, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }            
            Task op = ioStream.WriteAsync(outBuffer, 0, len, cancellationToken);
            try
            {
                await op.WithCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {                
            }
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }            
            op = ioStream.FlushAsync(cancellationToken);
            try
            {
                await op.WithCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw new TaskCanceledException();
            }
            return outBuffer.Length + 4;
        }        
    }

}