using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Directories
{
    [DataContract]
    internal class DirectoryGroup : DirectoryEntity, IDirectoryGroup
    {
        public string Description
        {
            get { return this[DirectoryGroupProperty.Description] as string; }
            internal set { this[DirectoryGroupProperty.Description] = value; }
        }
        
        public string Mail
        {
            get { return this[DirectoryGroupProperty.Mail] as string; }
            internal set { this[DirectoryGroupProperty.Mail] = value; }
        }
        
        public string MailNickname
        {
            get { return this[DirectoryGroupProperty.MailNickname] as string; }
            internal set { this[DirectoryGroupProperty.MailNickname] = value; }
        }

        #region Internals

        internal DirectoryGroup()
        {
            EntityType = DirectoryEntityType.Group;
        }

        [JsonConstructor]
        private DirectoryGroup(
            string entityId,
            string entityType,
            string originDirectory,
            string originId,
            string localDirectory,
            string localId,
            string principalName,
            string displayName,
            string scopeName,
            SubjectDescriptor? subjectDescriptor,
            string localDescriptor,
            DirectoryPermissionsEntry[] localPermissions,
            string description,
            string mail,
            string mailNickname,
            bool? active)
            : base(
                  entityId: entityId,
                  entityType: entityType,
                  originDirectory: originDirectory,
                  originId: originId,
                  localDirectory: localDirectory,
                  localId: localId,
                  principalName: principalName,
                  displayName: displayName,
                  scopeName: scopeName,
                  subjectDescriptor: subjectDescriptor,
                  localDescriptor: localDescriptor,
                  localPermissions: localPermissions)
        {
            Properties.SetIfNotNull(DirectoryGroupProperty.Description, description);
            Properties.SetIfNotNull(DirectoryGroupProperty.Mail, mail);
            Properties.SetIfNotNull(DirectoryGroupProperty.MailNickname, mailNickname);
            Properties.SetIfNotNull(DirectoryGroupProperty.Active, active);
        }

        #endregion
    }
}
