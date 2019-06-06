using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.Services.Organization
{
    [Serializable]
    public class OrganizationException : VssServiceException
    {
        public OrganizationException()
        { }

        public OrganizationException(string message)
            : base(message)
        { }

        public OrganizationException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected OrganizationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    #region Common Exceptions

    [Serializable]
    public class OrganizationBadRequestException : OrganizationException
    {
        protected OrganizationBadRequestException()
        {
        }

        public OrganizationBadRequestException(string message)
            : base(message)
        {
        }

        public OrganizationBadRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected OrganizationBadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion

    #region Organization operations related exceptions

    [Serializable]
    public class OrganizationNotFoundException : OrganizationException
    {
        public OrganizationNotFoundException()
        {
        }

        public OrganizationNotFoundException(string message)
            : base(message)
        {
        }

        public OrganizationNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected OrganizationNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class OrganizationAlreadyExistsException : OrganizationException
    {
        public OrganizationAlreadyExistsException()
        {
        }

        public OrganizationAlreadyExistsException(string message)
            : base(message)
        {
        }

        public OrganizationAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected OrganizationAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class AnotherHostReservedNameException : OrganizationException
    {
        public AnotherHostReservedNameException()
        {
        }

        public AnotherHostReservedNameException(string message)
            : base(message)
        {
        }

        public AnotherHostReservedNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AnotherHostReservedNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class HostHasPendingRenameException : OrganizationException
    {
        public HostHasPendingRenameException()
        {
        }

        public HostHasPendingRenameException(string message)
            : base(message)
        {
        }

        public HostHasPendingRenameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected HostHasPendingRenameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class ActivatedOrganizationAlreadyExists : OrganizationException
    {
        public ActivatedOrganizationAlreadyExists()
        {
        }

        public ActivatedOrganizationAlreadyExists(string message)
            : base(message)
        {
        }

        public ActivatedOrganizationAlreadyExists(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ActivatedOrganizationAlreadyExists(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CollectionAlreadyExistsException : OrganizationException
    {
        public CollectionAlreadyExistsException()
        {
        }

        public CollectionAlreadyExistsException(string message)
            : base(message)
        {
        }

        public CollectionAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CollectionAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CollectionNotFoundException : OrganizationException
    {
        public CollectionNotFoundException()
        {
        }

        public CollectionNotFoundException(string message)
            : base(message)
        {
        }

        public CollectionNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CollectionNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CollectionMarkedForDeletionException : OrganizationException
    {
        public CollectionMarkedForDeletionException()
        {
        }

        public CollectionMarkedForDeletionException(string message)
            : base(message)
        {
        }

        public CollectionMarkedForDeletionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CollectionMarkedForDeletionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CollectionNotMarkedForDeletionException : OrganizationException
    {
        public CollectionNotMarkedForDeletionException()
        {
        }

        public CollectionNotMarkedForDeletionException(string message)
            : base(message)
        {
        }

        public CollectionNotMarkedForDeletionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CollectionNotMarkedForDeletionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CollectionNameException : OrganizationException
    {
        public CollectionNameException()
        {
        }

        public CollectionNameException(string message)
            : base(message)
        {
        }

        public CollectionNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CollectionNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class OrganizationNameException : OrganizationException
    {
        public OrganizationNameException()
        {
        }

        public OrganizationNameException(string message)
            : base(message)
        {
        }

        public OrganizationNameException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        protected OrganizationNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CollectionCreationLimitsReachedException : OrganizationException
    {
        public CollectionCreationLimitsReachedException ()
        {
        }

        public CollectionCreationLimitsReachedException (string message)
            : base(message)
        {
        }

        public CollectionCreationLimitsReachedException (string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CollectionCreationLimitsReachedException (SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CollectionCreationRuleFailedException : OrganizationException
    {
        public CollectionCreationRuleFailedException()
        {
        }

        public CollectionCreationRuleFailedException(string message)
            : base(message)
        {
        }

        public CollectionCreationRuleFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CollectionCreationRuleFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class CollectionCreationException : OrganizationException
    {
        public CollectionCreationException()
        {
        }

        public CollectionCreationException(string message)
            : base(message)
        {
        }

        public CollectionCreationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CollectionCreationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class RegionNotAvailableException : OrganizationException
    {
        public RegionNotAvailableException()
        {
        }

        public RegionNotAvailableException(string message)
            : base(message)
        {
        }

        public RegionNotAvailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected RegionNotAvailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion

    #region Authorization related

    [Serializable]
    public class OrganizationServiceSecurityException : OrganizationException
    {
        public OrganizationServiceSecurityException()
        {
        }

        public OrganizationServiceSecurityException(string message)
            : base(message)
        {
        }

        public OrganizationServiceSecurityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected OrganizationServiceSecurityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    #endregion
}
