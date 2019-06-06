using System;
using System.Runtime.Serialization;

namespace GitHub.Services.OAuth
{
    [DataContract]
    public sealed class AuthorizationResponse
    {
        public AuthorizationResponse()
        {
        }

        public AuthorizationResponse(
            String redirectLocation)
        {
            RedirectLocation = redirectLocation;
        }

        [DataMember(Name = "redirect_location")]
        public String RedirectLocation { get; set; }
    }
}
