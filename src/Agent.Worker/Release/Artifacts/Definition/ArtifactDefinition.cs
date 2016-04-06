// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArtifactDefinition.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// <summary>
//   Defines the ArtifactDefinition type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Agent.Worker.Release.Artifacts.Definition
{
    public class ArtifactDefinition
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public ArtifactType ArtifactType { get; set; }

        public IArtifactDetails Details { get; set; }
    }
}
