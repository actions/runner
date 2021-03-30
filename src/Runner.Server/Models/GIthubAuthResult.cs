using System;
using System.Runtime.Serialization;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.OAuth;

namespace Runner.Server.Models
{
        
    [DataContract]
    public sealed class GitHubAuthResult
    {
        [DataMember(Name = "url")]
        public string TenantUrl { get; set; }

        [DataMember(Name = "token_schema")]
        public string TokenSchema { get; set; }

        [DataMember(Name = "token")]
        public string Token { get; set; }

        public VssCredentials ToVssCredentials()
        {
            ArgUtil.NotNullOrEmpty(TokenSchema, nameof(TokenSchema));
            ArgUtil.NotNullOrEmpty(Token, nameof(Token));

            if (string.Equals(TokenSchema, "OAuthAccessToken", StringComparison.OrdinalIgnoreCase))
            {
                return new VssCredentials(new VssOAuthAccessTokenCredential(Token), CredentialPromptType.DoNotPrompt);
            }
            else
            {
                throw new NotSupportedException($"Not supported token schema: {TokenSchema}");
            }
        }
    }
}