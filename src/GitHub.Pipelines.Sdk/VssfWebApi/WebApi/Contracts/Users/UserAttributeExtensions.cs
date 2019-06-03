using System;
using System.Collections.Generic;
using System.Reflection;

namespace GitHub.Services.Users
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
