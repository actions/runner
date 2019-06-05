using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Provides information about an error which occurred during validation.
    /// </summary>
    [DataContract]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TemplateValidationError
    {
        public TemplateValidationError()
        {
        }

        public TemplateValidationError(String message)
            : this(null, message)
        {
        }

        public TemplateValidationError(
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

        public static IEnumerable<TemplateValidationError> Create(Exception exception)
        {
            for (int i = 0; i < 50; i++)
            {
                yield return new TemplateValidationError(exception.Message);
                if (exception.InnerException == null)
                {
                    break;
                }

                exception = exception.InnerException;
            }
        }
    }
}
