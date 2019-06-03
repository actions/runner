using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineException : VssServiceException
    {
        public PipelineException(String message)
            : base(message)
        {
        }

        public PipelineException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes an exception from serialized data
        /// </summary>
        /// <param name="info">object holding the serialized data</param>
        /// <param name="context">context info about the source or destination</param>
        protected PipelineException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AmbiguousResourceSpecificationException : PipelineException
    {
        public AmbiguousResourceSpecificationException(String message)
            : base(message)
        {
        }

        public AmbiguousResourceSpecificationException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        protected AmbiguousResourceSpecificationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AmbiguousTaskSpecificationException : PipelineException
    {
        public AmbiguousTaskSpecificationException(String message)
            : base(message)
        {
        }

        public AmbiguousTaskSpecificationException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        protected AmbiguousTaskSpecificationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class InvalidPipelineOperationException : PipelineException
    {
        public InvalidPipelineOperationException(String message) : base(message)
        {
        }

        public InvalidPipelineOperationException(
            String message,
            Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidPipelineOperationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ResourceNotFoundException : PipelineException
    {
        public ResourceNotFoundException(String message)
            : base(message)
        {
        }

        public ResourceNotFoundException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        protected ResourceNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ResourceNotAuthorizedException : PipelineException
    {
        public ResourceNotAuthorizedException(String message)
            : base(message)
        {
        }

        public ResourceNotAuthorizedException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        protected ResourceNotAuthorizedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ResourceValidationException : PipelineException
    {
        public ResourceValidationException(String message)
            : base(message)
        {
        }

        public ResourceValidationException(
            String message,
            String propertyName)
            : base(message)
        {
            this.PropertyName = propertyName;
        }

        public ResourceValidationException(
            String message,
            String propertyName,
            Exception innerException)
            : base(message, innerException)
        {
            this.PropertyName = propertyName;
        }

        public ResourceValidationException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        protected ResourceValidationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the property name of the resource which caused the error.
        /// </summary>
        public String PropertyName
        {
            get;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class StageNotFoundException : PipelineException
    {
        public StageNotFoundException(String message)
            : base(message)
        {
        }

        public StageNotFoundException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        protected StageNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineValidationException : PipelineException
    {
        public PipelineValidationException()
            : this(PipelineStrings.PipelineNotValid())
        {
        }

        // Report first 2 error messages, due to space limit on printing this error message in UI
        public PipelineValidationException(IEnumerable<PipelineValidationError> errors)
            : this(PipelineStrings.PipelineNotValidWithErrors(string.Join(",", (errors ?? Enumerable.Empty<PipelineValidationError>()).Take(2).Select(e => e.Message))))
        {
            m_errors = new List<PipelineValidationError>(errors ?? Enumerable.Empty<PipelineValidationError>());
        }

        public PipelineValidationException(String message)
            : base(message)
        {
        }

        public PipelineValidationException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        public IList<PipelineValidationError> Errors
        {
            get
            {
                if (m_errors == null)
                {
                    m_errors = new List<PipelineValidationError>();
                }
                return m_errors;
            }
        }

        /// <summary>
        /// Initializes an exception from serialized data
        /// </summary>
        /// <param name="info">object holding the serialized data</param>
        /// <param name="context">context info about the source or destination</param>
        protected PipelineValidationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        private List<PipelineValidationError> m_errors;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MaxJobExpansionException : PipelineValidationException
    {
        public MaxJobExpansionException(IEnumerable<PipelineValidationError> errors)
            : base(errors)
        {
        }

        public MaxJobExpansionException(String message)
            : base(message)
        {
        }

        public MaxJobExpansionException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }
        protected MaxJobExpansionException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
