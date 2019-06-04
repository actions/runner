using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Common.TokenStorage
{
    /// <summary>
    /// 
    /// </summary>
    public class VssTokenKey
    {
        /// <summary>
        /// 
        /// </summary>
        public VssTokenKey()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="resource"></param>
        /// <param name="userName"></param>
        /// <param name="type"></param>
        public VssTokenKey(
            String kind,
            String resource,
            String userName,
            String type)
        {
            Kind = kind;
            Resource = resource;
            UserName = userName;
            Type = type;
        }

        /// <summary>
        /// The token kind (e.g., "vss-user", "vss-account", "windows-store").
        /// </summary>
        /// <remarks>
        /// The token kind is case-sensitive.
        /// </remarks>
        public String Kind
        {
            get 
            { 
                return m_kind; 
            }

            set 
            {
                ArgumentUtility.CheckStringForNullOrEmpty(value, "kind", true);
                ArgumentUtility.CheckStringForInvalidCharacters(value, "kind", s_invalidKindCharacters);
                m_kind = value; 
            }
        }

        /// <summary>
        /// The token resource name or uri.
        /// </summary>
        /// <remarks>
        /// The token resource is case-sensitive.
        /// </remarks>
        public String Resource
        {
            get
            { 
                return m_resource; 
            }
            
            set 
            {
                ArgumentUtility.CheckStringForNullOrEmpty(value, "resource", true);
                m_resource = value; 
            }
        }

        /// <summary>
        /// The token user name, user id, or any app-specific unique value.
        /// </summary>
        /// <remarks>
        /// The token user name is case-insensitive.
        /// </remarks>
        public String UserName
        {
            get 
            { 
                return m_userName; 
            }

            set 
            {
                ArgumentUtility.CheckStringForNullOrEmpty(value, "userName", true);
                m_userName = value; 
            }
        }

        /// <summary>
        /// The type of the stored token. Can be any app-specific value,
        /// but is intended to convey the authentication type.
        /// Therefore some examples might be:
        /// "Federated", "OAuth", "Windows", "Basic", "ServiceIdentity", "S2S"
        /// </summary>
        public String Type
        {
            get 
            { 
                return m_type; 
            }
            
            set 
            {
                ArgumentUtility.CheckStringForNullOrEmpty(value, "type", true);
                m_type = value; 
            }
        }

        private String m_kind;
        private String m_resource;
        private String m_userName;
        private String m_type;

        private static Char[] s_invalidKindCharacters = new Char[] { '\\' };
    }
}
