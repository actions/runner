using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

namespace GitHub.Services.FileContainer
{
    /// <summary>
    /// Represents an item in a container.
    /// </summary>
    [DataContract]
    public class FileContainerItem
    {
        /// <summary>
        /// Container Id.
        /// </summary>
        [DataMember(IsRequired = true)]
        public Int64 ContainerId { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Project Id.
        /// </summary>
        [DataMember(IsRequired = false)]
        public Guid ScopeIdentifier { get;[EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Unique path that identifies the item.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Path
        {
            get
            {
                return m_path;
            }
            [EditorBrowsable(EditorBrowsableState.Never)] 
            set
            {
                m_path = EnsurePathFormat(value);
            }
        }

        /// <summary>
        /// Type of the item: Folder, File or String.
        /// </summary>
        [DataMember(IsRequired = true)]
        public ContainerItemType ItemType { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Status of the item: Created or Pending Upload.
        /// </summary>
        [DataMember(IsRequired = true)]
        public ContainerItemStatus Status { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Length of the file. Zero if not of a file.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int64 FileLength { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Hash value of the file. Null if not a file.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Byte[] FileHash { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Encoding of the file. Zero if not a file.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 FileEncoding { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Type of the file. Zero if not a file.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 FileType { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Creation date.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime DateCreated { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Last modified date.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime DateLastModified { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Creator.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid CreatedBy { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Modifier.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid LastModifiedBy { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Location of the item resource.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String ItemLocation { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Download Url for the content of this item.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String ContentLocation { get; [EditorBrowsable(EditorBrowsableState.Never)] set; }

        /// <summary>
        /// Id of the file content.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Int32 FileId { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public byte[] ContentId { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Ticket { get; set; }
        
        public static string EnsurePathFormat(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            // We always make sure that the path is rooted
            StringBuilder sb = new StringBuilder();
            String[] components = path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (components.Length == 0)
            {
                return string.Empty;
            }

            for (int i = 0; i < components.Length; i++)
            {
                sb.AppendFormat("{0}{1}", components[i], i == components.Length - 1 ? String.Empty : "/");
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            FileContainerItem other = obj as FileContainerItem;
            if (other == null)
            {
                return false;
            }
            return this.ContainerId == other.ContainerId &&
                    this.ScopeIdentifier == other.ScopeIdentifier &&
                    this.Path == other.Path &&
                    this.ItemType == other.ItemType;
        }
        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        private string m_path;
    }
}
