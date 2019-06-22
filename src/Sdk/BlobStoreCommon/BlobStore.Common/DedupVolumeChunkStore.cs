using BuildXL.Cache.ContentStore.Hashing;
using BuildXL.Cache.ContentStore.Interfaces.Utils;
using Microsoft.DataDeduplication.Interop;
using GitHub.Services.Content.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.BlobStore.Common
{
    [CLSCompliant(false)]
    public static class DedupVolumeChunkStore
    {
        private static RunOnce<string, TaggedUnionValue<IDedupDataPortManager,string>> managers = 
            new RunOnce<string, TaggedUnionValue<IDedupDataPortManager, string>>(consolidateExceptions: true);

        private const int E_CLASSNOTREG = unchecked((int)0x80040154);
        private const int CO_E_SERVER_EXEC_FAILURE = unchecked((int)0x80080005);
        private const int E_NOINTERFACE = unchecked((int)0x80004002);
        private const int E_ACCESSDENIED = unchecked((int)0x80070005);

        private static async Task<TaggedUnionValue<IDedupDataPortManager, string>> CreateForVolumeAsync(string volume, CancellationToken cancellationToken, Action<string> logger)
        {
            IDedupDataPortManager dataPortManager;

            // https://blogs.msdn.microsoft.com/adioltean/2005/06/24/when-cocreateinstance-returns-0x80080005-co_e_server_exec_failure/
            int attemptsLeft = 5;
            while (true)
            {
                try
                {
                    dataPortManager = (IDedupDataPortManager)(Activator.CreateInstance(Type.GetTypeFromCLSID(Guid.Parse("8f107207-1829-48b2-a64b-e61f8e0d9acb"), true)));
                    break;
                }
                catch (COMException e) when (e.HResult == E_CLASSNOTREG)
                {
                    return new TaggedUnionValue<IDedupDataPortManager, string>($"Class not registered: E_CLASSNOTREG {E_CLASSNOTREG:x}");
                }
                catch (COMException e) when (e.HResult == CO_E_SERVER_EXEC_FAILURE)
                {
                    attemptsLeft--;
                    if(attemptsLeft > 0)
                    {
                        continue;
                    }

                    return new TaggedUnionValue<IDedupDataPortManager, string>($"Class not registered: CO_E_SERVER_EXEC_FAILURE {CO_E_SERVER_EXEC_FAILURE:x}");
                }
                catch (InvalidCastException e) when (e.HResult == E_NOINTERFACE)
                {
                    return new TaggedUnionValue<IDedupDataPortManager, string>($"Class not registered: E_NOINTERFACE {E_NOINTERFACE:x}");
                }
                catch (UnauthorizedAccessException e) when (e.HResult == E_ACCESSDENIED)
                {
                    return new TaggedUnionValue<IDedupDataPortManager, string>("Dataport requires elevated access.");
                }

                throw new InvalidOperationException("Should be unreachable");
            }

            DedupDataPortVolumeStatus volumeStatus;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                dataPortManager.GetVolumeStatus(
                    (uint)DedupDataPortManagerOptions.AutoStart,
                    volume,
                    out volumeStatus);
                switch (volumeStatus)
                {
                    case DedupDataPortVolumeStatus.Ready:
                        break;
                    case DedupDataPortVolumeStatus.Initializing:
                    case DedupDataPortVolumeStatus.Maintenance:
                        logger($"Current DataPort volume status: {volumeStatus}. Will retry in 1 second.");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        break;
                    case DedupDataPortVolumeStatus.NotEnabled:
                    case DedupDataPortVolumeStatus.NotAvailable:
                        // https://microsoft.visualstudio.com/_search?type=code&lp=search-Collection&text=SetDedupPolicySettings&result=DefaultCollection%2FOS%2Fos%2FGBofficial%2Frsmaster%2F%2Fservercommon%2Fbase%2Ffs%2Fdedup%2Fmodules%2Fpscim%2Fprovider%2Fcimdedup.cpp&preview=0&filters=ProjectFilters%7BOS%7D&_a=contents
                        // 
                        //if (setWorkload && workload == DedupVolumeUsageType_Backup)
                        //{
                        //    minFileAge = 0;
                        //    autoStart = TRUE;
                        //}
                        // 
                        return new TaggedUnionValue<IDedupDataPortManager, string>($"{volumeStatus}. Try 'Enable-DedupVolume -Volume {volume} -UsageType Backup'");
                    case DedupDataPortVolumeStatus.Shutdown:
                    case DedupDataPortVolumeStatus.Unknown:
                    default:
                        throw new InvalidOperationException($"DedupDataPortVolumeStatus:{volumeStatus}");
                }
            }
            while (volumeStatus != DedupDataPortVolumeStatus.Ready);

            return new TaggedUnionValue<IDedupDataPortManager, string>(dataPortManager);
        }

        public static async Task<TaggedUnionValue<IDedupDataPort, string>> GetDataPortAsync(string pathOnVolume, CancellationToken cancellationToken, Action<string> logger)
        {

#if NET_STANDARD
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new TaggedUnionValue<IDedupDataPort, string>($"Dataport will not be used as it is only currently available on Windows.");
            }
#endif

            string volume = VolumeHelper.GetVolumeRootFromPath(pathOnVolume);
            var initResult = await managers.RunOnceAsync(volume, () => CreateForVolumeAsync(volume, cancellationToken, logger));

            return initResult.Match(
                manager =>
                {
                    IDedupDataPort dataPort;
                    manager.GetVolumeDataPort((uint)DedupDataPortManagerOptions.AutoStart, volume, out dataPort);

                    DedupDataPortVolumeStatus volumeStatus;
                    uint maintMb;
                    dataPort.GetStatus(out volumeStatus, out maintMb);

                    return new TaggedUnionValue<IDedupDataPort, string>(dataPort);
                }, error =>
                {
                    return new TaggedUnionValue<IDedupDataPort, string>($"Could not initialize dataport for '{volume}' as '{error}'");
                });
        }
    }

    [CLSCompliant(false)]
    public class DataPortResult
    {
        public readonly int BatchResult;
        public readonly int[] ItemResults;
        public DataPortResult(int batchResult, uint batchCount, int[] itemResults)
        {
            if (batchCount != itemResults.Length)
            {
                throw new ArgumentException($"{nameof(batchCount)} must match the length of {nameof(itemResults)}");
            }

            BatchResult = batchResult;
            ItemResults = itemResults;
        }
    }

    [CLSCompliant(false)]
    public sealed class DedupHashComparer : IEqualityComparer<DedupHash>, IComparer<DedupHash>
    {
        public static readonly DedupHashComparer Instance = new DedupHashComparer();
        private DedupHashComparer() { }
        public int Compare(DedupHash x, DedupHash y)
        {
            return ByteArrayComparer.Instance.Compare(x.Hash, y.Hash);
        }

        public bool Equals(DedupHash x, DedupHash y)
        {
            return ByteArrayComparer.Instance.Equals(x.Hash, y.Hash);
        }

        public int GetHashCode(DedupHash obj)
        {
            return ByteArrayComparer.Instance.GetHashCode(obj.Hash);
        }
    }

    [CLSCompliant(false)]
    public static class DedupDataPortExtensions
    {
        public static Task<DataPortResult> GetResultAsync(this IDedupDataPort dataPort, Guid requestId)
        {
            return Task.Factory.StartNew(() =>
            {
                DedupDataPortRequestStatus requestStatus;
                while (true)
                {
                    int batchResult;
                    uint batchCount;
                    int[] itemResults;
                    try
                    {
                        dataPort.GetRequestResults(requestId, 1000, out batchResult, out batchCount, out requestStatus, out itemResults);
                    }
                    catch (COMException e) when (e.HResult == unchecked((int)0x8000000A))
                    {
                        continue;
                    }

                    switch (requestStatus)
                    {
                        case DedupDataPortRequestStatus.Complete:
                        case DedupDataPortRequestStatus.Failed:
                            return new DataPortResult(batchResult, batchCount, itemResults);
                        case DedupDataPortRequestStatus.Queued:
                        case DedupDataPortRequestStatus.Processing:
                            break;
                        default:
                            throw new InvalidOperationException(requestStatus.ToString());
                    }
                }
            },
            TaskCreationOptions.LongRunning);
        }

        public static async Task<Dictionary<DedupHash,bool>> ContainsChunksAsync(this IDedupDataPort dataPort, IEnumerable<DedupHash> chunkHashes)
        {
            var results = new Dictionary<DedupHash, bool>(DedupHashComparer.Instance);
            foreach (var page in chunkHashes.GetPages(1024))
            {
                Guid requestId;
                dataPort.LookupChunks((uint)page.Count, page.ToArray(), out requestId);
                var result = await dataPort.GetResultAsync(requestId);
                if (result.BatchResult != 0)
                {
                    throw new InvalidOperationException("LookupChunks failed: " + result.BatchResult);
                }

                for (int i = 0; i < page.Count; i++)
                {
                    results.Add(page[i], result.ItemResults[i] == 0);
                }
            }
            return results;
        }

        public static async Task<bool> ContainsChunkAsync(this IDedupDataPort dataPort, DedupHash chunkHash)
        {
            return (await ContainsChunksAsync(dataPort, new[] { chunkHash })).Single().Value;
        }

        public static async Task WriteStreamAsync(this IDedupDataPort dataPort, DedupNode node, string volumeRelativePath)
        {
            ulong chunkOffset = 0;
            var entries = node.EnumerateChunkLeafsInOrder().Select(chunk =>
            {
                var entry = new DedupStreamEntry()
                {
                    Hash = new DedupHash() { Hash = chunk.Hash },
                    LogicalSize = (uint)chunk.TransitiveContentBytes,
                    Offset = chunkOffset,
                };
                chunkOffset += chunk.TransitiveContentBytes;
                return entry;
            }).ToArray();

            var streams = new DedupStream[]
            {
                new DedupStream()
                {
                    ChunkCount = (uint)entries.Length,
                    Length = node.TransitiveContentBytes,
                    Offset = 0,
                    Path = volumeRelativePath,
                }
            };
            Guid requestId;
            dataPort.CommitStreams((uint)streams.Length, streams, (uint)entries.Length, entries, out requestId);
            var result = await dataPort.GetResultAsync(requestId);

            if (result.BatchResult != 0 || result.ItemResults[0] != 0)
            {
                throw new InvalidOperationException(string.Format("CommitStream failed 0x{0:x} 0x{1:x}", result.BatchResult, result.ItemResults[0]));
            }
        }

        public static Task InsertChunkAsync(this IDedupDataPort dataPort, ChunkDedupIdentifier chunkId, DedupCompressedBuffer buffer)
        {
            return InsertChunksAsync(dataPort, new Dictionary<ChunkDedupIdentifier, DedupCompressedBuffer>()
            {
                { chunkId, buffer }
            });
        }

        public static async Task InsertChunksAsync(this IDedupDataPort dataPort, Dictionary<ChunkDedupIdentifier, DedupCompressedBuffer> buffers)
        {
            long totalSize = 0;
            int bufferCount = 0;

            foreach (var buffer in buffers)
            {
                ArraySegment<byte> chunkBuffer;
                bool isCompressed;
                buffer.Value.GetBytes(out isCompressed, out chunkBuffer);

                if (chunkBuffer.Count == 0)
                {
                    throw new ArgumentException("Cannot insert a zero-length chunk.");
                }

                totalSize += chunkBuffer.Count;
                bufferCount++;
            }

            DedupChunk[] chunks = new DedupChunk[buffers.Count];
            byte[] allChunkBytes = new byte[totalSize];
            int i = 0;
            int offset = 0;
            foreach (var buffer in buffers)
            {
                ArraySegment<byte> chunkBuffer;
                bool isCompressed;
                buffer.Value.GetBytes(out isCompressed, out chunkBuffer);

                chunks[i] = new DedupChunk()
                {
                    DataSize = (uint)chunkBuffer.Count,
                    Flags = isCompressed ? DedupChunkFlags.Compressed : DedupChunkFlags.None,
                    LogicalSize = (uint)buffer.Value.Uncompressed.Count,
                    Hash = new DedupHash()
                    {
                        Hash = buffer.Key.AlgorithmResult
                    }
                };

                Buffer.BlockCopy(chunkBuffer.Array, chunkBuffer.Offset, allChunkBytes, offset, chunkBuffer.Count);

                i++;
                offset += chunkBuffer.Count;
            }

            Guid requestId;
            dataPort.InsertChunks((uint)chunks.Length, chunks, (uint)allChunkBytes.Length, allChunkBytes, out requestId);
            var result = await dataPort.GetResultAsync(requestId);
            if (result.BatchResult != 0)
            {
                throw new InvalidOperationException("InsertChunks batch failed: " + result.BatchResult);
            }
            if (result.ItemResults.Any(r => (r != 0 && r != 1)))
            {
                throw new InvalidOperationException("InsertChunks failed: " + string.Join(",",result.ItemResults));
            }
        }
    }
}
