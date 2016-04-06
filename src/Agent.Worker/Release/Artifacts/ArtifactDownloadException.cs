// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArtifactDownloadException.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// <summary>
//   Defines the ArtifactDownloadException type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    public class ArtifactDownloadException : Exception
    {
        public ArtifactDownloadException()
        {
        }

        public ArtifactDownloadException(string message) : base(message)
        {
        }

        public ArtifactDownloadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}