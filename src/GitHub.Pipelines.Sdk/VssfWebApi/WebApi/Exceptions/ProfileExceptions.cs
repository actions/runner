using GitHub.Services.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProfileException", "GitHub.Services.Profile.ProfileException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProfileException : VssServiceException
    {
        public ProfileException()
        {
            this.Data.Add(CircuitBreaker.Command.DontTriggerCircuitBreaker, true);
        }

        public ProfileException(string message)
            : base(message)
        {
            this.Data.Add(CircuitBreaker.Command.DontTriggerCircuitBreaker, true);
        }

        public ProfileException(string message, Exception innerException)
            : base(message, innerException)
        {
            this.Data.Add(CircuitBreaker.Command.DontTriggerCircuitBreaker, true);
        }

        protected ProfileException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.Data.Add(CircuitBreaker.Command.DontTriggerCircuitBreaker, true);
        }
    }

    #region Common Exceptions

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BadProfileRequestException", "GitHub.Services.Profile.BadProfileRequestException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadProfileRequestException : ProfileException
    {
        protected BadProfileRequestException()
        {
        }

        public BadProfileRequestException(string message)
            : base(message)
        {
        }

        public BadProfileRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadProfileRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProfileServiceUnavailableException", "GitHub.Services.Profile.ProfileServiceUnavailableException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProfileServiceUnavailableException : VssServiceException
    {
        public ProfileServiceUnavailableException()
        {
        }

        public ProfileServiceUnavailableException(string message)
            : base(message)
        {
        }

        public ProfileServiceUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ProfileServiceUnavailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion

    #region Avatar related exceptions

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AvatarTooBigException", "GitHub.Services.Profile.AvatarTooBigException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AvatarTooBigException : ProfileException
    {
        public AvatarTooBigException()
        {
        }

        public AvatarTooBigException(string message)
            : base(message)
        {
        }

        public AvatarTooBigException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AvatarTooBigException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AvatarTooBigException", "GitHub.Services.Profile.AvatarTooBigException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AvatarTooSmallException : ProfileException
    {
        public AvatarTooSmallException()
        {
        }

        public AvatarTooSmallException(string message)
            : base(message)
        {
        }

        public AvatarTooSmallException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AvatarTooSmallException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BadAvatarValueException", "GitHub.Services.Profile.BadAvatarValueException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadAvatarValueException : BadProfileRequestException
    {
        public BadAvatarValueException()
        {
        }

        public BadAvatarValueException(string message)
            : base(message)
        {
        }

        public BadAvatarValueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadAvatarValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion

    #region Profile operations related exceptions

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProfileDoesNotExistException", "GitHub.Services.Profile.ProfileDoesNotExistException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProfileDoesNotExistException : ProfileException
    {
        public ProfileDoesNotExistException()
        {
        }

        public ProfileDoesNotExistException(string message)
            : base(message)
        {
        }

        public ProfileDoesNotExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ProfileDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProfileAlreadyExistsException", "GitHub.Services.Profile.ProfileAlreadyExistsException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProfileAlreadyExistsException : ProfileException
    {
        public ProfileAlreadyExistsException()
        {
        }

        public ProfileAlreadyExistsException(string message)
            : base(message)
        {
        }

        public ProfileAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ProfileAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "NewerVersionOfProfileExists", "GitHub.Services.Profile.NewerVersionOfProfileExists, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class NewerVersionOfProfileExists : ProfileException
    {
        public NewerVersionOfProfileExists()
        {
        }

        public NewerVersionOfProfileExists(string message)
            : base(message)
        {
        }

        public NewerVersionOfProfileExists(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BadServiceSettingNameException", "GitHub.Services.Profile.BadServiceSettingNameException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadServiceSettingNameException : BadProfileRequestException
    {
        public BadServiceSettingNameException()
        {
        }

        public BadServiceSettingNameException(string message)
            : base(message)
        {
        }

        public BadServiceSettingNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadServiceSettingNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ServiceSettingNotFoundException", "GitHub.Services.Profile.ServiceSettingNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ServiceSettingNotFoundException : ProfileResourceNotFoundException
    {
        public ServiceSettingNotFoundException()
        {
        }

        public ServiceSettingNotFoundException(string message)
            : base(message)
        {
        }

        public ServiceSettingNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ServiceSettingNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProfileResourceNotFoundException", "GitHub.Services.Profile.ProfileResourceNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProfileResourceNotFoundException : ProfileException
    {
        protected ProfileResourceNotFoundException()
        {
        }

        public ProfileResourceNotFoundException(string message)
            : base(message)
        {
        }

        public ProfileResourceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ProfileResourceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion

    #region Attributes related exceptions

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProfileAttributeNotFoundException", "GitHub.Services.Profile.ProfileAttributeNotFoundException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProfileAttributeNotFoundException : ProfileException
    {
        public ProfileAttributeNotFoundException()
        {
        }

        public ProfileAttributeNotFoundException(string message) : base(message)
        {
        }

        public ProfileAttributeNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ProfileAttributeNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "NewerVersionOfResourceExistsException", "GitHub.Services.Profile.NewerVersionOfResourceExistsException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class NewerVersionOfResourceExistsException : ProfileException
    {
        public NewerVersionOfResourceExistsException()
        {
        }

        public NewerVersionOfResourceExistsException(string message)
            : base(message)
        {
        }

        public NewerVersionOfResourceExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected NewerVersionOfResourceExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AttributeValueTooBigException", "GitHub.Services.Profile.AttributeValueTooBigException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AttributeValueTooBigException : ProfileException
    {
        public AttributeValueTooBigException()
        {
        }

        public AttributeValueTooBigException(string message)
            : base(message)
        {
        }

        public AttributeValueTooBigException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AttributeValueTooBigException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [ExceptionMapping("0.0", "3.0", "BadAttributeValueException", "GitHub.Services.Profile.BadAttributeValueException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadAttributeValueException : BadProfileRequestException
    {
        public BadAttributeValueException()
        {
        }

        public BadAttributeValueException(string message)
            : base(message)
        {
        }

        public BadAttributeValueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadAttributeValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion

    #region Properties (PublicAlias, EmailAddress, DisplayName) related exceptions

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "PublicAliasAlreadyExistException", "GitHub.Services.Profile.PublicAliasAlreadyExistException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class PublicAliasAlreadyExistException : ProfileException
    {
        public PublicAliasAlreadyExistException()
        {
        }

        public PublicAliasAlreadyExistException(string message)
            : base(message)
        {
        }

        public PublicAliasAlreadyExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected PublicAliasAlreadyExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BadDisplayNameException", "GitHub.Services.Profile.BadDisplayNameException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadDisplayNameException : BadProfileRequestException
    {
        public BadDisplayNameException()
        {
        }

        public BadDisplayNameException(string message)
            : base(message)
        {
        }

        public BadDisplayNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadDisplayNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BadCountryNameException", "GitHub.Services.Profile.BadCountryNameException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadCountryNameException : BadProfileRequestException
    {
        public BadCountryNameException()
        {
        }

        public BadCountryNameException(string message)
            : base(message)
        {
        }

        public BadCountryNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadCountryNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BadPublicAliasException", "GitHub.Services.Profile.BadPublicAliasException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadPublicAliasException : BadProfileRequestException
    {
        public BadPublicAliasException()
        {
        }

        public BadPublicAliasException(string message)
            : base(message)
        {
        }

        public BadPublicAliasException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadPublicAliasException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "BadEmailAddressException", "GitHub.Services.Profile.BadEmailAddressException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class BadEmailAddressException : BadProfileRequestException
    {
        public BadEmailAddressException()
        {
        }

        public BadEmailAddressException(string message)
            : base(message)
        {
        }

        public BadEmailAddressException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadEmailAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion

    #region AEX

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "AexServiceUnavailableException", "GitHub.Services.Profile.AexServiceUnavailableException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class AexServiceUnavailableException : VssServiceException
    {
        public AexServiceUnavailableException()
        {
        }

        public AexServiceUnavailableException(string message)
            : base(message)
        {
        }

        public AexServiceUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AexServiceUnavailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    #endregion

    #region Authorization related

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProfileServiceSecurityException", "GitHub.Services.Profile.ProfileServiceSecurityException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProfileServiceSecurityException : ProfileException
    {
        public ProfileServiceSecurityException()
        {
        }

        public ProfileServiceSecurityException(string message)
            : base(message)
        {
        }

        public ProfileServiceSecurityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ProfileServiceSecurityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    [ExceptionMapping("0.0", "3.0", "ProfileNotAuthorizedException", "GitHub.Services.Profile.ProfileNotAuthorizedException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProfileNotAuthorizedException : ProfileException
    {
        public ProfileNotAuthorizedException()
        {
        }

        public ProfileNotAuthorizedException(string message, string url)
            : base(message)
        {
            HelpLink = url;
        }

        public ProfileNotAuthorizedException(string message, string url, Exception innerException)
            : base(message, innerException)
        {
            HelpLink = url;
        }

        protected ProfileNotAuthorizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public override sealed string HelpLink { get; set; }
    }
    #endregion
}
