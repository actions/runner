using System;
using System.Runtime.Serialization;

namespace GitHub.Runner.Worker
{
    public class ActionNotFoundException : Exception
    {
        public ActionNotFoundException(Uri actionUri, string requestId)
            : base(FormatMessage(actionUri, requestId))
        {
        }

        public ActionNotFoundException(string message)
            : base(message)
        {
        }

        public ActionNotFoundException(string message, System.Exception inner)
            : base(message, inner)
        {
        }

        protected ActionNotFoundException(SerializationInfo info, StreamingContext context)
                : base(info, context)
        {
        }

        private static string FormatMessage(Uri actionUri, string requestId)
        {
            if (!string.IsNullOrEmpty(requestId))
            {
                return $"An action could not be found at the URI '{actionUri}' ({requestId})";
            }

            return $"An action could not be found at the URI '{actionUri}'";
        }
    }
}
