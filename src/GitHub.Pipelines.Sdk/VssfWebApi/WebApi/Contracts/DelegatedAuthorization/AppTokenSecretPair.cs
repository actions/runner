namespace GitHub.Services.DelegatedAuthorization
{
    public class AppTokenSecretPair
    {
        //*************************************************************************
        /// <summary>
        /// This class is used while exchanging an app session token with oauth2 
        /// user token with API call.
        /// </summary>
        //*************************************************************************
        public string AppToken { get; set; }
        public string ClientSecret { get; set; }
    }
}
