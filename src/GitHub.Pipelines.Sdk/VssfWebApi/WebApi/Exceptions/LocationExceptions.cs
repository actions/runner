using Microsoft.VisualStudio.Services.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Location
{
    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ServiceDefinitionDoesNotExistException", "Microsoft.VisualStudio.Services.Location.ServiceDefinitionDoesNotExistException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "InvalidAccessPointException", "Microsoft.VisualStudio.Services.Location.InvalidAccessPointException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "InvalidServiceDefinitionException", "Microsoft.VisualStudio.Services.Location.InvalidServiceDefinitionException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "CannotChangeParentDefinitionException", "Microsoft.VisualStudio.Services.Location.CannotChangeParentDefinitionException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "ParentDefinitionNotFoundException", "Microsoft.VisualStudio.Services.Location.ParentDefinitionNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "ActionDeniedBySubscriberException", "Microsoft.VisualStudio.Services.Location.ActionDeniedBySubscriberException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
