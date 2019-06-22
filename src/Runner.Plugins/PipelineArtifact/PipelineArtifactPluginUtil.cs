using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Runner.Plugins.PipelineArtifact
{
    public static class PipelineArtifactPathHelper
    {
        // This collection of invalid characters is based on the characters that are illegal in Windows/NTFS filenames.
        // Also prevent files (pipeline artifact names) from containing "/" or "\" due to the added complexity this introduces for file pattern matching on download.
        private static readonly char[] ForbiddenArtifactNameChars =
                    new char[] {
                        (char) 0, (char) 1, (char) 2, (char) 3, (char) 4, (char) 5, (char) 6,
                        (char) 7, (char) 8, (char) 9, (char) 10, (char) 11, (char) 12, (char) 13,
                        (char) 14, (char) 15, (char) 16, (char) 17, (char) 18, (char) 19, (char) 20,
                        (char) 21, (char) 22, (char) 23, (char) 24, (char) 25, (char) 26, (char) 27,
                        (char) 28, (char) 29, (char) 30, (char) 31,
                        '"', ':', '<', '>', '|', '*', '?', '/', '\\' };
        private static readonly HashSet<Char> ForbiddenArtifactNameCharsSet = new HashSet<Char>(ForbiddenArtifactNameChars);

        public static bool IsValidArtifactName(string artifactName){
            return !artifactName.Any(c => ForbiddenArtifactNameCharsSet.Contains(c));
        }
    }
}