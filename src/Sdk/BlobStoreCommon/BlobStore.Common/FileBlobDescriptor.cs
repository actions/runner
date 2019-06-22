using BuildXL.Cache.ContentStore.Hashing;
using GitHub.Services.Common;
using GitHub.Services.Content.Common;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static GitHub.Services.BlobStore.Common.FileBlobDescriptorConstants;

namespace GitHub.Services.BlobStore.Common
{
    [CLSCompliant(false)]
    public sealed class FileBlobDescriptor : IDropFile, IComparable<FileBlobDescriptor>, IEquatable<FileBlobDescriptor>
    {
        private static readonly DedupNode precomputedEmptyDirectoryDedupNode;

        /// <summary>
        /// Initializes the <see cref="FileBlobDescriptor"/> class.
        /// NOTE: The calculation of the stream for empty directory is made sync here because we expect
        /// this operation to be very short. The async callers of this method should bear that in mind.
        /// </summary>
        static FileBlobDescriptor()
        {
            using (var emptyStrm = new MemoryStream())
            {
                precomputedEmptyDirectoryDedupNode = ChunkerHelper
                    .CreateFromStreamAsync(emptyStrm, CancellationToken.None, configureAwait: false).SyncResult();
            }
        }

        /// <summary>
        /// Calculates the asynchronous.
        /// </summary>
        /// <param name="rootDirectory">The root directory.</param>
        /// <param name="chunkDedup">if set to <c>true</c> [chunk dedup].</param>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="fileBlobType">Type of the file BLOB.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static Task<FileBlobDescriptor> CalculateAsync(
            string rootDirectory,
            bool chunkDedup,
            string relativePath,
            FileBlobType fileBlobType,
            CancellationToken cancellationToken)
        {
            return CalculateAsync(Content.Common.FileSystem.Instance, rootDirectory, chunkDedup, relativePath, fileBlobType, cancellationToken);
        }

