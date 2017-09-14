// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArtifactDirectoryCreationFailedException.cs" company="Microsoft Corporation">
//   2012-2023, All rights reserved.
// </copyright>
// <summary>
//   Defines the ArtifactDirectoryCreationFailedException type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    public class ArtifactDirectoryCreationFailedException : Exception
    {
        public ArtifactDirectoryCreationFailedException()
        {
        }

        public ArtifactDirectoryCreationFailedException(string message) : base(message)
        {
        }

        public ArtifactDirectoryCreationFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
