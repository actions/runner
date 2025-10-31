#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Actions.WorkflowParser
{
    /// <summary>
    /// Provides information about an error which occurred during workflow validation.
    /// </summary>
    [DataContract]
    public class WorkflowValidationError
    {
        public WorkflowValidationError()
        {
        }

        public WorkflowValidationError(String? message)
            : this(null, message)
        {
        }

        public WorkflowValidationError(
            String? code,
            String? message)
        {
            Code = code;
            Message = message;
        }

        [DataMember(EmitDefaultValue = false)]
        public String? Code
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String? Message
        {
            get;
            set;
        }

        internal WorkflowValidationError Clone()
        {
            return new WorkflowValidationError(Code, Message);
        }

        public static IEnumerable<WorkflowValidationError> Create(Exception exception)
        {
            for (int i = 0; i < 50; i++)
            {
                yield return new WorkflowValidationError(exception.Message);
                if (exception.InnerException == null)
                {
                    break;
                }

                exception = exception.InnerException;
            }
        }
    }
}
