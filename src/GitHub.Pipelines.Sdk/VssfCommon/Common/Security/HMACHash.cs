using System;
using System.Security.Cryptography;
using System.Text;

namespace GitHub.Services.Common
{
    public class HMACSHA256Hash : HMACHash<HMACSHA256>
    {
        public HMACSHA256Hash(string content, byte[] key) : base(content, key) { }
    }

    public class HMACSHA512Hash : HMACHash<HMACSHA512>
    {
        public HMACSHA512Hash(string content, byte[] key) : base(content, key) { }
    }

    public abstract class HMACHash<THMAC> : IDisposable where THMAC : HMAC
    {
        private string m_content;
        private byte[] m_hash;
        private string m_hashBase32Encoded;
        private string m_hashBase64Encoded;
        private THMAC m_hashAlgorithm;

        public HMACHash(string content, byte[] key)
        {
            if (String.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Content cannot be null or empty.");
            }

            if (key == null || key.Length == 0)
            {
                throw new ArgumentException("Key cannot be null or empty.");
            }

            m_content = content;
            m_hashAlgorithm = (THMAC)Activator.CreateInstance(typeof(THMAC), new[] { key });
        }

        public void Dispose()
        {
            m_hashAlgorithm.Dispose();
        }

        public byte[] Hash
        {
            get
            {
                if (m_hash == null)
                {
                    ComputeHash();
                }

                return m_hash;
            }
        }

        public string HashBase32Encoded
        {
            get
            {
                if (m_hash == null)
                {
                    ComputeHash();
                }

                return m_hashBase32Encoded;
            }
        }

        public string HashBase64Encoded
        {
            get
            {
                if (m_hash == null)
                {
                    ComputeHash();
                }

                return m_hashBase64Encoded;
            }
        }

        private void ComputeHash()
        {
            var encodedBytes = Encoding.ASCII.GetBytes(m_content);
            m_hash = m_hashAlgorithm.ComputeHash(encodedBytes);

            m_hashBase64Encoded = Convert.ToBase64String(m_hash);
            m_hashBase32Encoded = Base32Encoder.Encode(m_hash);
        }
    }
}
