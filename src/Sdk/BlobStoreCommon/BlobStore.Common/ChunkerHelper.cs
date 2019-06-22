using BuildXL.Cache.ContentStore.Hashing;
using GitHub.Services.Content.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.BlobStore.Common
{
    [CLSCompliant(false)]
    public static class ChunkerHelper
    {
        private static readonly IContentHasher nodeHasher = DedupNodeHashInfo.Instance.CreateContentHasher();

        public const int MaxChunkSizeInBytes = 128 * 1024;

        private static ByteArrayPool chunkBufferPool = new ByteArrayPool(MaxChunkSizeInBytes, maxToKeep: 4 * Environment.ProcessorCount);

        private static Pool<IChunker> chunkerPool = new Pool<IChunker>(
            () => DedupNodeHashAlgorithm.CreateChunker(),
            chunker => { },
            maxToKeep: 4 * Environment.ProcessorCount);

        public static IPoolHandle<byte[]> BorrowChunkBuffer()
        {
            return chunkBufferPool.Get();
        }

        public enum NodeCallbackResult
        {
            Done,
            WalkChildren
        }

        public delegate Task<NodeCallbackResult> NodeCallbackAsync(DedupNode node, ulong offset);
        public delegate Task ChunkCallbackAsync(DedupNode chunk, ulong offset);

        public static async Task<DedupNode> CreateFromFileAsync(IFileSystem fileSystem, string path, CancellationToken cancellationToken, bool configureAwait)
        {
            using (Stream file = fileSystem.OpenStreamForFile(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            {
                return await CreateFromStreamAsync(file, cancellationToken, configureAwait).ConfigureAwait(configureAwait);
            }
        }

        public static async Task WalkTreeDepthFirstAsync(
            DedupNode root, 
            NodeCallbackAsync nodeCallbackAsync,
            ChunkCallbackAsync chunkCallbackAsync,
            CancellationToken cancellationToken,
            bool configureAwait,
            ulong offset = 0)
        {
            switch (root.Type)
            {
                case DedupNode.NodeType.InnerNode:
                {
                    var nodeResult = await nodeCallbackAsync(root, offset).ConfigureAwait(configureAwait);
                    switch (nodeResult)
                    {
                        case NodeCallbackResult.Done:
                            return;
                        case NodeCallbackResult.WalkChildren:
                            foreach (var child in root.ChildNodes)
                            {
                                await WalkTreeDepthFirstAsync(
                                    child,
                                    nodeCallbackAsync,
                                    chunkCallbackAsync,
                                    cancellationToken,
                                    configureAwait,
                                    offset).ConfigureAwait(configureAwait);
                                offset += child.TransitiveContentBytes;
                            }
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                    break;
                }
                case DedupNode.NodeType.ChunkLeaf:
                {
                    await chunkCallbackAsync(root, offset).ConfigureAwait(configureAwait);
                    break;
                }
                default:
                    throw new InvalidOperationException();
            }
        }

        public static async Task<DedupNode> CreateFromStreamAsync(Stream content, CancellationToken cancellationToken, bool configureAwait)
        {
            using (var hashToken = nodeHasher.CreateToken())
            {
                var hasher = (DedupNodeHashAlgorithm)hashToken.Hasher;
                using (Pool<byte[]>.PoolHandle bufferHandle = chunkBufferPool.Get())
                {
                    byte[] buffer = bufferHandle.Value;
                    int bytesRead;
                    while ((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(configureAwait)) != 0)
                    {
                        hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
                    }

                    hasher.TransformFinalBlock(buffer, 0, 0);
                }

                //change in cloudstore
                var node = hasher.GetNode();
                if(node.ChildNodes.Count == 1)
                {
                    node = node.ChildNodes.Single();
                }
                return node;
            }
        }

        public static DedupNode CreateFromStream(Stream content)
        {
            using (var hashToken = nodeHasher.CreateToken())
            {
                var hasher = (DedupNodeHashAlgorithm)hashToken.Hasher;
                using (Pool<byte[]>.PoolHandle bufferHandle = chunkBufferPool.Get())
                {
                    byte[] buffer = bufferHandle.Value;
                    int bytesRead;
                    while ((bytesRead = content.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
                    }

                    hasher.TransformFinalBlock(buffer, 0, 0);
                }
                return hasher.GetNode();
            }
        }
    }
}
