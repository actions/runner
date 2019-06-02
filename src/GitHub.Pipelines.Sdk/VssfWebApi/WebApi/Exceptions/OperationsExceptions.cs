using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Security;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Operations
{
    /// <summary>
    /// This exception is thrown when multiple plugin have the same Id.
    /// </summary>
    [Serializable]
    public class OperationPluginWithSameIdException : VssServiceException
    {
        public OperationPluginWithSameIdException()
        {
        }

        public OperationPluginWithSameIdException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public OperationPluginWithSameIdException(String message)
            : base(message)
        {
        }

        public OperationPluginWithSameIdException(Guid pluginId)
            : base(WebApiResources.OperationPluginWithSameIdException(pluginId))
        {
        }

        protected OperationPluginWithSameIdException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when we can't find an operation.
    /// </summary>
    [Serializable]
    public class OperationNotFoundException : VssServiceException
    {
        public OperationNotFoundException()
        {
        }

        public OperationNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public OperationNotFoundException(String message)
            : base(message)
        {
        }

        public OperationNotFoundException(Guid operationId)
            : base(WebApiResources.OperationNotFoundException(operationId))
        {
        }

        protected OperationNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when we can't find a plugin.
    /// </summary>
    [Serializable]
    public class OperationPluginNotFoundException : VssServiceException
    {
        public OperationPluginNotFoundException()
        {
        }

        public OperationPluginNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public OperationPluginNotFoundException(String message)
            : base(message)
        {
        }

        public OperationPluginNotFoundException(Guid pluginId)
            : base(WebApiResources.OperationPluginNotFoundException(pluginId))
        {
        }

        protected OperationPluginNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This exception is thrown when permissions are missing to get the operation.
    /// </summary>
    [Serializable]
    public class OperationPluginNoPermission : SecurityException
    {
        public OperationPluginNoPermission(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public OperationPluginNoPermission(String message)
            : base(message)
        {
        }

        public OperationPluginNoPermission(Guid pluginId, Guid operationId)
            : base(WebApiResources.OperationPluginNoPermission(pluginId, operationId))
        {
        }
    }

    /// <summary>
    /// This exception is thrown when we fail to update an operation.
    /// </summary>
    [Serializable]
    public class OperationUpdateFailedException : VssServiceException
    {
        public OperationUpdateFailedException()
        {
        }

        public OperationUpdateFailedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        public OperationUpdateFailedException(String message)
            : base(message)
        {
        }

        public OperationUpdateFailedException(Guid operationId)
            : base(WebApiResources.OperationUpdateException(operationId))
        {
        }

        protected OperationUpdateFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
