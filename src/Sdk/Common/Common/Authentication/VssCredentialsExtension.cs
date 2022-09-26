using GitHub.Services.OAuth;

namespace GitHub.Services.Common
{
    public static class VssCredentialsExtension
    {
        public static VssOAuthCredential ToOAuthCredentials(
            this VssCredentials credentials)
        {
            if(credentials.Federated.CredentialType == VssCredentialsType.OAuth)
            {
                return credentials.Federated as VssOAuthCredential;
            }
            else
            {
                return null;
            }
        }
    }
}
