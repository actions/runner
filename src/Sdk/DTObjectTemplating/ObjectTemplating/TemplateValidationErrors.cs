using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Provides information about an error which occurred during validation.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class TemplateValidationErrors : IEnumerable<TemplateValidationError>
    {
        public TemplateValidationErrors()
        {
        }

        public TemplateValidationErrors(
            Int32 maxErrors,
            Int32 maxMessageLength)
        {
            m_maxErrors = maxErrors;
            m_maxMessageLength = maxMessageLength;
        }

        public Int32 Count => m_errors.Count;

        public void Add(String message)
        {
            Add(new TemplateValidationError(message));
        }

        public void Add(Exception ex)
        {
            Add(null, ex);
        }

        public void Add(String messagePrefix, Exception ex)
        {
            for (int i = 0; i < 50; i++)
            {
                String message = !String.IsNullOrEmpty(messagePrefix) ? $"{messagePrefix} {ex.Message}" : ex.ToString();
                Add(new TemplateValidationError(message));
                if (ex.InnerException == null)
                {
                    break;
                }

                ex = ex.InnerException;
            }
        }

        public void Add(IEnumerable<TemplateValidationError> errors)
        {
            foreach (var error in errors)
            {
                Add(error);
            }
        }

        public void Add(TemplateValidationError error)
        {
            // Check max errors
            if (m_maxErrors <= 0 ||
                m_errors.Count < m_maxErrors)
            {
                // Check max message length
                if (m_maxMessageLength > 0 &&
                    error.Message?.Length > m_maxMessageLength)
                {
                    error = new TemplateValidationError(error.Code, error.Message.Substring(0, m_maxMessageLength) + "[...]");
                }

                m_errors.Add(error);
            }
        }

        /// <summary>
        /// Throws <c ref="TemplateValidationException" /> if any errors.
        /// </summary>
        public void Check()
        {
            if (m_errors.Count > 0)
            {
                throw new TemplateValidationException(m_errors);
            }
        }

        /// <summary>
        /// Throws <c ref="TemplateValidationException" /> if any errors.
        /// <param name="prefix">The error message prefix</param>
        /// </summary>
        public void Check(String prefix)
        {
            if (String.IsNullOrEmpty(prefix))
            {
                this.Check();
            }
            else if (m_errors.Count > 0)
            {
                var message = $"{prefix.Trim()} {String.Join(",", m_errors.Select(e => e.Message))}";
                throw new TemplateValidationException(message, m_errors);
            }
        }

        public void Clear()
        {
            m_errors.Clear();
        }

        public IEnumerator<TemplateValidationError> GetEnumerator()
        {
            return (m_errors as IEnumerable<TemplateValidationError>).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (m_errors as IEnumerable).GetEnumerator();
        }

        private readonly List<TemplateValidationError> m_errors = new List<TemplateValidationError>();
        private readonly Int32 m_maxErrors;
        private readonly Int32 m_maxMessageLength;
    }
}
