using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.BlobStore.Common
{
    public class KeepUntilReceipt
    {
        public static readonly byte[] NullSignature = new byte[32];

        public KeepUntilReceipt(KeepUntilBlobReference keepUntil, byte[] signature)
        {
            this.KeepUntil = keepUntil;
            this.Signature = signature;
        }

        public byte[] Signature { get; private set; }

        public KeepUntilBlobReference KeepUntil { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is KeepUntilReceipt)
            {
                return this.Equals((KeepUntilReceipt)obj);
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(KeepUntilReceipt lhs, KeepUntilReceipt rhs)
        {
            return object.Equals(lhs, rhs);
        }

        public static bool operator !=(KeepUntilReceipt lhs, KeepUntilReceipt rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(KeepUntilReceipt obj)
        {
            return this.KeepUntil == obj.KeepUntil && Enumerable.SequenceEqual(this.Signature, obj.Signature);
        }

        public override int GetHashCode()
        {
            return Convert.ToBase64String(this.Signature).GetHashCode();
        }

        public static KeepUntilReceipt Create(string secret, Guid serviceHost, DedupIdentifier dedupId, KeepUntilBlobReference keepuntil)
        {
            var signature = ComputeSignature(secret, serviceHost, dedupId, keepuntil);
            return new KeepUntilReceipt(keepuntil, signature);
        }

        internal static byte[] ComputeSignature(string secret, Guid serviceHost, DedupIdentifier dedupId, KeepUntilBlobReference keepuntil)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(serviceHost.ToByteArray());
                writer.Write(dedupId.Value);
                writer.Write(keepuntil.KeepUntil.ToFileTimeUtc());
                writer.Flush();

                var key = Encoding.UTF8.GetBytes(secret);
                return HMACSHA256Encode(stream.ToArray(), key);
            }
        }

        private static byte[] HMACSHA256Encode(byte[] input, byte[] key)
        {
            var hmac = new HMACSHA256(key); // TODO make a pool of these
            return hmac.ComputeHash(input);
        }
    }
}
