using System;
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
    public class PipelineValidationError
    {
        public PipelineValidationError()
        {
        }

        public PipelineValidationError(String message)
            : this(null, message)
        {
        }

        public PipelineValidationError(
            String code,
            String message)
        {
            Code = code;
            Message = message;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Code
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Message
        {
            get;
            set;
        }

        public static IEnumerable<PipelineValidationError> Create(Exception exception)
        {
            for (int i = 0; i < 50; i++)
            {
                yield return new PipelineValidationError(exception.Message);
                if (exception.InnerException == null)
                {
                    break;
                }

                exception = exception.InnerException;
            }
        }
    }
}
