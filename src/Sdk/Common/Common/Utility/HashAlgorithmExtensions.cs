using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    public static class HashAlgorithmExtensions
    {
        public static async Task<byte[]> ComputeHashAsync(this HashAlgorithm hashAlg, Stream inputStream)
        {
            byte[] buffer = new byte[4096];

            while (true)
            {
                int read = await inputStream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                    break;

                hashAlg.TransformBlock(buffer, 0, read, null, 0);
            }

            hashAlg.TransformFinalBlock(buffer, 0, 0);
            return hashAlg.Hash;
        }
    }
}
