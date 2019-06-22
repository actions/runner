using GitHub.Services.BlobStore.Common;
using GitHub.Services.Content.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DedupIdentifier = GitHub.Services.BlobStore.Common.DedupIdentifier;
using ChunkDedupIdentifier = GitHub.Services.BlobStore.Common.ChunkDedupIdentifier;

namespace GitHub.Services.BlobStore.WebApi
{
    public delegate Task<MaybeCached<DedupCompressedBuffer>> GetDedupAsyncFunc(CancellationToken cancellationToken);

    public static class MaybeCached
    {
        public static MaybeCached<T> FromCached<T>(T value)
        {
            return new MaybeCached<T>(value, true);
        }

        public static MaybeCached<T> FromUncached<T>(T value)
        {
            return new MaybeCached<T>(value, false);
        }
    }

    public struct MaybeCached<T>
    {
        public readonly T Value;
        public readonly bool Cached;

        public MaybeCached(T value, bool cached)
        {
            Value = value;
            Cached = cached;
        }

        public static implicit operator T(MaybeCached<T> m)
        {
            return m.Value;
        }
    }

    public interface IDedupStoreHttpClient : IArtifactHttpClient
    {
        Task<Dictionary<DedupIdentifier, PreauthenticatedUri>> GetDedupUrlsAsync(ISet<DedupIdentifier> dedupIds, EdgeCache edgeCache, CancellationToken cancellationToken);

        Task<Dictionary<DedupIdentifier, GetDedupAsyncFunc>> GetDedupGettersAsync(ISet<DedupIdentifier> dedupIds, Uri proxyUri, EdgeCache edgeCache, CancellationToken cancellationToken);

        Task<Dictionary<ChunkDedupIdentifier, KeepUntilReceipt>> PutChunksAsync(Dictionary<ChunkDedupIdentifier, DedupCompressedBuffer> chunks, KeepUntilBlobReference keepUntil, CancellationToken cancellationToken);

        Task<KeepUntilReceipt> PutChunkAndKeepUntilReferenceAsync(ChunkDedupIdentifier chunkId, DedupCompressedBuffer chunk, KeepUntilBlobReference keepUntil, CancellationToken cancellationToken);

        Task<MaybeCached<DedupCompressedBuffer>> GetChunkAsync(ChunkDedupIdentifier chunkId, bool canRedirect, CancellationToken cancellationToken);

        Task<KeepUntilReceipt> TryKeepUntilReferenceChunkAsync(ChunkDedupIdentifier chunkId, KeepUntilBlobReference keepUntil, CancellationToken cancellationToken);

        Task<PutNodeResponse> PutNodeAndKeepUntilReferenceAsync( NodeDedupIdentifier nodeId, DedupCompressedBuffer node, KeepUntilBlobReference keepUntil, SummaryKeepUntilReceipt receipt, CancellationToken cancellationToken);
        
        Task<MaybeCached<DedupCompressedBuffer>> GetNodeAsync(NodeDedupIdentifier nodeId, bool canRedirect, CancellationToken cancellationToken);

        Task<TryReferenceNodeResponse> TryKeepUntilReferenceNodeAsync(NodeDedupIdentifier nodeId, KeepUntilBlobReference keepUntil, SummaryKeepUntilReceipt receipt, CancellationToken cancellationToken);

        Task PostEchoAsync(byte[] echoBytes, bool hash, bool base64, bool echo, bool vsoHash, bool storeInBlobStore, CancellationToken cancellationToken);

        Task PutRootAsync(DedupIdentifier dedupId, IdBlobReference rootRef, CancellationToken cancellationToken);

        Task DeleteRootAsync(DedupIdentifier dedupId, IdBlobReference rootRef, CancellationToken cancellationToken);

        Task<DedupDownloadInfo> GetDownloadInfoAsync(DedupIdentifier dedupId, bool includeChunks, CancellationToken cancellationToken);

        Task<IList<DedupDownloadInfo>> GetBatchDownloadInfoAsync(ISet<DedupIdentifier> dedupIds, bool includeChunks, CancellationToken cancellationToken);

        long Calls { get; }
        long ThrottledCalls { get; }

        // https://anothersysadmin.wordpress.com/2008/04/22/x-cache-and-x-cache-lookup-headers-explained/
        long XCacheHits { get; }
        long XCacheMisses { get; }

        int RecommendedChunkCountPerCall { get; }
    }

    public class PutNodeResponse : TaggedUnion<DedupNodeChildrenNeedAction, DedupNodeUpdated>
    {
        public PutNodeResponse(DedupNodeChildrenNeedAction arg) : base(arg)
        {
        }

        public PutNodeResponse(DedupNodeUpdated arg) : base(arg)
        {
        }
    }

    public class TryReferenceNodeResponse : TaggedUnion<DedupNodeNotFound, DedupNodeChildrenNotEnoughKeepUntil, DedupNodeUpdated>
    {
        public TryReferenceNodeResponse(DedupNodeNotFound arg) : base(arg)
        {
        }

        public TryReferenceNodeResponse(DedupNodeChildrenNotEnoughKeepUntil arg) : base(arg)
        {
        }

