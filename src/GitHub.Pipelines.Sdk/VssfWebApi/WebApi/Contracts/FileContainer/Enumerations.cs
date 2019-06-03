using System;
using System.Runtime.Serialization;

namespace GitHub.Services.FileContainer
{
    /// <summary>
    /// Options a container can have.
    /// </summary>
    [Flags]
    [DataContract]
    public enum ContainerOptions
    {
        /// <summary>
        /// No option.
        /// </summary>
        [EnumMember]
        None = 0,

        ///// <summary>
        ///// Encrypts content of the container.
        ///// </summary>
        //EncryptContent = 1
    }

    /// <summary>
    /// Type of a container item.
    /// </summary>
    [DataContract]
    public enum ContainerItemType
    {
        /// <summary>
        /// Any item type.
        /// </summary>
        [EnumMember]
        Any = 0,

        /// <summary>
        /// Item is a folder which can have child items.
        /// </summary>
        [EnumMember]
        Folder = 1,

        /// <summary>
        /// Item is a file which is stored in the file service.
        /// </summary>
        [EnumMember]
        File = 2,
    }

    /// <summary>
    /// Status of a container item.
    /// </summary>
    [DataContract]
    public enum ContainerItemStatus
    {
        /// <summary>
        /// Item is created.
        /// </summary>
        [EnumMember]
        Created = 1,

        /// <summary>
        /// Item is a file pending for upload.
        /// </summary>
        [EnumMember]
        PendingUpload = 2
    }
}
