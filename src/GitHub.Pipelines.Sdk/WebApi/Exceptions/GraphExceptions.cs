using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Graph
{
    [Serializable]
    public class GraphException : VssServiceException
    {
        public GraphException()
        { }

        public GraphException(string message)
            : base(message)
        { }

        public GraphException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected GraphException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    #region Common Exceptions

    [Serializable]
    public class GraphBadRequestException : GraphException
    {
        protected GraphBadRequestException()
        {
        }

        public GraphBadRequestException(string message)
            : base(message)
        {
        }

        public GraphBadRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GraphBadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidGraphMemberIdException : GraphException
    {
        protected InvalidGraphMemberIdException()
        {
        }

        public InvalidGraphMemberIdException(string message)
            : base(message)
        {
        }

        public InvalidGraphMemberIdException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidGraphMemberIdException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class GraphSubjectNotFoundException : GraphException
    {
        protected GraphSubjectNotFoundException()
        {
        }

        public GraphSubjectNotFoundException(string message)
            : base(message)
        {
        }

        public GraphSubjectNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GraphSubjectNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public GraphSubjectNotFoundException(SubjectDescriptor subjectDescriptor)
            : base(GraphResources.GraphSubjectNotFound(subjectDescriptor.ToString()))
        {
        }

        public GraphSubjectNotFoundException(Guid id)
            : base(IdentityResources.IdentityNotFoundWithTfid(id))
        {
        }
    }

    [Serializable]
    public class GraphMemberNotFoundException : GraphException
    {
        protected GraphMemberNotFoundException()
        {
        }

        public GraphMemberNotFoundException(string message)
            : base(message)
        {
        }

        public GraphMemberNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GraphMemberNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public GraphMemberNotFoundException(SubjectDescriptor subjectDescriptor, SubjectDescriptor containerDescriptor)
            : base(GraphResources.GraphMembershipNotFound(subjectDescriptor.ToString(), containerDescriptor.ToString()))
        {
        }
    }

    [Serializable]
    public class GraphMembershipNotFoundException : GraphException
    {
        protected GraphMembershipNotFoundException()
        {
        }

        public GraphMembershipNotFoundException(string message)
            : base(message)
        {
        }

        public GraphMembershipNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GraphMembershipNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public GraphMembershipNotFoundException(SubjectDescriptor subjectDescriptor, SubjectDescriptor containerDescriptor)
            : base(GraphResources.GraphMembershipNotFound(subjectDescriptor.ToString(), containerDescriptor.ToString()))
        {
        }
    }

    [Serializable]
    public class GraphApiUnavailableException : GraphException
    {
        protected GraphApiUnavailableException()
        {
        }

        public GraphApiUnavailableException(string message)
            : base(message)
        {
        }

        public GraphApiUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GraphApiUnavailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public GraphApiUnavailableException(SubjectDescriptor subjectDescriptor)
            : base(IdentityResources.IdentityNotFoundWithDescriptor(subjectDescriptor.SubjectType, subjectDescriptor.Identifier))
        {
        }

        public GraphApiUnavailableException(Guid id)
            : base(IdentityResources.IdentityNotFoundWithTfid(id))
        {
        }
    }

    #endregion

    [Serializable]
    public class GraphProviderInfoApiUnavailableException : GraphException
    {
        protected GraphProviderInfoApiUnavailableException()
        {
        }

        public GraphProviderInfoApiUnavailableException(string message)
            : base(message)
        {
        }

        public GraphProviderInfoApiUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GraphProviderInfoApiUnavailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public GraphProviderInfoApiUnavailableException(SubjectDescriptor subjectDescriptor)
            : base(IdentityResources.IdentityNotFoundWithDescriptor(subjectDescriptor.SubjectType, subjectDescriptor.Identifier))
        {
        }

        public GraphProviderInfoApiUnavailableException(Guid id)
            : base(IdentityResources.IdentityNotFoundWithTfid(id))
        {
        }
    }

    [Serializable]
    public class SubjectDescriptorNotFoundException : GraphException
    {
        public SubjectDescriptorNotFoundException()
        { }

        public SubjectDescriptorNotFoundException(string message)
            : base(message)
        { }

        public SubjectDescriptorNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected SubjectDescriptorNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public SubjectDescriptorNotFoundException(Guid storageKey)
            : base(GraphResources.SubjectDescriptorNotFoundWithStorageKey(storageKey))
        {
        }

        public SubjectDescriptorNotFoundException(IdentityDescriptor identityDescriptor)
            : base(GraphResources.SubjectDescriptorNotFoundWithIdentityDescriptor(identityDescriptor))
        {
        }
    }

    [Serializable]
    public class StorageKeyNotFoundException : GraphException
    {
        public StorageKeyNotFoundException()
        { }

        public StorageKeyNotFoundException(string message)
            : base(message)
        { }

        public StorageKeyNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected StorageKeyNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public StorageKeyNotFoundException(SubjectDescriptor descriptor)
            : base(GraphResources.StorageKeyNotFound(descriptor))
        {
        }
    }

    [Serializable]
    public class InvalidGetDescriptorRequestException : GraphException
    {
        public InvalidGetDescriptorRequestException()
        { }

        public InvalidGetDescriptorRequestException(string message)
            : base(message)
        { }

        public InvalidGetDescriptorRequestException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected InvalidGetDescriptorRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public InvalidGetDescriptorRequestException(Guid id)
            : base(IdentityResources.InvalidGetDescriptorRequestWithLocalId(id))
        {
        }
    }

    [Serializable]
    public class TooManyRequestedItemsException : GraphException
    {
        /// <summary>
        /// Gets the count of the requested items.
        /// Note: the value can be null based on whether the message disclose the limit.
        /// </summary>
        [DataMember]
        public int? RequestedCount { get; set; }

        /// <summary>
        /// Gets max limit for the requested items.
        /// Note: the value can be null based on whether the message disclose the limit.
        /// </summary>
        [DataMember]
        public int? MaxLimit { get; set; }

        public TooManyRequestedItemsException()
            : base(IdentityResources.TooManyRequestedItemsError())
        { }

        public TooManyRequestedItemsException(string message)
            : base(message)
        { }

        public TooManyRequestedItemsException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected TooManyRequestedItemsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public TooManyRequestedItemsException(int providedCount, int maxCount)
            : base(IdentityResources.TooManyRequestedItemsErrorWithCount(providedCount, maxCount))
        {
            this.RequestedCount = providedCount;
            this.MaxLimit = maxCount;
        }
    }

    [Serializable]
    public class InvalidGraphRequestException : GraphException
    {
        public InvalidGraphRequestException()
        { }

        public InvalidGraphRequestException(string message)
            : base(message)
        { }

        public InvalidGraphRequestException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected InvalidGraphRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    public class CannotEditChildrenOfNonGroupException : GraphException
    {
        public CannotEditChildrenOfNonGroupException()
        { }

        public CannotEditChildrenOfNonGroupException(string message)
            : base(message)
        { }

        public CannotEditChildrenOfNonGroupException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected CannotEditChildrenOfNonGroupException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public CannotEditChildrenOfNonGroupException(SubjectDescriptor subjectDescriptor)
            : base(GraphResources.CannotEditChildrenOfNonGroup(subjectDescriptor.ToString()))
        {
        }
    }

    [Serializable]
    public class InvalidSubjectTypeException : GraphException
    {
        public InvalidSubjectTypeException()
        { }

        public InvalidSubjectTypeException(string message)
        : base(message)
        { }

        public InvalidSubjectTypeException(string message, Exception innerException)
        : base(message, innerException)
        { }

        protected InvalidSubjectTypeException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        { }
    }

    [Serializable]
    public class GraphAccountNameCollisionRepairUnsafeException : GraphException
    {
        public GraphAccountNameCollisionRepairUnsafeException()
        { }

        public GraphAccountNameCollisionRepairUnsafeException(string message)
        : base(message)
        { }

        public GraphAccountNameCollisionRepairUnsafeException(string message, Exception innerException)
        : base(message, innerException)
        { }

        protected GraphAccountNameCollisionRepairUnsafeException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        { }
    }

    [Serializable]
    public class GraphAccountNameCollisionRepairFailedException : GraphException
    {
        public GraphAccountNameCollisionRepairFailedException()
        { }

        public GraphAccountNameCollisionRepairFailedException(string message)
        : base(message)
        { }

        public GraphAccountNameCollisionRepairFailedException(string message, Exception innerException)
        : base(message, innerException)
        { }

        protected GraphAccountNameCollisionRepairFailedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        { }
    }

    [Serializable]
    public class CannotUpdateWellKnownGraphGroupException : GraphException
    {
        public CannotUpdateWellKnownGraphGroupException()
        { }

        public CannotUpdateWellKnownGraphGroupException(string message)
        : base(message)
        { }

        public CannotUpdateWellKnownGraphGroupException(string message, Exception innerException)
        : base(message, innerException)
        { }

        protected CannotUpdateWellKnownGraphGroupException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        { }
    }
}