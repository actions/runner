// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IArtifactDetails.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// <summary>
//   Defines the IArtifactDetails type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition
{
    public interface IArtifactDetails
    {
        // TODO: We may not need this, server may return / always, check and remove it
        string RelativePath { get; set; }
    }
}