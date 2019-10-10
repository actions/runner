using GitHub.Services.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.Location
{
    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ServiceDefinitionDoesNotExistException", "GitHub.Services.Location.ServiceDefinitionDoesNotExistException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class ServiceDefinitionDoesNotExistException : VssServiceException
    {
        public ServiceDefinitionDoesNotExistException(String message)
            : base(message)
        {
        }

        public ServiceDefinitionDoesNotExistException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ServiceDefinitionDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidAccessPointException", "GitHub.Services.Location.InvalidAccessPointException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class InvalidAccessPointException : VssServiceException
    {
        public InvalidAccessPointException(String message)
            : base(message)
        {
        }

        public InvalidAccessPointException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidAccessPointException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidServiceDefinitionException", "GitHub.Services.Location.InvalidServiceDefinitionException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class InvalidServiceDefinitionException : VssServiceException
    {
        public InvalidServiceDefinitionException(String message)
            : base(message)
        {
        }

        public InvalidServiceDefinitionException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected InvalidServiceDefinitionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "CannotChangeParentDefinitionException", "GitHub.Services.Location.CannotChangeParentDefinitionException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class CannotChangeParentDefinitionException : VssServiceException
    {
        public CannotChangeParentDefinitionException()
        {
        }

        public CannotChangeParentDefinitionException(String message)
            : base(GetMessage(message))
        {
        }

        public CannotChangeParentDefinitionException(String message, Exception ex)
            : base(GetMessage(message), ex)
        {
        }

        protected CannotChangeParentDefinitionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private static String GetMessage(String message)
        {
#if DEBUG
            String helpText = " Are you trying to move an existing Resource Area to a different service? You will need to follow the steps at: https://vsowiki.com/index.php?title=Moving_an_existing_Resource_Area_to_a_different_Service";
            if (message != null && !message.EndsWith(helpText, StringComparison.Ordinal))
            {
                message = String.Concat(message, helpText);
            }
#endif
            return message;
        }
    }

    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ParentDefinitionNotFoundException", "GitHub.Services.Location.ParentDefinitionNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class ParentDefinitionNotFoundException : VssServiceException
    {
        public ParentDefinitionNotFoundException(String serviceType, Guid identifier, String parentServiceType, Guid serviceInstance)
            : this(LocationResources.ParentDefinitionNotFound(serviceType, identifier, parentServiceType, serviceInstance))
        {
        }

        public ParentDefinitionNotFoundException(String message)
            : base(message)
        {
        }

        public ParentDefinitionNotFoundException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ParentDefinitionNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ActionDeniedBySubscriberException", "GitHub.Services.Location.ActionDeniedBySubscriberException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class ActionDeniedBySubscriberException : VssServiceException
    {
        public ActionDeniedBySubscriberException(String message)
            : base(message)
        {
        }

        public ActionDeniedBySubscriberException(String message, Exception ex)
            : base(message, ex)
        {
        }

        protected ActionDeniedBySubscriberException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
