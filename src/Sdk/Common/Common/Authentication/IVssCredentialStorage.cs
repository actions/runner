using System;

namespace GitHub.Services.Common
{
    public interface IVssCredentialStorage
    {
        IssuedToken RetrieveToken(
            Uri serverUrl,
            VssCredentialsType credentialsType);

        void StoreToken(
            Uri serverUrl,
            IssuedToken token);

        void RemoveToken(
            Uri serverUrl,
            IssuedToken token);

        bool RemoveTokenValue(
            Uri serverUrl,
            IssuedToken token);
    }
}
