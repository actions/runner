using System;

namespace Microsoft.VisualStudio.Services.Compliance
{
    public class ExternalUserResolution
    {
        /// <summary>
        /// Gets the redirect Uri object for resolving the external user.
        /// </summary>
        public Uri RedirectUri { get; private set; }

        /// <summary>
        /// Gets the error message while trying resolve the external user.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Is true if an error occurred while resolving the external user.
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // private constructor
        public ExternalUserResolution(Uri redirectUri, string errorMessage)
        {
            RedirectUri = redirectUri;
            ErrorMessage = errorMessage;
        }
    }
}
