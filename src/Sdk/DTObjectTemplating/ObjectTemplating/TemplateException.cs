using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.ObjectTemplating
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TemplateException : VssServiceException
    {
        public TemplateException(String message)
            : base(message)
        {
        }

        public TemplateException(
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
        protected TemplateException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TemplateValidationException : TemplateException
    {
        public TemplateValidationException()
            : this(TemplateStrings.TemplateNotValid())
        {
        }

        public TemplateValidationException(IEnumerable<TemplateValidationError> errors)
            : this(TemplateStrings.TemplateNotValidWithErrors(string.Join(",", (errors ?? Enumerable.Empty<TemplateValidationError>()).Select(e => e.Message))))
        {
            m_errors = new List<TemplateValidationError>(errors ?? Enumerable.Empty<TemplateValidationError>());
        }

        public TemplateValidationException(
            String message,
            IEnumerable<TemplateValidationError> errors)
            : this(message)
        {
            m_errors = new List<TemplateValidationError>(errors ?? Enumerable.Empty<TemplateValidationError>());
        }

        public TemplateValidationException(String message)
            : base(message)
        {
        }

        public TemplateValidationException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        public IList<TemplateValidationError> Errors
        {
            get
            {
                if (m_errors == null)
                {
                    m_errors = new List<TemplateValidationError>();
                }
                return m_errors;
            }
        }

        /// <summary>
        /// Initializes an exception from serialized data
        /// </summary>
        /// <param name="info">object holding the serialized data</param>
        /// <param name="context">context info about the source or destination</param>
        protected TemplateValidationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        private List<TemplateValidationError> m_errors;
    }
}
