// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArtifactType.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// <summary>
//   Defines the ArtifactType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Agent.Worker.Release.Artifacts.Definition
{
    public enum ArtifactType
    {
        XamlBuild,
        Build,
        Jenkins,
        FileShare,
        Nuget,
        TfsOnPrem,
        GitHub,
        TFGit,
        ExternalTfsBuild,
        Custom,
        Tfvc
    }
}