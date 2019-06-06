using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Common.TokenStorage
{
    /// <summary>
    /// Represents a token in the system which is used to connect to a resource.
    /// </summary>
    public abstract class VssToken : VssTokenKey
    {
        protected VssToken(
            String kind,
            String resource,
            String userName,
            String type,
            String tokenValue)
            : base(kind, resource, userName, type)
        {
            if (tokenValue == null)
            {
                tokenValue = String.Empty;
            }

            this.TokenValue = tokenValue;
        }

        /// <summary>
        /// The last token value, may be null.
        /// </summary>
        /// <remarks>
        /// This value is updated whenever SetTokenValue or RefreshTokenValue succeeds.
        /// </remarks>
        public String TokenValue { get; protected set; }
        
        /// <summary>
        /// Get the token value (secret) for this token.
        /// </summary>
        public Boolean RefreshTokenValue()
        {
           String tokenValue = RetrieveValue();
           if (tokenValue == null)
            {
                TokenValue = String.Empty;
                return false;
            }

            TokenValue = tokenValue;
            return true;
        }

        /// <summary>
        /// Sets the token value (secret) for this token.
        /// </summary>
        public Boolean SetTokenValue(String token)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(token, "token", true);

            Boolean succeeded = SetValue(token);

            if (succeeded)
            {
                this.TokenValue = token;
            }

            return succeeded;
        }

        /// <summary>
        /// Removes the token value (secret) for this token.
        /// </summary>
        public Boolean RemoveTokenValue()
        {
            Boolean succeeded = RemoveValue();

            if (succeeded)
            {
                this.TokenValue = String.Empty;
            }

            return succeeded;
        }

        public virtual IEnumerable<string> GetPropertyNames()
        {
            return null;
        }

        /// <summary>
        /// Get a property related to the token out of storage
        /// </summary>
        /// <param name="name">Name of the property in storage</param>
        public abstract String GetProperty(String name);

        /// <summary>
        /// Set a property related to the token in storage
        /// </summary>
        public abstract Boolean SetProperty(String name, String value);

        /// <summary>
        /// Retrieve the token (secret) from storage
        /// </summary>
        protected abstract String RetrieveValue();

        /// <summary>
        /// Store the token (secret) in storage
        /// </summary>
        protected abstract Boolean SetValue(String token);

        /// <summary>
        /// Remove the token (secret) from storage
        /// </summary>
        protected abstract Boolean RemoveValue();
    }
}
