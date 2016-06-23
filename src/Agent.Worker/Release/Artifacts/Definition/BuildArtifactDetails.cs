// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildArtifactDetails.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// <summary>
//   Defines the BuildArtifactDetails type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition
{
    public class BuildArtifactDetails : IArtifactDetails
    {
        public string RelativePath { get; set; }

        public Uri TfsUrl { get; set; }

        public VssCredentials Credentials { get; set; }

        public string Project { get; set; }
    }
}