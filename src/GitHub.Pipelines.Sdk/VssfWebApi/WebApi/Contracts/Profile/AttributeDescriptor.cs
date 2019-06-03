using GitHub.Services.Common;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    /// <summary>
    /// Identifies an attribute with a name and a container.
    /// </summary>
    public class AttributeDescriptor : IComparable<AttributeDescriptor>, ICloneable
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AttributeDescriptor(string containerName, string attributeName)
        {
            //Validation in setters...
            AttributeName = attributeName;
            ContainerName = containerName;
        }

        /// <summary>
        /// The name of the attribute.
        /// </summary>
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public string AttributeName
        {
            get
            {
                return m_attributeName;
            }
            set
            {
                ProfileArgumentValidation.ValidateAttributeName(value);
                m_attributeName = value;
            }
        }

        /// <summary>
        /// The container the attribute resides in.
        /// </summary>
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public string ContainerName
        {
            get
            {
                return m_containerName;
            }
            set
            {
                ProfileArgumentValidation.ValidateContainerName(value);
                m_containerName = value;
            }
        }

        private string m_attributeName;
        private string m_containerName;

        public int CompareTo(AttributeDescriptor obj)
        {
            if (this == obj) return 0;
            if (obj == null) return 1;

            int retValue;
            if ((retValue = VssStringComparer.AttributesDescriptor.Compare(this.AttributeName, obj.AttributeName)) != 0)
            {
                return retValue;
            }

            return VssStringComparer.AttributesDescriptor.Compare(this.ContainerName, obj.ContainerName);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return CompareTo((AttributeDescriptor) obj) == 0;
        }

        public override int GetHashCode()
        {
            return this.ContainerName.GetHashCode() + this.AttributeName.GetHashCode();
        }

        public object Clone()
        {
            return new AttributeDescriptor(ContainerName, AttributeName);
        }

        public override string ToString()
        {
            return string.Concat(ContainerName,";",AttributeName);
        }
    }

    /// <summary>
    /// Class used for comparing AttributeDescriptors
    /// </summary>
    public class AttributeDescriptorComparer : IComparer<AttributeDescriptor>, IEqualityComparer<AttributeDescriptor>
    {
        private AttributeDescriptorComparer() { }

        public int Compare(AttributeDescriptor x, AttributeDescriptor y)
        {
            if (x == y) return 0;
            if (x == null && y != null) return -1;
            if (x != null && y == null) return 1;

            return (x.CompareTo(y));
        }

        public bool Equals(AttributeDescriptor x, AttributeDescriptor y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(AttributeDescriptor obj)
        {
            return obj.GetHashCode();
        }

        public static AttributeDescriptorComparer Instance
        {
            get
            {
                return s_instance;
            }
        }

        private static AttributeDescriptorComparer s_instance = new AttributeDescriptorComparer();
    }


}

