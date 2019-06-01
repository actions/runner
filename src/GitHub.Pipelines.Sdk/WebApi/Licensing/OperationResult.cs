using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Licensing
{
    public enum OperationResult
    {
        [EnumMember]
        Success = 0,

        [EnumMember]
        Warning = 1,

        [EnumMember]
        Error = 2
    }
}
