using System;
using System.Runtime.Serialization;

namespace GitHub.Runner.Worker
{
    public class ActionNotFoundException : Exception
    {
        public ActionNotFoundException(string actionUri)
            : base(FormatMessage(actionUri))
        {
            ActionUri = actionUri;
        }

        public ActionNotFoundException(string actionUri, string message)
            : base(message)
        {
            ActionUri = actionUri;
        }

        public ActionNotFoundException(string actionUri, string message, System.Exception inner)
            : base(message, inner)
        {
            ActionUri = actionUri;
        }

        protected ActionNotFoundException(SerializationInfo info, StreamingContext context)
                : base(info, context)
        {
        }

        public string ActionUri { get; }

        private static string FormatMessage(string actionUri)
        {
            return $"An action could not be found at the URI '{actionUri}'";
        }
    }
}