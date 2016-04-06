// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IArtifact.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// <summary>
//   Defines the IArtifact type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Worker;

namespace Agent.Worker.Release.Artifacts.Definition
{
    public interface IArtifact
    {
        Task Download(
            ArtifactDefinition artifactDefinition,
            IHostContext hostContext,
            IExecutionContext executionContext,
            string localFolderPath);
    }
}