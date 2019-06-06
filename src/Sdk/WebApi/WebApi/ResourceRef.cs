using System;
using System.Runtime.Serialization;

namespace GitHub.Services.WebApi
{
    [DataContract]
    public class ResourceRef : ISecuredObject
    {
        public ResourceRef()
        {
        }

        public ResourceRef(ISecuredObject securedObject)
        {
            m_securedObject = securedObject;
        }

        [DataMember(Name = "id")]
        public String Id { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public String Url { get; set; }

        public void SetSecuredObject(ISecuredObject securedObject)
        {
            m_securedObject = securedObject;
        }

        private ISecuredObject SecuredObject
        {
            get
            {
                if (m_securedObject == null)
                {
                    throw new InvalidOperationException("SecuredObject required but not set.");
                }

                return m_securedObject;
            }
        }

        Guid ISecuredObject.NamespaceId => SecuredObject.NamespaceId;

        int ISecuredObject.RequiredPermissions => SecuredObject.RequiredPermissions;

        string ISecuredObject.GetToken()
        {
            return SecuredObject.GetToken();
        }

        private ISecuredObject m_securedObject;
    }
}
