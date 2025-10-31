#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating
{
    /// <summary>
    /// Provides information about an error which occurred during validation.
    /// </summary>
    [DataContract]
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

        [DataMember(Name = "code", EmitDefaultValue = false)]
        public String Code
        {
            get;
            set;
        }

        [DataMember(Name = "Message", EmitDefaultValue = false)]
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