        /// <summary>
        /// For DropServiceClient, ItemUploader, and PrecomputedHashesGenerator, calculates the blob descriptor.
        /// Handles empty directory case for both file and chunk dedup.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="rootDirectory">The root directory.</param>
        /// <param name="chunkDedup">if set to <c>true</c> [chunk dedup].</param>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="fileBlobType">Type of the file BLOB.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static async Task<FileBlobDescriptor> CalculateAsync(
            IFileSystem fileSystem,
            string rootDirectory,
            bool chunkDedup,
            string relativePath,
            FileBlobType fileBlobType,
            CancellationToken cancellationToken)
        {
            FileBlobDescriptor descriptor;

            if (chunkDedup)
            {
                if (fileBlobType == FileBlobType.EmptyDirectory)
                {
                    descriptor =
                        new FileBlobDescriptor(
                            fileSystem,
                            rootDirectory,
                            relativePath,
                            fileSize: 0,
                            FileBlobDescriptorConstants.EmptyDirectoryChunkBlobIdentifier,
                            networkPaths: new List<string>(0))
                        {
                            Node = precomputedEmptyDirectoryDedupNode
                        };
                }
                else
                {
                    descriptor = new FileBlobDescriptor(fileSystem, rootDirectory, relativePath);
                    descriptor.Node = await ChunkerHelper.CreateFromFileAsync(fileSystem, descriptor.AbsolutePath, cancellationToken, configureAwait: false).ConfigureAwait(false);
                    descriptor.FileSize = (long)descriptor.Node.TransitiveContentBytes;
                    descriptor.BlobIdentifier = descriptor.Node.GetDedupId().ToBlobIdentifier();
                }
            }
            // File dedup case.
            else
            {
                if (fileBlobType == FileBlobType.EmptyDirectory)
                {
                    descriptor = new FileBlobDescriptor(fileSystem, rootDirectory, relativePath, fileSize: 0, VsoHash.OfNothing.BlobId, networkPaths: new List<string>(0));
                }
                else
                {
                    descriptor = new FileBlobDescriptor(fileSystem, rootDirectory, relativePath);
                    using (var stream = fileSystem.OpenStreamForFile(descriptor.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                    {
                        descriptor.FileSize = stream.Length;

                        // Fail-Fast: If the file size is 100GB or more, disallow to avoid 409 conflicts later on. Don't bother calculating the hash.
                        // See bug 1405394 and https://docs.microsoft.com/en-us/rest/api/storageservices/put-block-list for details.
                        //
                        if (descriptor.FileSize >= MaxFileSizeFileDedup)
                        {
                            throw new InvalidOperationException(
                                $"File: {descriptor.RelativePath} is {descriptor.FileSize} bytes and " +
                                $"larger than or equal to the max allowed size of : {MaxFileSizeFileDedup} bytes. " +
                                $"Consider using chunk dedup or contact artsup@microsoft.com for details.");
                        }

                        descriptor.BlobIdentifier = (await VsoHash.CalculateBlobIdentifierWithBlocksAsync(stream)).BlobId;
                    }
                }
            }

            return descriptor;
        }

        /// <summary>
        /// For PrecomputedHashesGenerator
        /// </summary>
        public static FileBlobDescriptor Deserialize(string rootDirectory, string serialized)
        {
            if (serialized == null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            var separatedString = serialized.Split(new[] { PathIdentifierSeperator }, StringSplitOptions.RemoveEmptyEntries);

            if (separatedString.Length < 3)
            {
                throw new ArgumentException($"Serialized value \"{serialized}\" didn't match expected syntax: [RelativePath]?[FileSizeBytes]?[BlobId]?[OptionalNetworkPaths]", nameof(serialized));
            }

            return new FileBlobDescriptor(
                rootDirectory: rootDirectory,
                relativePath: separatedString[0],
                fileSize: long.Parse(separatedString[1]),
                blobIdentifier: BlobIdentifier.Deserialize(separatedString[2]),
                networkPaths: separatedString.Length > 3 ? separatedString[3].Split(',').ToList() : null);
        }

        /// <summary>
        /// For RemoteableDropServiceClient
        /// </summary>
        public FileBlobDescriptor(IDropFile copy, IFileSystem fileSystem)
        {
            this.FileSystem = fileSystem;
            RelativePath = copy.RelativePath;
            FileSize = copy.FileSize;
            BlobIdentifier = copy.BlobIdentifier; // We don't set the Node property in this case
            NetworkPaths = new List<string>(0);
        }

        /// <summary>
        /// For RemoteableDropServiceClient
        /// </summary>
        public FileBlobDescriptor(IDropFile copy, IFileSystem fileSystem, string absolutePath) : this(copy, fileSystem)
        {
            AbsolutePath = absolutePath;
        }

        /// <summary>
        /// For RemoteableDropServiceClient
        /// </summary>
        public FileBlobDescriptor(IDropFile copy, string absolutePath)
            : this(copy, Content.Common.FileSystem.Instance, absolutePath) { }

        /// <summary>
        /// For Deserialize and tests.
        /// TODO: Do not use this API from tests. Instead use the constructor which alllows an InMemoryFileSystem to be specified.
        /// </summary>
        internal FileBlobDescriptor(string rootDirectory, string relativePath, long? fileSize, BlobIdentifier blobIdentifier, List<string> networkPaths)
            : this(Content.Common.FileSystem.Instance, rootDirectory, relativePath, fileSize, blobIdentifier, networkPaths) { }

        /// <summary>
        /// For Deserialize and tests
        /// </summary>
        internal FileBlobDescriptor(
            IFileSystem fileSystem, 
            string rootDirectory, 
            string relativePath, 
            long? fileSize,
            BlobIdentifier blobIdentifier,
            List<string> networkPaths) : this(fileSystem, rootDirectory, relativePath)
        {
            ArgumentUtility.CheckForNull(blobIdentifier, nameof(blobIdentifier));

            this.FileSystem = fileSystem;
            FileSize = fileSize;
            BlobIdentifier = blobIdentifier;
            NetworkPaths = networkPaths;

            if (blobIdentifier.AlgorithmId == ChunkDedupIdentifier.ChunkAlgorithmId)
            {
                this.Node = new DedupNode(DedupNode.NodeType.ChunkLeaf, (ulong)fileSize.Value, blobIdentifier.AlgorithmResultBytes, height: null);
            }
            else if (blobIdentifier.AlgorithmId == NodeDedupIdentifier.NodeAlgorithmId)
            {
                this.Node = new DedupNode(DedupNode.NodeType.InnerNode, (ulong)fileSize.Value, blobIdentifier.AlgorithmResultBytes, height: null);
            }
        }

        /// <summary>
        /// Private constructor
        /// </summary>
        private FileBlobDescriptor(IFileSystem fileSystem, string rootDirectory, string relativePath)
        {
            if (rootDirectory == null)
            {
                throw new ArgumentNullException(nameof(rootDirectory));
            }

            RootDirectory = rootDirectory;
            RelativePath = relativePath;
        }

        private readonly IFileSystem FileSystem;

        public DedupNode Node {get; private set;}

        public long? FileSize { get; private set; }

        private string relativePath;
        public string RelativePath
        {
            get { return relativePath; }
            set
            {
                relativePath = value;
                if (RootDirectory != null && relativePath != null)
                {
                    AbsolutePath = Path.Combine(RootDirectory, relativePath);
                }
            }
        }

        public string RootDirectory { get; }

        public string AbsolutePath { get; private set; }
        
        public BlobIdentifier BlobIdentifier { get; private set; }

        public List<string> NetworkPaths { get; }

        public string Serialize()
        {
            return Serialize(this.RelativePath, this.FileSize.Value, this.BlobIdentifier, this.NetworkPaths);
        }

        private static string Serialize(string relativePath, long fileSize, BlobIdentifier blobIdentifier, IEnumerable<string> uncs)
        {
            return $"{relativePath}{PathIdentifierSeperator}{fileSize}{PathIdentifierSeperator}{blobIdentifier.ValueString}{PathIdentifierSeperator}{String.Join(",", (uncs ?? Enumerable.Empty<string>()))}";
        }
        
        public int CompareTo(FileBlobDescriptor other)
        {
            if (other.FileSize != FileSize)
            {
                return (FileSize - other.FileSize < 0) ? -1 : 1;
            }
            else if (!RelativePath.Equals(other.RelativePath))
            {
                return String.Compare(RelativePath, other.RelativePath, StringComparison.Ordinal);
            }
            else if (!RootDirectory.Equals(other.RootDirectory))
            {
                return String.Compare(RootDirectory, other.RootDirectory, StringComparison.Ordinal);
            }
            else if (!AbsolutePath.Equals(other.AbsolutePath))
            {
                return String.Compare(AbsolutePath, other.AbsolutePath, StringComparison.Ordinal);
            }
            else if (!BlobIdentifier.Equals(other.BlobIdentifier))
            {
                return BlobIdentifier.CompareTo(other.BlobIdentifier);
            }

            return 0;
        }
        
        public override int GetHashCode()
        {
            return this.Serialize().GetHashCode();
        }
        
        public bool Equals(FileBlobDescriptor other)
        {
            return this.CompareTo(other) == 0;
        }

        public override bool Equals(object obj) => Equals(obj as FileBlobDescriptor);

        public override string ToString()
        {
            return this.Serialize();
        }
    }
}
