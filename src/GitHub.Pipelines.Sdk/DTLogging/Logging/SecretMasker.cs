using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace GitHub.DistributedTask.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class SecretMasker : ISecretMasker, IDisposable
    {
        public SecretMasker()
        {
            m_originalValueSecrets = new HashSet<ValueSecret>();
            m_regexSecrets = new HashSet<RegexSecret>();
            m_valueEncoders = new HashSet<ValueEncoder>();
            m_valueSecrets = new HashSet<ValueSecret>();
        }

        private SecretMasker(SecretMasker copy)
        {
            // Read section.
            try
            {
                copy.m_lock.EnterReadLock();

                // Copy the hash sets.
                m_originalValueSecrets = new HashSet<ValueSecret>(copy.m_originalValueSecrets);
                m_regexSecrets = new HashSet<RegexSecret>(copy.m_regexSecrets);
                m_valueEncoders = new HashSet<ValueEncoder>(copy.m_valueEncoders);
                m_valueSecrets = new HashSet<ValueSecret>(copy.m_valueSecrets);
            }
            finally
            {
                if (copy.m_lock.IsReadLockHeld)
                {
                    copy.m_lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// This implementation assumes no more than one thread is adding regexes, values, or encoders at any given time.
        /// </summary>
        public void AddRegex(String pattern)
        {
            // Test for empty.
            if (String.IsNullOrEmpty(pattern))
            {
                return;
            }

            // Write section.
            try
            {
                m_lock.EnterWriteLock();

                // Add the value.
                m_regexSecrets.Add(new RegexSecret(pattern));
            }
            finally
            {
                if (m_lock.IsWriteLockHeld)
                {
                    m_lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// This implementation assumes no more than one thread is adding regexes, values, or encoders at any given time.
        /// </summary>
        public void AddValue(String value)
        {
            // Test for empty.
            if (String.IsNullOrEmpty(value))
            {
                return;
            }

            var valueSecrets = new List<ValueSecret>(new[] { new ValueSecret(value) });

            // Read section.
            ValueEncoder[] valueEncoders;
            try
            {
                m_lock.EnterReadLock();

                // Test whether already added.
                if (m_originalValueSecrets.Contains(valueSecrets[0]))
                {
                    return;
                }

                // Read the value encoders.
                valueEncoders = m_valueEncoders.ToArray();
            }
            finally
            {
                if (m_lock.IsReadLockHeld)
                {
                    m_lock.ExitReadLock();
                }
            }

            // Compute the encoded values.
            foreach (ValueEncoder valueEncoder in valueEncoders)
            {
                String encodedValue = valueEncoder(value);
                if (!String.IsNullOrEmpty(encodedValue))
                {
                    valueSecrets.Add(new ValueSecret(encodedValue));
                }
            }

            // Write section.
            try
            {
                m_lock.EnterWriteLock();

                // Add the values.
                m_originalValueSecrets.Add(valueSecrets[0]);
                foreach (ValueSecret valueSecret in valueSecrets)
                {
                    m_valueSecrets.Add(valueSecret);
                }
            }
            finally
            {
                if (m_lock.IsWriteLockHeld)
                {
                    m_lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// This implementation assumes no more than one thread is adding regexes, values, or encoders at any given time.
        /// </summary>
        public void AddValueEncoder(ValueEncoder encoder)
        {
            ValueSecret[] originalSecrets;

            // Read section.
            try
            {
                m_lock.EnterReadLock();

                // Test whether already added.
                if (m_valueEncoders.Contains(encoder))
                {
                    return;
                }

                // Read the original value secrets.
                originalSecrets = m_originalValueSecrets.ToArray();
            }
            finally
            {
                if (m_lock.IsReadLockHeld)
                {
                    m_lock.ExitReadLock();
                }
            }

            // Compute the encoded values.
            var encodedSecrets = new List<ValueSecret>();
            foreach (ValueSecret originalSecret in originalSecrets)
            {
                String encodedValue = encoder(originalSecret.m_value);
                if (!String.IsNullOrEmpty(encodedValue))
                {
                    encodedSecrets.Add(new ValueSecret(encodedValue));
                }
            }

            // Write section.
            try
            {
                m_lock.EnterWriteLock();

                // Add the encoder.
                m_valueEncoders.Add(encoder);

                // Add the values.
                foreach (ValueSecret encodedSecret in encodedSecrets)
                {
                    m_valueSecrets.Add(encodedSecret);
                }
            }
            finally
            {
                if (m_lock.IsWriteLockHeld)
                {
                    m_lock.ExitWriteLock();
                }
            }
        }

        public ISecretMasker Clone() => new SecretMasker(this);

        public void Dispose()
        {
            m_lock?.Dispose();
            m_lock = null;
        }

        public String MaskSecrets(String input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return String.Empty;
            }

            var secretPositions = new List<ReplacementPosition>();

            // Read section.
            try
            {
                m_lock.EnterReadLock();

                // Get indexes and lengths of all substrings that will be replaced.
                foreach (RegexSecret regexSecret in m_regexSecrets)
                {
                    secretPositions.AddRange(regexSecret.GetPositions(input));
                }

                foreach (ValueSecret valueSecret in m_valueSecrets)
                {
                    secretPositions.AddRange(valueSecret.GetPositions(input));
                }
            }
            finally
            {
                if (m_lock.IsReadLockHeld)
                {
                    m_lock.ExitReadLock();
                }
            }

            // Short-circuit if nothing to replace.
            if (secretPositions.Count == 0)
            {
                return input;
            }

            // Merge positions into ranges of characters to replace.
            List<ReplacementPosition> replacementPositions = new List<ReplacementPosition>();
            ReplacementPosition currentReplacement = null;
            foreach (ReplacementPosition secretPosition in secretPositions.OrderBy(x => x.Start))
            {
                if (currentReplacement == null)
                {
                    currentReplacement = new ReplacementPosition(copy: secretPosition);
                    replacementPositions.Add(currentReplacement);
                }
                else
                {
                    if (secretPosition.Start <= currentReplacement.End)
                    {
                        // Overlap
                        currentReplacement.Length = Math.Max(currentReplacement.End, secretPosition.End) - currentReplacement.Start;
                    }
                    else
                    {
                        // No overlap
                        currentReplacement = new ReplacementPosition(copy: secretPosition);
                        replacementPositions.Add(currentReplacement);
                    }
                }
            }

            // Replace
            var stringBuilder = new StringBuilder();
            Int32 startIndex = 0;
            foreach (var replacement in replacementPositions)
            {
                stringBuilder.Append(input.Substring(startIndex, replacement.Start - startIndex));
                stringBuilder.Append("***");
                startIndex = replacement.Start + replacement.Length;
            }

            if (startIndex < input.Length)
            {
                stringBuilder.Append(input.Substring(startIndex));
            }

            return stringBuilder.ToString();
        }

        private readonly HashSet<ValueSecret> m_originalValueSecrets;
        private readonly HashSet<RegexSecret> m_regexSecrets;
        private readonly HashSet<ValueEncoder> m_valueEncoders;
        private readonly HashSet<ValueSecret> m_valueSecrets;
        private ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    }
}
