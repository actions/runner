using System;
using System.Collections.Generic;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Represents an object which participates in setting parameters for an OAuth token request.
    /// </summary>
    public interface IVssOAuthTokenParameterProvider
    {
        /// <summary>
        /// Sets applicable parameters on the provided parameters collection for a token request in which the provider
        /// is a participant.
        /// </summary>
        /// <param name="parameters">The current set of parameters</param>
        void SetParameters(IDictionary<String, String> parameters);
    }
}
