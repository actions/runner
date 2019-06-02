using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Commerce
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "CommerceException", "Microsoft.VisualStudio.Services.Commerce.CommerceException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class CommerceException : VssServiceException
    {
        public CommerceException()
        {
        }

        public CommerceException(string message)
            : base(message)
        {
        }

        public CommerceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CommerceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #region Authorization Exceptions
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "CommerceSecurityException", "Microsoft.VisualStudio.Services.Commerce.CommerceSecurityException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class CommerceSecurityException : CommerceException
    {
        public CommerceSecurityException(string message)
            : base(message)
        {
        }

        public CommerceSecurityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    #endregion

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountQuantityException", "Microsoft.VisualStudio.Services.Commerce.AccountQuantityException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountQuantityException : CommerceException
    {
        public int ErrorNumber
        {
            get;
            set;
        }

        public AccountQuantityException(string message)
            : base(message)
        {
        }

        public AccountQuantityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AccountQuantityException(string message, int errorNumber)
            : base(message)
        {
            ErrorNumber = errorNumber;
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidResourceException", "Microsoft.VisualStudio.Services.Commerce.InvalidResourceException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidResourceException : CommerceException
    {
        public InvalidResourceException(string message)
            : base(message)
        {
        }

        public InvalidResourceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "OfferMeterNotFoundException", "Microsoft.VisualStudio.Services.Commerce.OfferMeterNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class OfferMeterNotFoundException : CommerceException
    {
        public OfferMeterNotFoundException(string nameOrGalleryId)
            : base(CommerceResources.OfferMeterNotFoundExceptionMessage(nameOrGalleryId))
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "UnsupportedSubscriptionTypeException", "Microsoft.VisualStudio.Services.Commerce.UnsupportedSubscriptionTypeException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class UnsupportedSubscriptionTypeException : CommerceException
    {
        public UnsupportedSubscriptionTypeException()
            : base(CommerceResources.UnsupportedSubscriptionTypeExceptionMessage())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "UserIsNotSubscriptionAdminException", "Microsoft.VisualStudio.Services.Commerce.UserIsNotSubscriptionAdminException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class UserIsNotSubscriptionAdminException : CommerceException
    {

        public UserIsNotSubscriptionAdminException()
            : base(CommerceResources.UserIsNotSubscriptionAdmin())
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "UserIsNotAccountOwnerException", "Microsoft.VisualStudio.Services.Commerce.UserIsNotAccountOwnerException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class UserIsNotAccountOwnerException : CommerceException
    {
        public UserIsNotAccountOwnerException(Identity.Identity identity, string collectionName)
            : base(CommerceResources.UserNotAccountAdministrator(identity.DisplayName, collectionName))
        {
            Identity = identity;
            CollectionName = collectionName;
        }

        public UserIsNotAccountOwnerException(String userEmail, String accountName)
            : base(CommerceResources.UserNotAccountAdministrator(userEmail, accountName))
        {
            Email = userEmail;
            AccountName = accountName;
        }

        public Identity.Identity Identity { get; set; }
        public String CollectionName { get; set; }
        public Guid IdentityId { get; set; }
        public String Email { get; set; }
        public String AccountName { get; set; }
            
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ReportingViewNotSupportedException", "Microsoft.VisualStudio.Services.Commerce.ReportingViewNotSupportedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ReportingViewNotSupportedException : CommerceException
    {
        public ReportingViewNotSupportedException(string message)
            : base(message)
        {
        }

        public ReportingViewNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "ReportingViewInvalidFilterException", "Microsoft.VisualStudio.Services.Commerce.ReportingViewInvalidFilterException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ReportingViewInvalidFilterException : CommerceException
    {
        public ReportingViewInvalidFilterException(string message)
            : base(message)
        {
        }

        public ReportingViewInvalidFilterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
