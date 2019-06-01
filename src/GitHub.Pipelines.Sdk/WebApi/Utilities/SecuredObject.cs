using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    [DataContract]
    public sealed class SecuredObject : BaseSecuredObject
    {
        public SecuredObject()
        {
        }

        public SecuredObject(Guid namespaceId, int requiredPermission, string token)
        {
            this.m_namespaceId = namespaceId;
            this.m_requiredPermissions = requiredPermission;
            this.m_token = token;
        }
    }
}