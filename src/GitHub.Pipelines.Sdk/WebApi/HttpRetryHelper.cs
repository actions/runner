using Microsoft.VisualStudio.Services.Common;
using System;
using System.ComponentModel;
using System.Threading;

namespace Microsoft.VisualStudio.Services.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class HttpRetryHelper
    {
        /// <param name="maxAttempts">The total number of attempts to invoke the submitted action with. A value of 1 indicates that no retries will be attempted. Note that this was renamed from "maxRetries" to match the behavior of the parameter (i.e. maxRetries was previously behaving like maxAttempts).</param>
        /// <param name="canRetryDelegate">Evaluation function which returns true for a given exception if that exception is permitted to be retried.</param>
        public HttpRetryHelper(Int32 maxAttempts, Func<Exception, Boolean> canRetryDelegate = null)
        {
            m_maxAttempts = maxAttempts;
            m_canRetryDelegate = canRetryDelegate;
        }


        public void Invoke(Action action)
        {
            Int32 remainingAttempts;
            Invoke(action, out remainingAttempts);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        public void Invoke(Action action,
                           out Int32 remainingAttempts)
        {
            remainingAttempts = m_maxAttempts;

            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch (Exception exception)
                {
                    if ((VssNetworkHelper.IsTransientNetworkException(exception) ||
                        (m_canRetryDelegate != null && m_canRetryDelegate(exception)))
                        && remainingAttempts > 1)
                    {
                        Sleep(remainingAttempts);
                        remainingAttempts--;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public TResult Invoke<TResult>(Func<TResult> function)
        {
            Int32 remainingAttempts;
            return Invoke(function, out remainingAttempts);
        }

        public TResult Invoke<TResult>(Func<TResult> function,
                                       out Int32 remainingAttempts)
        {
            remainingAttempts = m_maxAttempts;

            while (true)
            {
                try
                {
                    return function();
                }
                catch (Exception exception)
                {
                    if ((VssNetworkHelper.IsTransientNetworkException(exception) ||
                                            (m_canRetryDelegate != null && m_canRetryDelegate(exception)))
                                            && remainingAttempts > 1)
                    {
                        Sleep(remainingAttempts);
                        remainingAttempts--;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        protected virtual void Sleep(Int32 remainingAttempts)
        {
            Thread.Sleep(BackoffTimerHelper.GetExponentialBackoff((m_maxAttempts - remainingAttempts) + 1, s_minBackoff, s_maxBackoff, s_deltaBackoff));
        }

        public Int32 MaxAttempts
        {
            get
            {
                return m_maxAttempts;
            }
        }

        private Int32 m_maxAttempts;
        private Func<Exception, Boolean> m_canRetryDelegate;
        private static TimeSpan s_minBackoff = TimeSpan.FromSeconds(1);
        private static TimeSpan s_maxBackoff = TimeSpan.FromMinutes(1);
        private static TimeSpan s_deltaBackoff = TimeSpan.FromSeconds(1);
    }
}
