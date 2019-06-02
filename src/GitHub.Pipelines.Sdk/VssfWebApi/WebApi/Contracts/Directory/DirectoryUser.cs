using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Directories
{
    [DataContract]
    internal class DirectoryUser : DirectoryEntity, IDirectoryUser
    {
        public string Department
        {
            get { return this[DirectoryUserProperty.Department] as string; }
            internal set { this[DirectoryUserProperty.Department] = value; }
        }
        
        public bool? Guest
        {
            get { return this[DirectoryUserProperty.Guest] as bool?; }
            internal set { this[DirectoryUserProperty.Guest] = value; }
        }

        public string JobTitle
        {
            get { return this[DirectoryUserProperty.JobTitle] as string; }
            internal set { this[DirectoryUserProperty.JobTitle] = value; }
        }
        
        public string Mail
        {
            get { return this[DirectoryUserProperty.Mail] as string; }
            internal set { this[DirectoryUserProperty.Mail] = value; }
        }
        
        public string MailNickname
        {
            get { return this[DirectoryUserProperty.MailNickname] as string; }
            internal set { this[DirectoryUserProperty.MailNickname] = value; }
        }
        
        public string PhysicalDeliveryOfficeName
        {
            get { return this[DirectoryUserProperty.PhysicalDeliveryOfficeName] as string; }
            internal set { this[DirectoryUserProperty.PhysicalDeliveryOfficeName] = value; }
        }
        
        public string SignInAddress
        {
            get { return this[DirectoryUserProperty.SignInAddress] as string; }
            internal set { this[DirectoryUserProperty.SignInAddress] = value; }
        }
        
        public string Surname
        {
            get { return this[DirectoryUserProperty.Surname] as string; }
            internal set { this[DirectoryUserProperty.Surname] = value; }
        }

        public string TelephoneNumber
        {
            get { return this[DirectoryUserProperty.TelephoneNumber] as string; }
            internal set { this[DirectoryUserProperty.TelephoneNumber] = value; }
        }

        #region Internals

        internal DirectoryUser()
        {
            EntityType = DirectoryEntityType.User;
        }

        [JsonConstructor]
        private DirectoryUser(
            string entityId,
            string entityType,
            string originDirectory,
            string originId,
            string localDirectory,
            string localId,
            string principalName,
            string displayName,
            SubjectDescriptor? subjectDescriptor,
            string scopeName,
            string localDescriptor,
            DirectoryPermissionsEntry[] localPermissions,
            string department,
            bool? guest,            
            string jobTitle,
            string mail,
            string mailNickName,
            string physicalDeliveryOfficeName,
            string signInAddress,
            string surname,
            string telephoneNumber,
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
                  subjectDescriptor: subjectDescriptor,
                  scopeName: scopeName,
                  localDescriptor: localDescriptor,
                  localPermissions: localPermissions)
        {
            Properties.SetIfNotNull(DirectoryUserProperty.Department, department);
            Properties.SetIfNotNull(DirectoryUserProperty.Guest, guest);
            Properties.SetIfNotNull(DirectoryUserProperty.Active, active);
            Properties.SetIfNotNull(DirectoryUserProperty.JobTitle, jobTitle);
            Properties.SetIfNotNull(DirectoryUserProperty.Mail, mail);
            Properties.SetIfNotNull(DirectoryUserProperty.PhysicalDeliveryOfficeName, physicalDeliveryOfficeName);
            Properties.SetIfNotNull(DirectoryUserProperty.SignInAddress, signInAddress);
            Properties.SetIfNotNull(DirectoryUserProperty.Surname, surname);
            Properties.SetIfNotNull(DirectoryUserProperty.TelephoneNumber, telephoneNumber);
        }

        #endregion
    }
}
