using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides information about an error which occurred during pipeline validation.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PipelineValidationErrors : IEnumerable<PipelineValidationError>
    {
        public PipelineValidationErrors()
        {
        }

        public PipelineValidationErrors(
            Int32 maxErrors,
            Int32 maxMessageLength)
        {
            m_maxErrors = maxErrors;
            m_maxMessageLength = maxMessageLength;
        }

        public Int32 Count => m_errors.Count;

        public void Add(String message)
        {
            Add(new PipelineValidationError(message));
        }

        public void Add(Exception ex)
        {
            Add(null, ex);
        }

        public void Add(String messagePrefix, Exception ex)
        {
            for (int i = 0; i < 50; i++)
            {
                String message = !String.IsNullOrEmpty(messagePrefix) ? $"{messagePrefix} {ex.Message}" : ex.Message;
                Add(new PipelineValidationError(message));
                if (ex.InnerException == null)
                {
                    break;
                }

                ex = ex.InnerException;
            }
        }

        public void Add(IEnumerable<PipelineValidationError> errors)
        {
            foreach (var error in errors)
            {
                Add(error);
            }
        }

        public void Add(PipelineValidationError error)
        {
            // Check max errors
            if (m_maxErrors <= 0 ||
                m_errors.Count < m_maxErrors)
            {
                // Check max message length
                if (m_maxMessageLength > 0 &&
                    error.Message?.Length > m_maxMessageLength)
                {
                    error = new PipelineValidationError(error.Code, error.Message.Substring(0, m_maxMessageLength) + "[...]");
                }

                m_errors.Add(error);
            }
        }

        public void Clear()
        {
            m_errors.Clear();
        }

        public IEnumerator<PipelineValidationError> GetEnumerator()
        {
            return (m_errors as IEnumerable<PipelineValidationError>).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (m_errors as IEnumerable).GetEnumerator();
        }

        private readonly List<PipelineValidationError> m_errors = new List<PipelineValidationError>();
        private readonly Int32 m_maxErrors;
        private readonly Int32 m_maxMessageLength;
    }
}
