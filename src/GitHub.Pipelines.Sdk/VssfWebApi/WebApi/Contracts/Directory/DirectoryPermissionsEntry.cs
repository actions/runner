using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Directories
{
    [DataContract]
    public class DirectoryPermissionsEntry
    {
        public Guid NamespaceId => namespaceId;

        public string Token => token;

        public int Allow => allow;

        public int Deny => deny;

        public bool Merge => merge;

        public DirectoryPermissionsEntry(
            Guid namespaceId,
            string token,
            int allow = 0,
            int deny = 0,
            bool merge = true)
        {
            this.namespaceId = namespaceId;
            this.token = token;
            this.allow = allow;
            this.deny = deny;
            this.merge = merge;
        }

        #region Internals

        [DataMember]
        private readonly Guid namespaceId;
        [DataMember]
        private readonly string token;
        [DataMember]
        private readonly int allow;
        [DataMember]
        private readonly int deny;
        [DataMember]
        private readonly bool merge;

        #endregion
    }
}
