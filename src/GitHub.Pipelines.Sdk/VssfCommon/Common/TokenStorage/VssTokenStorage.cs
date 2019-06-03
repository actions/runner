using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Common.TokenStorage
{
    /// <summary>
    /// Defines how to store a token in storage.
    /// </summary>
    public abstract class VssTokenStorage
    {
        /// <summary>
        /// Add a new token to the storage.
        /// </summary>
        /// <param name="tokenKey">Required.</param>
        /// <param name="tokenValue">Required.</param>
        public VssToken Add(VssTokenKey tokenKey, String tokenValue)
        {
            ArgumentUtility.CheckForNull(tokenKey, "tokenKey");
            ArgumentUtility.CheckStringForNullOrEmpty(tokenValue, "tokenValue", true);

            return AddToken(tokenKey, tokenValue);
        }

        /// <summary>
        /// Retrieve the specified token.
        /// </summary>
        /// <param name="tokenKey">Required.</param>
        public VssToken Retrieve(VssTokenKey tokenKey)
        {
            ArgumentUtility.CheckForNull(tokenKey, "tokenKey");

            return RetrieveToken(tokenKey);
        }

        /// <summary>
        /// Remove a token from storage.
        /// </summary>
        /// <param name="tokenKey">Required.</param>
        public Boolean Remove(VssTokenKey tokenKey)
        {
            ArgumentUtility.CheckForNull(tokenKey, "tokenKey");

            return RemoveToken(tokenKey);
        }

        public virtual IEnumerable<String> GetPropertyNames(VssToken token)
        {
            return null;
        }

        /// <summary>
        /// Retrieve all tokens by kind.
        /// </summary>
        public abstract IEnumerable<VssToken> RetrieveAll(String kind);

        public abstract String GetProperty(VssToken token, String name);
        public abstract Boolean SetProperty(VssToken token, String name, String value);
        public abstract Boolean SetTokenSecret(VssToken token, String tokenValue);
        public abstract String RetrieveTokenSecret(VssToken token);
        public abstract Boolean RemoveTokenSecret(VssToken token);
        public abstract Boolean RemoveAll();

        protected abstract VssToken AddToken(VssTokenKey tokenKey, String tokenValue);
        protected abstract VssToken RetrieveToken(VssTokenKey tokenKey);
        protected abstract Boolean RemoveToken(VssTokenKey tokenKey);
    }
}
