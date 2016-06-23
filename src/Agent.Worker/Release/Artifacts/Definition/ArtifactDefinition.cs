// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArtifactDefinition.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// <summary>
//   Defines the ArtifactDefinition type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition
{
    public class ArtifactDefinition
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public AgentArtifactType ArtifactType { get; set; }

        public IArtifactDetails Details { get; set; }
    }
}
