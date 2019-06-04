using System;

namespace GitHub.Services.WebApi
{
    public static class ServicingOrchestrationExceptionExtensions
    {
        /// <summary>
        /// Marks exception as fatal preventing ServicingOrchestration job to retry when this exception is thrown
        /// </summary>
        public static void MarkAsFatalServicingOrchestrationException(this Exception ex)
        {
            ex.Data[c_dontRetryServicingOrchestrationJobMarker] = null;
        }

        /// <summary>
        /// Marks exception as fatal preventing ServicingOrchestration job to retry when this exception is thrown
        /// </summary>
        public static T AsFatalServicingOrchestrationException<T>(this T ex) where T : Exception
        {
            ex.MarkAsFatalServicingOrchestrationException();
            return ex;
        }

        /// <summary>
        /// Checks whether exception is marked as fatal Orchestration exception
        /// </summary>
        public static bool IsFatalServicingOrchestrationException(this Exception ex)
        {
            return ex.Data.Contains(c_dontRetryServicingOrchestrationJobMarker);
        }

        /// <summary>
        /// Marks exception as Blocking Servicing, these exceptions should result in an unlimited number of retries, enforcing a timeout the responsibility of the marking the exception.
        /// </summary>
        public static void MarkAsBlockedServicingOrchestrationException(this Exception ex)
        {
            ex.Data[c_blockedServicingOrchestrationJobMarker] = null;
        }

        /// <summary>
        /// Marks exception as Blocking Servicing, these exceptions should result in an unlimited number of retries, enforcing a timeout the responsibility of the marking the exception.
        /// </summary>
        public static T AsBlockedServicingOrchestrationException<T>(this T ex) where T : Exception
        {
            ex.MarkAsBlockedServicingOrchestrationException();
            return ex;
        }

        /// <summary>
        /// Checks whether exception is marked as a Blocking Servicing Orchestration exception
        /// </summary>
        public static bool IsBlockedServicingOrchestrationException(this Exception ex)
        {
            return ex.Data.Contains(c_blockedServicingOrchestrationJobMarker);
        }

        /// <summary>
        /// Marks exception as user error preventing ServicingOrchestration job to retry when this exception is thrown
        /// </summary>
        public static void MarkAsUserErrorServicingOrchestrationException(this Exception ex, string errorMessage = null)
        {
            ex.Data[c_userErrorServicingOrchestrationJobMarker] = errorMessage;
            // all user-error exception are fatal exceptions
            ex.MarkAsFatalServicingOrchestrationException();
        }

        /// <summary>
        /// Marks exception as user error preventing ServicingOrchestration job to retry when this exception is thrown
        /// </summary>
        public static T AsUserErrorServicingOrchestrationException<T>(this T ex, string errorMessage) where T : Exception
        {
            ex.MarkAsUserErrorServicingOrchestrationException(errorMessage);
            // all user-error exception are fatal exceptions
            ex.MarkAsFatalServicingOrchestrationException();
            return ex;
        }

        /// <summary>
        /// Checks whether exception is marked as user error Orchestration exception
        /// </summary>
        public static bool IsUserErrorServicingOrchestrationException(this Exception ex)
        {
            return ex.Data.Contains(c_userErrorServicingOrchestrationJobMarker);
        }

        /// <summary>
        /// Gets the user error exception null when the exception is not a user error exception
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string GetUserErrorServicingOrchestrationExceptionMessage(this Exception ex)
        {
            // If this is a user error then return the message
            if (ex.IsUserErrorServicingOrchestrationException())
            {
                return ex.Message;
            }

            // otherwise check the inner exception recursively
            return ex.InnerException?.GetUserErrorServicingOrchestrationExceptionMessage();
        }

        /// <summary>
        /// Recursively try to find a specific error in the exception message
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static bool CheckServicingOrchestrationExceptionForError(this Exception ex, string error)
        {
            // if we find a match return true
            if (ex.Message.IndexOf(error, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // otherwise check the inner exception recursively
            if (ex.InnerException == null)
            {
                return false;
            }
            else
            {
                return ex.InnerException.CheckServicingOrchestrationExceptionForError(error);
            }
        }

        private const string c_dontRetryServicingOrchestrationJobMarker = "{89814AE3-E489-41F4-A022-B541E889F582}";
        private const string c_blockedServicingOrchestrationJobMarker = "{7012936B-1443-49C4-899E-D4BE91F648F3}";
        private const string c_userErrorServicingOrchestrationJobMarker = "{8EAF91AE-F583-435D-AA1E-058AC1B7200D}";
    }
}
