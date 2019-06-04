using System;
using System.Runtime.Serialization;

namespace GitHub.Services.WebApi
{
    [DataContract]
    public abstract class BaseSecuredObject : ISecuredObject
    {
        protected BaseSecuredObject()
        {
        }

        protected BaseSecuredObject(ISecuredObject securedObject)
        {
            if (securedObject != null)
            {
                this.m_namespaceId = securedObject.NamespaceId;
                this.m_requiredPermissions = securedObject.RequiredPermissions;
                this.m_token = securedObject.GetToken();
            }
        }
        
        Guid ISecuredObject.NamespaceId
        {
            get
            {
                return m_namespaceId;
            }
        }

        int ISecuredObject.RequiredPermissions
        {
            get
            {
                return m_requiredPermissions;
            }
        }

        string ISecuredObject.GetToken()
        {
            return m_token;
        }

        internal Guid m_namespaceId;
        internal int m_requiredPermissions;
        internal string m_token;
    }
}
