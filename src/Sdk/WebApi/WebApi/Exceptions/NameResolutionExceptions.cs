using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.NameResolution
{
    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public partial class MultiplePrimaryNameResolutionEntriesException : VssServiceException
    {
        public MultiplePrimaryNameResolutionEntriesException(String message)
            : base(message)
        {
        }

        public MultiplePrimaryNameResolutionEntriesException(String message, Exception ex)
            : base(message, ex)
        {
        }

        public MultiplePrimaryNameResolutionEntriesException(String value, Int32 dummy)
            : base(NameResolutionResources.MultiplePrimaryNameResolutionEntriesError(value))
        {
            Value = value;
        }

        protected MultiplePrimaryNameResolutionEntriesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public String Value { get; set; }
    }

    [Serializable]
    [SuppressMessageAttribute("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public partial class NameResolutionEntryAlreadyExistsException : VssServiceException
    {
        public NameResolutionEntryAlreadyExistsException(String message)
            : base(message)
        {
        }

        public NameResolutionEntryAlreadyExistsException(String message, Exception ex)
            : base(message, ex)
        {
        }

        public NameResolutionEntryAlreadyExistsException(String name, String value, String conflictingValue)
            : base(NameResolutionResources.NameResolutionEntryAlreadyExistsError(name, value, conflictingValue))
        {
            Name = name;
            Value = value;
            ConflictingValue = conflictingValue;
        }

        protected NameResolutionEntryAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public String Name { get; set; }
        public String Value { get; set; }
        public String ConflictingValue { get; set; }
    }
}
