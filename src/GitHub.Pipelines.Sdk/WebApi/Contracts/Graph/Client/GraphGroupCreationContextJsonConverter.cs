using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    public class GraphGroupCreationContextJsonConverter : VssJsonCreationConverter<GraphGroupCreationContext>
    {
        protected override GraphGroupCreationContext Create(Type objectType, JObject jsonObject)
        {
            // enforce origin id or principalname or displayName
            var hasOriginId = jsonObject["originId"] != null;
            var hasMailAddress = jsonObject["mailAddress"] != null;
            var hasDisplayName = jsonObject["displayName"] != null;
            var requiredFields = new bool[]
            {
                hasOriginId,
                hasDisplayName,
                hasMailAddress
            };

            if (requiredFields.Count(b => b) > 1)
            {
                throw new ArgumentNullException(WebApiResources.GraphGroupMissingRequiredFields());
            }

            if (hasOriginId)
            {
                return  new GraphGroupOriginIdCreationContext();
            }

            if (hasMailAddress)
            {
                return  new GraphGroupMailAddressCreationContext();
            }

            if (hasDisplayName)
            {
                return new GraphGroupVstsCreationContext();
            }

            throw new ArgumentException(WebApiResources.GraphGroupMissingRequiredFields());
        }
    }
}
