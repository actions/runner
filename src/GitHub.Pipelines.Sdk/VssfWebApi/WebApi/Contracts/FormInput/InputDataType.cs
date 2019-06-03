using System.Runtime.Serialization;

namespace GitHub.Services.FormInput
{
    /// <summary>
    /// Enumerates data types that are supported as subscription input values.
    /// </summary>
    [DataContract]
    public enum InputDataType
    {
        /// <summary>
        /// No data type is specified.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Represents a textual value.
        /// </summary>
        [EnumMember]
        String = 10,

        /// <summary>
        /// Represents a numberic value.
        /// </summary>
        [EnumMember]
        Number = 20,

        /// <summary>
        /// Represents a value of true or false.
        /// </summary>
        [EnumMember]
        Boolean = 30,

        /// <summary>
        /// Represents a Guid.
        /// </summary>
        [EnumMember]
        Guid = 40,

        /// <summary>
        /// Represents a URI.
        /// </summary>
        [EnumMember]
        Uri = 50
    }
}
