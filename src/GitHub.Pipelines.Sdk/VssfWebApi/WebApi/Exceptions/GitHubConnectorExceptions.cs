using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.GitHubConnector
{
    [Serializable]
    public class GitHubConnectorException : VssServiceException
    {
        public GitHubConnectorException()
        { }

        public GitHubConnectorException(string message)
            : base(message)
        { }

        public GitHubConnectorException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected GitHubConnectorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    public class BadRequestException : GitHubConnectorException
    {
        public BadRequestException()
        { }

        public BadRequestException(string message)
            : base(message)
        { }

        public BadRequestException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected BadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    public class ConnectionExistsException : GitHubConnectorException
    {
        public ConnectionExistsException()
        { }

        public ConnectionExistsException(string message)
            : base(message)
        { }

        public ConnectionExistsException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected ConnectionExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
