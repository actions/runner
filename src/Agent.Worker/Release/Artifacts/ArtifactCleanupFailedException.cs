// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArtifactCleanupFailedException.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// <summary>
//   Defines the ArtifactCleanupFailedException type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    public class ArtifactCleanupFailedException : Exception
    {
        public ArtifactCleanupFailedException()
        {
        }

        public ArtifactCleanupFailedException(string message) : base(message)
        {
        }

        public ArtifactCleanupFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
