using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using GitHub.Services.WebApi;

namespace GitHub.Services.Graph.Client
{
    public class GraphUserCreationContextJsonConverter : VssJsonCreationConverter<GraphUserCreationContext>
    {
        protected override GraphUserCreationContext Create(Type objectType, JObject jsonObject)
        {
            // enforce origin id or principalname or displayName
            var hasOriginId = jsonObject["originId"] != null;
            var hasPrincipalName = jsonObject["principalName"] != null;
            var hasMailAddress = jsonObject["mailAddress"] != null;
            var requiredFields = new bool[]
            {
                hasOriginId,
                hasPrincipalName,
                hasMailAddress,
            };

            if (requiredFields.Count(b => b) != 1)
            {
                throw new ArgumentException(WebApiResources.GraphUserMissingRequiredFields());
            }

            if (hasOriginId)
            {
                return new GraphUserOriginIdCreationContext();
            }

            if (hasPrincipalName)
            {
                return new GraphUserPrincipalNameCreationContext();
            }

            if (hasMailAddress)
            {
                return new GraphUserMailAddressCreationContext();
            }

            throw new ArgumentException(WebApiResources.GraphUserMissingRequiredFields());
        }
    }
}
