using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using GitHub.Services.WebApi;

namespace GitHub.Services.Graph.Client
{
    public class GraphUserUpdateContextJsonConverter : VssJsonCreationConverter<GraphUserUpdateContext>
    {
        protected override GraphUserUpdateContext Create(Type objectType, JObject jsonObject)
        {
            // enforce origin id or principalname or displayName
            var hasOriginId = jsonObject["originId"] != null;

            var requiredFields = new bool[]
            {
                hasOriginId
            };

            if (requiredFields.Count(b => b) != 1)
            {
                throw new ArgumentException(WebApiResources.GraphUserMissingRequiredFields());
            }

            if (hasOriginId)
            {
                return new GraphUserOriginIdUpdateContext();
            }

            throw new ArgumentException(WebApiResources.GraphUserMissingRequiredFields());
        }
    }
}
