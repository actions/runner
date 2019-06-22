using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace GitHub.Services.BlobStore.Common
{
    using NTSTATUS = Int32;

    public partial class VsoHash
    {
        private unsafe static class BCrypt
        {
            /// <content>
            /// The <see cref="SafeHashHandle"/> nested type.
            /// </content>
            private class SafeHashHandle : SafeHandle
            {
                /// <summary>
                /// A handle that may be used in place of <see cref="IntPtr.Zero"/>.
                /// </summary>
                public static readonly SafeHashHandle Null = new SafeHashHandle();

                /// <summary>
                /// Initializes a new instance of the <see cref="SafeHashHandle"/> class.
                /// </summary>
                public SafeHashHandle()
                    : base(IntPtr.Zero, true)
                {
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="SafeHashHandle"/> class.
                /// </summary>
                /// <param name="preexistingHandle">An object that represents the pre-existing handle to use.</param>
                /// <param name="ownsHandle">
                ///     <see langword="true" /> to have the native handle released when this safe handle is disposed or finalized;
                ///     <see langword="false" /> otherwise.
                /// </param>
                public SafeHashHandle(IntPtr preexistingHandle, bool ownsHandle = true)
                    : base(IntPtr.Zero, ownsHandle)
                {
                    this.SetHandle(preexistingHandle);
                }

                /// <inheritdoc />
                public override bool IsInvalid => this.handle == IntPtr.Zero;

                /// <inheritdoc />
                protected override bool ReleaseHandle()
                {
                    return BCryptDestroyHash(this.handle) == 0;
                }
            }

            /// <summary>
            /// Destroys a hash or Message Authentication Code (MAC) object.
            /// </summary>
            /// <returns>Returns a status code that indicates the success or failure of the function.</returns>
            [DllImport(nameof(BCrypt), SetLastError = true)]
            private static extern NTSTATUS BCryptDestroyHash(IntPtr hHash);

            /// <summary>
            /// A BCrypt algorithm handle.
            /// </summary>
            private class SafeAlgorithmHandle : SafeHandle
            {
                /// <summary>
                /// A handle that may be used in place of <see cref="IntPtr.Zero"/>.
                /// </summary>
                public static readonly SafeAlgorithmHandle Null = new SafeAlgorithmHandle();

                /// <summary>
                /// Initializes a new instance of the <see cref="SafeAlgorithmHandle"/> class.
                /// </summary>
                public SafeAlgorithmHandle()
                    : base(IntPtr.Zero, true)
                {
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="SafeAlgorithmHandle"/> class.
                /// </summary>
                /// <param name="preexistingHandle">An object that represents the pre-existing handle to use.</param>
                /// <param name="ownsHandle">
                ///     <see langword="true" /> to have the native handle released when this safe handle is disposed or finalized;
                ///     <see langword="false" /> otherwise.
                /// </param>
                public SafeAlgorithmHandle(IntPtr preexistingHandle, bool ownsHandle = true)
                    : base(IntPtr.Zero, ownsHandle)
                {
                    this.SetHandle(preexistingHandle);
                }

                /// <inheritdoc />
                public override bool IsInvalid => this.handle == IntPtr.Zero;

                /// <inheritdoc />
                protected override bool ReleaseHandle()
                {
                    return BCryptCloseAlgorithmProvider(this.handle, 0) == 0;
                }
            }

            [DllImport(nameof(BCrypt), SetLastError = true, ExactSpelling = true)]
            private static extern NTSTATUS BCryptCloseAlgorithmProvider(
                IntPtr algorithmHandle,
                BCryptCloseAlgorithmProviderFlags flags = BCryptCloseAlgorithmProviderFlags.None);

            private enum BCryptCloseAlgorithmProviderFlags
            {
                None = 0
            }

            private enum BCryptOpenAlgorithmProviderFlags
            {
                //
                // Summary:
                //     No flags.
                None = 0,
                //
                // Summary:
                //     The provider will perform the Hash-Based Message Authentication Code (HMAC) algorithm
                //     with the specified hash algorithm. This flag is only used by hash algorithm providers.
                BCRYPT_ALG_HANDLE_HMAC_FLAG = 8,
                //
                // Summary:
                //     Creates a reusable hashing object. The object can be used for a new hashing operation
                //     immediately after calling BCryptFinishHash. For more information, see Creating
                //     a Hash with CNG.
                BCRYPT_HASH_REUSABLE_FLAG = 32,
                //
                // Summary:
                //     Needed for use with PInvoke.BCrypt.BCryptCreateMultiHash(PInvoke.BCrypt.SafeAlgorithmHandle,PInvoke.BCrypt.SafeHashHandle@,System.Int32,System.Byte[],System.Int32,System.Byte[],System.Int32,PInvoke.BCrypt.BCryptCreateHashFlags).
                BCRYPT_MULTI_FLAG = 64
            }

            private enum HashOperationType
            {
                BCRYPT_HASH_OPERATION_HASH_DATA = 1,
                BCRYPT_HASH_OPERATION_FINISH_HASH = 2
            }

            private struct BCRYPT_MULTI_HASH_OPERATION
            {
                public int iHash;
                public HashOperationType hashOperation;
                public byte* pbBuffer;
                public int cbBuffer;
            }

            private static class AlgorithmIdentifiers
            {
                //
                // Summary:
                //     The 512-bit secure hash algorithm. Standard: FIPS 180-2, FIPS 198
                public const string BCRYPT_SHA512_ALGORITHM = "SHA512";
                //
                // Summary:
                //     The 256-bit secure hash algorithm. Standard: FIPS 180-2, FIPS 198
                public const string BCRYPT_SHA256_ALGORITHM = "SHA256";
            }

            [DllImport(nameof(BCrypt), SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
            private static extern NTSTATUS BCryptOpenAlgorithmProvider(
                out SafeAlgorithmHandle phAlgorithm,
                string pszAlgId,
                string pszImplementation,
                BCryptOpenAlgorithmProviderFlags dwFlags);

            public enum BCryptCreateHashFlags
            {
                None = 0,
                //
                // Summary:
                //     Creates a reusable hashing object. The object can be used for a new hashing operation
                //     immediately after calling BCryptFinishHash. For more information, see Creating
                //     a Hash with CNG. Windows Server 2008 R2, Windows 7, Windows Server 2008, and
                //     Windows Vista: This flag is not supported.
                BCRYPT_HASH_REUSABLE_FLAG = 32
            }

            [DllImport(nameof(BCrypt), SetLastError = true)]
            private static extern NTSTATUS BCryptCreateMultiHash(
                SafeAlgorithmHandle hAlgorithm,
                out SafeHashHandle phHash,
                int nHashes,
                byte[] pbHashObject,
                int cbHashObject,
                byte[] pbSecret,
                int cbSecret,
                BCryptCreateHashFlags dwFlags);

            public enum MultiOperationType
            {
                BCRYPT_OPERATION_TYPE_HASH = 1
            }

            [DllImport(nameof(BCrypt), SetLastError = true)]
            private static extern NTSTATUS BCryptProcessMultiOperations(
                SafeHashHandle hHash,
                MultiOperationType operationType,
                BCRYPT_MULTI_HASH_OPERATION[] pOperations,
                int cbOperations,
                int dwFlags = 0);

            public sealed class BCryptVsoHashContext : IDisposable
            {
                private const int sha256ByteCount = 32;
                private static readonly int MultiHashStructSize = Marshal.SizeOf<BCRYPT_MULTI_HASH_OPERATION>();
                private readonly BCRYPT_MULTI_HASH_OPERATION[] ops = new BCRYPT_MULTI_HASH_OPERATION[2 * VsoHash.PagesPerBlock];
                private readonly SafeAlgorithmHandle algorithm;
                private readonly SafeHashHandle hash;
                private readonly byte[] pageHashes = new byte[32 * VsoHash.PagesPerBlock];
                private readonly SHA256Managed metaHasher = new SHA256Managed();

                public BCryptVsoHashContext()
                {
                    BCryptOpenAlgorithmProvider(out algorithm, AlgorithmIdentifiers.BCRYPT_SHA256_ALGORITHM, null, BCryptOpenAlgorithmProviderFlags.BCRYPT_MULTI_FLAG);
                    BCryptCreateMultiHash(algorithm, out hash, VsoHash.PagesPerBlock, null, 0, null, 0, BCryptCreateHashFlags.BCRYPT_HASH_REUSABLE_FLAG);
                }

                public BlobBlockHash HashBlock(byte[] block, int blockLength)
                {
                    int pageCount = (blockLength + VsoHash.PageSize - 1) / VsoHash.PageSize;
                    int opsBytes = pageCount * 2 * MultiHashStructSize;
                    fixed (byte* blockPtr = block)
                    fixed (byte* pageHashesPtr = pageHashes)
                    {
                        // Set up hashing inputs
                        {
                            byte* pagePtr = blockPtr;
                            int bytesRemaining = blockLength;

                            for (int i = 0; i < pageCount; i++)
                            {
                                ops[i].iHash = i;
                                ops[i].hashOperation = HashOperationType.BCRYPT_HASH_OPERATION_HASH_DATA;
                                ops[i].pbBuffer = pagePtr;
                                int pageSize = Math.Min(bytesRemaining, VsoHash.PageSize);
                                ops[i].cbBuffer = pageSize;

                                bytesRemaining -= pageSize;
                                pagePtr += VsoHash.PageSize;
                            }
                        }

                        // set up hashing outputs
                        {
                            byte* hashPtr = pageHashesPtr;
                            for (int i = 0; i < pageCount; i++)
                            {
                                ops[pageCount + i].iHash = i;
                                ops[pageCount + i].hashOperation = HashOperationType.BCRYPT_HASH_OPERATION_FINISH_HASH;
                                ops[pageCount + i].pbBuffer = hashPtr;
                                ops[pageCount + i].cbBuffer = sha256ByteCount;

                                hashPtr += sha256ByteCount;
                            }
                        }

                        BCryptProcessMultiOperations(hash, MultiOperationType.BCRYPT_OPERATION_TYPE_HASH, ops, opsBytes);
                    }

                    return new BlobBlockHash(metaHasher.ComputeHash(pageHashes, 0, pageCount * sha256ByteCount));
                }

                public void Dispose()
                {
                    metaHasher.Dispose();
                    hash.Dispose();
                    algorithm.Dispose();
                }
            }
        }
    }
}
