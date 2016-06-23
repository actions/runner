using System.Collections.Generic;

using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition
{
    public class CustomArtifactDetails : IArtifactDetails
    {
        public string ArtifactsUrl { get; set; }

        public string ResultSelector { get; set; }

        public string ResultTemplate { get; set; }

        public ServiceEndpoint Endpoint { get; set; }

        public string RelativePath { get; set; }

        public Dictionary<string, string> ArtifactVariables { get; set; }

        public List<AuthorizationHeader> AuthorizationHeaders { get; set; }

        public IDictionary<string, string> ArtifactTypeStreamMapping { get; set; }
    }
}