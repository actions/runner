using GitHub.Services.Common;
using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Users
{
    [Serializable]    
    public class UserException : VssServiceException
    {
        public UserException()
        {
        }

        public UserException(String message)
            : base(message)
        {
        }

        public UserException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class BadAvatarValueException : UserException
    {
        public BadAvatarValueException()
        {
        }

        public BadAvatarValueException(String message)
            : base(message)
        {
        }

        public BadAvatarValueException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadAvatarValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class BadMailAddressException : UserException
    {
        public BadMailAddressException()
        {
        }

        public BadMailAddressException(String message)
            : base(message)
        {
        }

        public BadMailAddressException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadMailAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class BadUserRequestException : UserException
    {
        public BadUserRequestException()
        {
        }

        public BadUserRequestException(String message)
            : base(message)
        {
        }

        public BadUserRequestException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BadUserRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class InvalidAttributeNameException : UserException
    {
        public InvalidAttributeNameException()
        {
        }

        public InvalidAttributeNameException(String message)
            : base(message)
        {
        }

        public InvalidAttributeNameException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidAttributeNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class InvalidAttributeValueException : UserException
    {
        public InvalidAttributeValueException()
        {
        }

        public InvalidAttributeValueException(String message)
            : base(message)
        {
        }

        public InvalidAttributeValueException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidAttributeValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class InvalidBatchException : UserException
    {
        public InvalidBatchException()
        {
        }

        public InvalidBatchException(String message)
            : base(message)
        {
        }

        public InvalidBatchException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidBatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class InvalidCountryException : UserException
    {
        public InvalidCountryException()
        {
        }

        public InvalidCountryException(String message)
            : base(message)
        {
        }

        public InvalidCountryException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidCountryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class InvalidQueryPatternException : UserException
    {
        public InvalidQueryPatternException()
        {
        }

        public InvalidQueryPatternException(String message)
            : base(message)
        {
        }

        public InvalidQueryPatternException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidQueryPatternException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class InvalidMailConfirmationException : UserException
    {
        public InvalidMailConfirmationException()
        {
        }

        public InvalidMailConfirmationException(String message)
            : base(message)
        {
        }

        public InvalidMailConfirmationException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidMailConfirmationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class InvalidSubjectDescriptorException : UserException
    {
        public InvalidSubjectDescriptorException()
        {
        }

        public InvalidSubjectDescriptorException(String message)
            : base(message)
        {
        }

        public InvalidSubjectDescriptorException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidSubjectDescriptorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class InvalidUserCreateException : UserException
    {
        public InvalidUserCreateException()
        {
        }

        public InvalidUserCreateException(String message)
            : base(message)
        {
        }

        public InvalidUserCreateException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidUserCreateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class InvalidUserUpdateException : UserException
    {
        public InvalidUserUpdateException()
        {
        }

        public InvalidUserUpdateException(String message)
            : base(message)
        {
        }

        public InvalidUserUpdateException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidUserUpdateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class RegionNotSupportedException : UserException
    {
        public RegionNotSupportedException()
        {
        }

        public RegionNotSupportedException(String message)
            : base(message)
        {
        }

        public RegionNotSupportedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected RegionNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class UserAlreadyExistsException : UserException
    {
        public UserAlreadyExistsException()
        {
        }

        public UserAlreadyExistsException(String message)
            : base(message)
        {
        }

        public UserAlreadyExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class UserAttributeDoesNotExistException : UserException
    {
        public UserAttributeDoesNotExistException()
        {
        }

        public UserAttributeDoesNotExistException(String message)
            : base(message)
        {
        }

        public UserAttributeDoesNotExistException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserAttributeDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class UserDoesNotExistException : UserException
    {
        public UserDoesNotExistException()
        {
        }

        public UserDoesNotExistException(String message)
            : base(message)
        {
        }

        public UserDoesNotExistException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]   
    public class UserServiceUnavailableException : UserException
    {
        public UserServiceUnavailableException()
        {
        }

        public UserServiceUnavailableException(String message)
            : base(message)
        {
        }

        public UserServiceUnavailableException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserServiceUnavailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class UserAccountMappingDoesNotExistException : UserException
    {
        public UserAccountMappingDoesNotExistException()
        {
        }

        public UserAccountMappingDoesNotExistException(string message)
            : base(message)
        {
        }

        public UserAccountMappingDoesNotExistException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserAccountMappingDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class UserAccessCheckException : UserException
    {
        public UserAccessCheckException()
        {
        }

        public UserAccessCheckException(string message)
            : base(message)
        {
        }

        public UserAccessCheckException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserAccessCheckException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class UserRequestDiscardedException : UserException
    {
        public UserRequestDiscardedException()
        {
        }

        public UserRequestDiscardedException(string message)
            : base(message)
        {
        }

        public UserRequestDiscardedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UserRequestDiscardedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class SetUserAttributesException : UserException
    {
        public SetUserAttributesException()
        {
        }

        public SetUserAttributesException(string message)
            : base(message)
        {
        }

        public SetUserAttributesException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected SetUserAttributesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
