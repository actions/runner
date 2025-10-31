#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.Actions.WorkflowParser
{
    public class WorkflowValidationException : Exception
    {
        public WorkflowValidationException()
            : this(WorkflowStrings.WorkflowNotValid())
        {
        }

        public WorkflowValidationException(IEnumerable<WorkflowValidationError> errors)
            : this(WorkflowStrings.WorkflowNotValidWithErrors(string.Join(" ", (errors ?? Enumerable.Empty<WorkflowValidationError>()).Take(ErrorCount).Select(e => e.Message))))
        {
            m_errors = new List<WorkflowValidationError>(errors ?? Enumerable.Empty<WorkflowValidationError>());
        }

        public WorkflowValidationException(String message)
            : base(message)
        {
        }

        public WorkflowValidationException(
            String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        internal IReadOnlyList<WorkflowValidationError> Errors => (m_errors ?? new List<WorkflowValidationError>()).AsReadOnly();

        private List<WorkflowValidationError>? m_errors;

        /// <summary>
        /// Previously set to 2 when there were UI limitations.
        /// Setting this to 10 to increase the number of errors returned from parser.
        /// </summary>
        private const int ErrorCount = 10;
    }
}
