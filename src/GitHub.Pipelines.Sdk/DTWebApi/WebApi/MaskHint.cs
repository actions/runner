using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class MaskHint
    {
        public MaskHint()
        {
        }

        private MaskHint(MaskHint maskHintToBeCloned)
        {
            this.Type = maskHintToBeCloned.Type;
            this.Value = maskHintToBeCloned.Value;
        }

        public MaskHint Clone()
        {
            return new MaskHint(this);
        }

        [DataMember]
        public MaskType Type
        {
            get;
            set;
        }

        [DataMember]
        public String Value
        {
            get;
            set;
        }

        public override Boolean Equals(Object obj)
        {
            var otherHint = obj as MaskHint;
            if (otherHint != null)
            {
                return this.Type == otherHint.Type && String.Equals(this.Value ?? String.Empty, otherHint.Value ?? String.Empty, StringComparison.Ordinal);
            }

            return false;
        }

        public override Int32 GetHashCode()
        {
            return this.Type.GetHashCode() ^ (this.Value ?? String.Empty).GetHashCode();
        }
    }
}
