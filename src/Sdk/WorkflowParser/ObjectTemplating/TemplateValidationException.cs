#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating
{
    public class TemplateValidationException : Exception
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

        private List<TemplateValidationError> m_errors;
    }
}
