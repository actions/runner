using System.Collections.Generic;
using System.Security.Claims;

namespace GitHub.Services.WebApi.Jwt
{
    public sealed class JsonWebTokenValidationParameters
    {
        public JsonWebTokenValidationParameters()
        {
            ValidateActor = false;
            ValidateAudience = true;
            ValidateIssuer = true;
            ValidateExpiration = true;
            ValidateNotBefore = false;
            ValidateSignature = true;
            ClockSkewInSeconds = 300;
            IdentityNameClaimType = ClaimTypes.NameIdentifier;
        }

        public bool ValidateActor
        {
            get;
            set;
        }

        public bool ValidateAudience
        {
            get;
            set;
        }

        public bool ValidateIssuer
        {
            get;
            set;
        }

        public bool ValidateExpiration
        {
            get;
            set;
        }

        public bool ValidateNotBefore
        {
            get;
            set;
        }

        public bool ValidateSignature
        {
            get;
            set;
        }

        public JsonWebTokenValidationParameters ActorValidationParameters
        {
            get;
            set;
        }

        public IEnumerable<string> AllowedAudiences
        {
            get;
            set;
        }

        public int ClockSkewInSeconds
        {
            get;
            set;
        }

        public VssSigningCredentials SigningCredentials
        {
            get;
            set;
        }

        public IEnumerable<string> ValidIssuers
        {
            get;
            set;
        }

        public string IdentityNameClaimType
        {
            get; 
            set; 
        }
    }
}
