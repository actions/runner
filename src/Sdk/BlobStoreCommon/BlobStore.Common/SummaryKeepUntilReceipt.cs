using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace GitHub.Services.BlobStore.Common
{
    public class SummaryKeepUntilReceipt
    {
        private static readonly Pool<SHA256CryptoServiceProvider> PoolSha256 = new Pool<SHA256CryptoServiceProvider>(
            factory: () => new SHA256CryptoServiceProvider(),
            reset: sha256 => sha256.Initialize(),
            maxToKeep: 4 * Environment.ProcessorCount);

        public SummaryKeepUntilReceipt(KeepUntilBlobReference?[] keepUntils, byte[] signature)
        {
            this.KeepUntils = keepUntils;
            this.Signature = signature;
        }

        public SummaryKeepUntilReceipt(params KeepUntilReceipt[] receipts)
        {
            this.KeepUntils = receipts.Select(r => r?.KeepUntil).ToArray();
            this.Signature = ComputeSummarySignature(receipts.Select(r => r?.Signature));
        }

        public KeepUntilBlobReference?[] KeepUntils { get; private set; }

        public byte[] Signature { get; private set; }

        internal static byte[] ComputeSummarySignature(IEnumerable<byte[]> signatures)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            using (var sha256Handle = PoolSha256.Get())
            {
                foreach (var signature in signatures)
                {
                    if (signature != null)
                    {
                        writer.Write(signature);
                    }
                    else
                    {
                        writer.Write(KeepUntilReceipt.NullSignature);
                    }
                }

                writer.Flush();

                var data = stream.ToArray();
                return sha256Handle.Value.ComputeHash(data, 0, data.Length);
            }
        }
    }
}
