using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Security
{
    /// <summary>
    /// Represents a request to rename a token in a security namespace.
    /// </summary>
    [DataContract]
    public sealed class TokenRename
    {
        public TokenRename()
        {
        }

        /// <summary>
        /// Creates a new rename request for a token in a security namespace.
        /// </summary>
        /// <param name="oldToken">The current name of the token</param>
        /// <param name="newToken">The desired new name of the token</param>
        /// <param name="copy">True if the existing token should be preserved; false if it should be deleted</param>
        /// <param name="recurse">True if the scope of the operation should be extended to all child tokens of oldToken; false otherwise</param>
        public TokenRename(
            String oldToken,
            String newToken,
            bool copy,
            bool recurse)
        {
            OldToken = oldToken;
            NewToken = newToken;
            Copy = copy;
            Recurse = recurse;
        }

        /// <summary>
        /// The current name of the token.
        /// </summary>
        [DataMember]
        public String OldToken { get; set; }

        /// <summary>
        /// The desired new name of the token.
        /// </summary>
        [DataMember]
        public String NewToken { get; set; }

        /// <summary>
        /// True if the existing token should be preserved; false if it should be deleted.
        /// </summary>
        [DataMember]
        public bool Copy { get; set; }

        /// <summary>
        /// True if the scope of the operation should be extended to all child tokens of OldToken; false otherwise.
        /// </summary>
        [DataMember]
        public bool Recurse { get; set; }
    }
}
