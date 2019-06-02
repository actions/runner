using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Account
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountException", "Microsoft.VisualStudio.Services.Account.AccountException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountException : VssServiceException
    {
        public AccountException(String message) : base(message)
        {
            EventId = VssEventId.AccountException;
        }

        public AccountException(String message, Exception innerException)
            : base(message, innerException)
        {
            EventId = VssEventId.AccountException;
        }
    }

    /// <summary>
    /// An exception thrown when an account could not be found.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountNotFoundException", "Microsoft.VisualStudio.Services.Account.AccountNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountNotFoundException : AccountException
    {
        public AccountNotFoundException()
            : base(AccountResources.AccountNotFound())
        {
            //We do not want the CircuitBreaker to trip on the actual AccountNotFoundException
            this.Data.Add(CircuitBreaker.Command.DontTriggerCircuitBreaker, true);
        }

        public AccountNotFoundException(Guid accountId)
            : this(accountId.ToString(null, CultureInfo.InvariantCulture))
        {
        }

        public AccountNotFoundException(String accountId)
            : base(AccountResources.AccountNotFoundByIdError(accountId))
        {
            //We do not want the CircuitBreaker to trip on the actual AccountNotFoundException
            //this.Data.Add(CircuitBreaker.Command.DontTriggerCircuitBreaker, true);
        }

        public AccountNotFoundException(String message, Exception innerException)
            : base(message, innerException)
        {
            //We do not want the CircuitBreaker to trip on the actual AccountNotFoundException
            //this.Data.Add(CircuitBreaker.Command.DontTriggerCircuitBreaker, true);
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountExistsException", "Microsoft.VisualStudio.Services.Account.AccountExistsException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountExistsException : AccountException
    {
        public AccountExistsException(String accountName)
            : base(AccountResources.AccountExists(accountName))
        {
        }

        public AccountExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountNameTemporarilyReservedException", "Microsoft.VisualStudio.Services.Account.AccountNameTemporarilyReservedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountNameTemporarilyReservedException : AccountExistsException
    {
        public AccountNameTemporarilyReservedException(String accountName, TimeSpan duration)
            : base(AccountResources.AccountNameTemporarilyUnavailable())
        { }

        public AccountNameTemporarilyReservedException(String message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountNameReservedException", "Microsoft.VisualStudio.Services.Account.AccountNameReservedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountNameReservedException : AccountException
    {
        public AccountNameReservedException(String accountName)
            : base(AccountResources.AccountExists(accountName))
        {
        }

        public AccountNameReservedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountPropertyException", "Microsoft.VisualStudio.Services.Account.AccountPropertyException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountPropertyException : AccountException
    {
        public AccountPropertyException(String message)
            : base(message)
        {

        }

        public AccountPropertyException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// An exception thrown when an account has been marked for deletion
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountMarkedForDeletionException", "Microsoft.VisualStudio.Services.Account.CollectionMarkedForDeletionException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountMarkedForDeletionException : AccountException
    {
        public AccountMarkedForDeletionException(String accountId)
            : base(AccountResources.AccountMarkedForDeletionError(accountId))
        {
        }

        public AccountMarkedForDeletionException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// An exception thrown when an account has not been marked for deletion
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountNotMarkedForDeletionException", "Microsoft.VisualStudio.Services.Account.CollectionNotMarkedForDeletionException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountNotMarkedForDeletionException : AccountException
    {
        public AccountNotMarkedForDeletionException(String accountId)
            : base(AccountResources.AccountNotMarkedForDeletionError(accountId))
        {
        }

        public AccountNotMarkedForDeletionException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountTrialException", "Microsoft.VisualStudio.Services.Account.AccountTrialException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountTrialException : AccountException
    {
        public AccountTrialException(string message)
            : this(message, null)
        {
        }

        public AccountTrialException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountIsNotActiveException", "Microsoft.VisualStudio.Services.Account.AccountIsNotActiveException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountIsNotActiveException : AccountException
    {
        public AccountIsNotActiveException(string message)
            : this(message, null)
        {
        }

        public AccountIsNotActiveException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountFeatureNotAvailableException", "Microsoft.VisualStudio.Services.Account.AccountFeatureNotAvailableException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountFeatureNotAvailableException : AccountException
    {
        public AccountFeatureNotAvailableException(string message)
            : this(message, null)
        {
        }

        public AccountFeatureNotAvailableException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountAlreadyInTrialException", "Microsoft.VisualStudio.Services.Account.AccountAlreadyInTrialException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountAlreadyInTrialException : AccountException
    {
        public AccountAlreadyInTrialException(string message)
            : this(message, null)
        {
        }

        public AccountAlreadyInTrialException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountTrialExpiredException", "Microsoft.VisualStudio.Services.Account.AccountTrialExpiredException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountTrialExpiredException : AccountException
    {
        public AccountTrialExpiredException(string message)
            : this(message, null)
        {
        }

        public AccountTrialExpiredException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "MaxNumberAccountsException", "Microsoft.VisualStudio.Services.Account.MaxNumberAccountsException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class MaxNumberAccountsException : AccountException
    {
        public MaxNumberAccountsException()
            : base(AccountResources.MaxNumberOfAccountsExceptions())
        {
        }

        public MaxNumberAccountsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "MaxNumberAccountsPerUserException", "Microsoft.VisualStudio.Services.Account.MaxNumberAccountsPerUserException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class MaxNumberAccountsPerUserException : AccountException
    {
        public MaxNumberAccountsPerUserException()
            : base(AccountResources.MaxNumberOfAccountsPerUserException())
        {
        }

        public MaxNumberAccountsPerUserException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// An exception thrown when an account could not be found.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountHostMappingNotFoundException", "Microsoft.VisualStudio.Services.Account.AccountHostMappingNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountHostMappingNotFoundException : AccountException
    {
        public AccountHostMappingNotFoundException(Guid hostId)
            : this(hostId.ToString(null, CultureInfo.InvariantCulture))
        {
        }

        public AccountHostMappingNotFoundException(String hostId)
            : base(AccountResources.AccountHostmappingNotFoundById(hostId))
        {
        }

        public AccountHostMappingNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountServiceLockDownModeException", "Microsoft.VisualStudio.Services.Account.AccountServiceLockDownModeException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountServiceLockDownModeException : AccountException
    {
        public AccountServiceLockDownModeException()
            : this(AccountResources.AccountServiceLockDownModeException())
        {
        }

        public AccountServiceLockDownModeException(string message)
            : this(message, null)
        {
        }

        public AccountServiceLockDownModeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountServiceUnavailableException", "Microsoft.VisualStudio.Services.Account.AccountServiceUnavailableException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountServiceUnavailableException : AccountException
    {
        public AccountServiceUnavailableException()
            : this(AccountResources.AccountServiceUnavailableException())
        {
        }

        public AccountServiceUnavailableException(string message)
            : this(message, null)
        {
        }

        public AccountServiceUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// thrown when trying to remove a user license that does not exist from an account
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountUserNotFoundException", "Microsoft.VisualStudio.Services.Account.AccountUserNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountUserNotFoundException : AccountException
    {
        public AccountUserNotFoundException(String userId, String accountId)
            : base(AccountResources.AccountUserNotFoundException(userId, accountId))
        {
        }

        public AccountUserNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BadServiceSettingNameException", "Microsoft.VisualStudio.Services.Account.BadServiceSettingNameException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadServiceSettingNameException : AccountException
    {
        public BadServiceSettingNameException(string message)
            : base(message)
        {
        }

        public BadServiceSettingNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "InvalidAccountOwnerException", "Microsoft.VisualStudio.Services.Account.InvalidAccountOwnerException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidAccountOwnerException : AccountException
    {
        public InvalidAccountOwnerException(string message)
            : base(message)
        {
        }

        public InvalidAccountOwnerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AADGuestUserCannotBeAnAccountOwnerException", "Microsoft.VisualStudio.Services.Account.AADGuestUserCannotBeAnAccountOwnerException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AADGuestUserCannotBeAnAccountOwnerException : AccountException
    {
        public AADGuestUserCannotBeAnAccountOwnerException(string message)
            : base(message)
        {
        }

        public AADGuestUserCannotBeAnAccountOwnerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when attempting to link an account to an AAD tenant when the account is already linked.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountAlreadyLinkedException", "Microsoft.VisualStudio.Services.Account.AccountAlreadyLinkedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountAlreadyLinkedException : AccountException
    {
        public AccountAlreadyLinkedException(string message) : base(message) { }

        public AccountAlreadyLinkedException(string message, Exception innerException) : base (message, innerException) { }
    }

    /// <summary>
    /// Thrown when attempting to unlink an account that is not linked to any tenant.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountNotLinkedException", "Microsoft.VisualStudio.Services.Account.AccountNotLinkedException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountNotLinkedException : AccountException
    {
        public AccountNotLinkedException(string message) : base(message) { }

        public AccountNotLinkedException(string message, Exception innerException) : base (message, innerException) { }
    }

    /// <summary>
    /// Thrown when attempting to link/unlink an account that is not in a valid state as per IMS.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountStateNotValidException", "Microsoft.VisualStudio.Services.Account.AccountStateNotValidException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountStateNotValidException : AccountException
    {
        public AccountStateNotValidException(string message) : base(message) { }

        public AccountStateNotValidException(string message, Exception innerException) : base(message, innerException) { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidUserTypeException", "Microsoft.VisualStudio.Services.Account.InvalidUserTypeException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class InvalidUserTypeException : AccountException
    {
        public InvalidUserTypeException(string message) : base(message) { }

        public InvalidUserTypeException(string message, Exception innerException) : base (message, innerException) { }
    }

    /// <summary>
    /// Thrown when an account a users attempts to softdelete an account that is linked to an azure subscription.
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "UnableToDeleteAzureLinkedAccountException", "Microsoft.VisualStudio.Services.Account.UnableToDeleteAzureLinkedAccountException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class UnableToDeleteAzureLinkedAccountException : AccountException
    {
        public UnableToDeleteAzureLinkedAccountException() : base(AccountResources.AccountMustBeUnlinkedBeforeDeletion()) { }
    }

    /// <summary>
    /// Thrown when the target AAD tenant does not exist
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AadTenantNotFoundException", "Microsoft.VisualStudio.Services.Account.AadTenantNotFoundException, Microsoft.VisualStudio.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AadTenantNotFoundException : AccountException
    {
        public AadTenantNotFoundException(string message) : base(message) { }

        public AadTenantNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
