using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Account
{
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AccountException", "GitHub.Services.Account.AccountException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountNotFoundException", "GitHub.Services.Account.AccountNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountExistsException", "GitHub.Services.Account.AccountExistsException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountNameTemporarilyReservedException", "GitHub.Services.Account.AccountNameTemporarilyReservedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountNameReservedException", "GitHub.Services.Account.AccountNameReservedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountPropertyException", "GitHub.Services.Account.AccountPropertyException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountMarkedForDeletionException", "GitHub.Services.Account.CollectionMarkedForDeletionException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountNotMarkedForDeletionException", "GitHub.Services.Account.CollectionNotMarkedForDeletionException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountTrialException", "GitHub.Services.Account.AccountTrialException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountIsNotActiveException", "GitHub.Services.Account.AccountIsNotActiveException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountFeatureNotAvailableException", "GitHub.Services.Account.AccountFeatureNotAvailableException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountAlreadyInTrialException", "GitHub.Services.Account.AccountAlreadyInTrialException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountTrialExpiredException", "GitHub.Services.Account.AccountTrialExpiredException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "MaxNumberAccountsException", "GitHub.Services.Account.MaxNumberAccountsException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "MaxNumberAccountsPerUserException", "GitHub.Services.Account.MaxNumberAccountsPerUserException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountHostMappingNotFoundException", "GitHub.Services.Account.AccountHostMappingNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountServiceLockDownModeException", "GitHub.Services.Account.AccountServiceLockDownModeException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountServiceUnavailableException", "GitHub.Services.Account.AccountServiceUnavailableException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountUserNotFoundException", "GitHub.Services.Account.AccountUserNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "BadServiceSettingNameException", "GitHub.Services.Account.BadServiceSettingNameException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "InvalidAccountOwnerException", "GitHub.Services.Account.InvalidAccountOwnerException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AADGuestUserCannotBeAnAccountOwnerException", "GitHub.Services.Account.AADGuestUserCannotBeAnAccountOwnerException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountAlreadyLinkedException", "GitHub.Services.Account.AccountAlreadyLinkedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountNotLinkedException", "GitHub.Services.Account.AccountNotLinkedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "AccountStateNotValidException", "GitHub.Services.Account.AccountStateNotValidException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AccountStateNotValidException : AccountException
    {
        public AccountStateNotValidException(string message) : base(message) { }

        public AccountStateNotValidException(string message, Exception innerException) : base(message, innerException) { }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "InvalidUserTypeException", "GitHub.Services.Account.InvalidUserTypeException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
    [ExceptionMapping("0.0", "3.0", "UnableToDeleteAzureLinkedAccountException", "GitHub.Services.Account.UnableToDeleteAzureLinkedAccountException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class UnableToDeleteAzureLinkedAccountException : AccountException
    {
        public UnableToDeleteAzureLinkedAccountException() : base(AccountResources.AccountMustBeUnlinkedBeforeDeletion()) { }
    }

    /// <summary>
    /// Thrown when the target AAD tenant does not exist
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "AadTenantNotFoundException", "GitHub.Services.Account.AadTenantNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AadTenantNotFoundException : AccountException
    {
        public AadTenantNotFoundException(string message) : base(message) { }

        public AadTenantNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
