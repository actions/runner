using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.VisualStudio.Services.Users
{
    public static class UserAttributeExtensions
    {
        public static UserAttribute ToUserAttribute(this SetUserAttributeParameters attributeParameters)
        {
            return new UserAttribute
            {
                Name = attributeParameters.Name,
                Value = attributeParameters.Value,
                LastModified = attributeParameters.LastModified,
                Revision = attributeParameters.Revision
            };
        }
    }
}
