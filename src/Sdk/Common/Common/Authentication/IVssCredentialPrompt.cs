using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Provide an interface to get a new token for the credentials.
    /// </summary>
    public interface IVssCredentialPrompt
    {
        /// <summary>
        /// Get a new token using the specified provider and the previously failed token.
        /// </summary>
        /// <param name="provider">The provider for the token to be retrieved</param>
        /// <param name="failedToken">The token which previously failed authentication, if available</param>
        /// <returns>The new token</returns>
        Task<IssuedToken> GetTokenAsync(IssuedTokenProvider provider, IssuedToken failedToken);

        IDictionary<string, string> Parameters { get; set; }
    }

    public interface IVssCredentialPrompts : IVssCredentialPrompt
    {
        IVssCredentialPrompt FederatedPrompt
        {
            get;
        }
    }
}
