using System;
using System.ComponentModel;
using System.Threading;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Marks HTTP requests sent within the scope as retries of an earlier, failed attempt made by
    /// a caller-level retry loop (for example, reconnecting after a failed long poll).
    /// <see cref="VssHttpRetryMessageHandler"/> reads the ambient retry number and sends it in the
    /// <c>X-Is-Retry</c> header.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpRetryScope
    {
        private static readonly AsyncLocal<int> s_retryNumber = new AsyncLocal<int>();

        /// <summary>
        /// The ambient 1-based retry number, or 0 when the current call is not a retry.
        /// </summary>
        public static int Current => s_retryNumber.Value;

        /// <summary>
        /// Sets the ambient retry number until the returned scope is disposed. A value of 0 or less
        /// marks the calls within the scope as first attempts.
        /// </summary>
        public static IDisposable Begin(int retryNumber)
        {
            return new Scope(Math.Max(retryNumber, 0));
        }

        private sealed class Scope : IDisposable
        {
            private readonly int m_previous;
            private bool m_disposed;

            public Scope(int retryNumber)
            {
                m_previous = s_retryNumber.Value;
                s_retryNumber.Value = retryNumber;
            }

            public void Dispose()
            {
                if (!m_disposed)
                {
                    s_retryNumber.Value = m_previous;
                    m_disposed = true;
                }
            }
        }
    }
}