        public TryReferenceNodeResponse(DedupNodeUpdated arg) : base(arg)
        {
        }
    }

    public struct DedupNodeNotFound
    {
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public struct DedupNodeChildrenNeedAction
    {
        [JsonProperty(PropertyName = "InsufficientKeepUntil", Required = Required.Always)]
        public readonly DedupIdentifier[] InsufficientKeepUntil;

        [JsonProperty(PropertyName = "Missing", Required = Required.Always)]
        public readonly DedupIdentifier[] Missing;

        [JsonProperty(PropertyName = "Receipts", Required = Required.Always)]
        private readonly Dictionary<string, KeepUntilReceipt> receipts;

        public Dictionary<DedupIdentifier, KeepUntilReceipt> Receipts
        {
            get
            {
                return receipts.ToDictionary(kvp => DedupIdentifier.Create(kvp.Key), kvp => kvp.Value);
            }
        }

        public DedupNodeChildrenNeedAction(DedupIdentifier[] missing, DedupIdentifier[] insufficientKeepUntil,  Dictionary<DedupIdentifier, KeepUntilReceipt> receipts)
        {
            this.InsufficientKeepUntil = insufficientKeepUntil;
            this.Missing = missing;
            this.receipts = receipts.ToDictionary(kvp => kvp.Key.ValueString, kvp => kvp.Value);
        }

        public IEnumerable<DedupIdentifier> GetAllNeedingAction()
        {
            if(InsufficientKeepUntil != null)
            {
                foreach(var dedupId in InsufficientKeepUntil)
                {
                    yield return dedupId;
                }
            }

            if (Missing != null)
            {
                foreach (var dedupId in Missing)
                {
                    yield return dedupId;
                }
            }
        }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public struct DedupNodeChildrenNotEnoughKeepUntil
    {
        [JsonProperty(PropertyName = "InsufficientKeepUntil", Required = Required.Always)]
        public readonly DedupIdentifier[] InsufficientKeepUntil;


        [JsonProperty(PropertyName = "Receipts", Required = Required.Always)]
        private readonly Dictionary<string, KeepUntilReceipt> receipts;

        public Dictionary<DedupIdentifier, KeepUntilReceipt> Receipts
        {
            get
            {
                return receipts.ToDictionary(kvp => DedupIdentifier.Create(kvp.Key), kvp => kvp.Value);
            }
        }

        public DedupNodeChildrenNotEnoughKeepUntil(DedupIdentifier[] insufficientKeepUntil, Dictionary<DedupIdentifier, KeepUntilReceipt> receipts)
        {
            this.InsufficientKeepUntil = insufficientKeepUntil;
            this.receipts = receipts.ToDictionary(kvp => kvp.Key.ValueString, kvp => kvp.Value);
        }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public struct DedupNodeUpdated
    {
        [JsonProperty(PropertyName = "Receipts", Required = Required.Always)]
        private readonly Dictionary<string, KeepUntilReceipt> receipts;

        public Dictionary<DedupIdentifier, KeepUntilReceipt> Receipts
        {
            get
            {
                return receipts.ToDictionary(kvp => DedupIdentifier.Create(kvp.Key), kvp => kvp.Value);
            }
        }

        public DedupNodeUpdated(Dictionary<DedupIdentifier, KeepUntilReceipt> receipts)
        {
            this.receipts = receipts.ToDictionary(kvp => kvp.Key.ValueString, kvp => kvp.Value);
        }
    }

    public enum ChildNodeState
    {
        Missing = 0,
        NotEnoughKeepUntil = 1
    }

    public abstract class DedupDownloadInfoBase
    {
        public DedupDownloadInfoBase() { } // this is needed for JSON deserializing

        public DedupDownloadInfoBase(DedupIdentifier id, Uri url, long transitiveSize)
        {
            this.Id = id;
            this.Url = url;
            this.Size = transitiveSize;
        }

        public DedupIdentifier Id { get; set; }
        public Uri Url { get; set; }
        public long Size { get; set; }
    }

    public class DedupDownloadInfo : DedupDownloadInfoBase
    {
        public DedupDownloadInfo() { } // this is needed for JSON deserializing

        public DedupDownloadInfo(DedupDownloadInfoBase downloadInfoBase, ChunkDedupDownloadInfo[] chunks, long transitiveSize)
            : this(downloadInfoBase.Id, downloadInfoBase.Url, chunks, transitiveSize)
        { }

        public DedupDownloadInfo(DedupIdentifier id, Uri url, ChunkDedupDownloadInfo[] chunks, long transitiveSize)
            : base(id, url, transitiveSize)
        {
            if (chunks == null)
            {
                chunks = new ChunkDedupDownloadInfo[0];
            }
            this.Chunks = chunks;
        }

        public ChunkDedupDownloadInfo[] Chunks { get; set; }
    }

    public class ChunkDedupDownloadInfo : DedupDownloadInfoBase
    {
        public ChunkDedupDownloadInfo() { } // this is needed for JSON deserializing

        public ChunkDedupDownloadInfo(DedupIdentifier id, Uri url, long chunkSize)
            : base(id, url, chunkSize) { }
    }
}
