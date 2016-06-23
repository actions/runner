// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JenkinsArtifactDetails.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition
{
    public class JenkinsArtifactDetails : IArtifactDetails
    {
        public string RelativePath { get; set; }

        public Uri Url { get; set; }

        public string AccountName { get; set; }

        public string AccountPassword { get; set; }

        public string JobName { get; set; }

        public int BuildId { get; set; }
    }
}