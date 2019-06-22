using System;
using System.Collections.Generic;
using GitHub.Services.BlobStore.Common;

namespace GitHub.Services.BlobStore.WebApi
{
    public class DownloadDedupManifestArtifactOptions
    {
        //A DownloadDedupManifestArtifactOptions should either have a ManifestId or a ArtifactNameAndManifestIds, if none exists, then throw an error.
        public DedupIdentifier ManifestId { get; private protected set; }
        public IDictionary<string, DedupIdentifier> ArtifactNameAndManifestIds { get; private protected set; }
        public string ArtifactName { get; private protected set; }
        public string AbsoluteManifestPath { get; private protected set; }
        public string TargetDirectory { get; private protected set; }
        public IEnumerable<string> MinimatchPatterns { get; private protected set; }
        public Uri ProxyUri { get; private protected set; }
        public bool MinimatchFilterWithArtifactName { get; private protected set; }

        // Disable external access to ctor
        private protected DownloadDedupManifestArtifactOptions() { }

        /// <summary>
        /// Download options for single download and for DownloadSingleManifestAsync function
        /// </summary>
        /// <param name="manifestId">Manifest Id</param>
        /// <param name="targetDirectory">Path on the agent machine where the artifacts will be downloaded</param>
        /// <param name="proxyUri">Proxy Uri</param>
        /// <param name="minimatchPatterns">Minimatch patterns</param>
        /// <param name="artifactName">Artifact name - by setting the artifact name, it will create a root directory that is the artifact name itself.</param>
        /// <param name="minimatchFilterWithArtifactName">Specify if the new minimatch pattern should apply - the new minimatch pattern will filter including the artifact name when artifact exists</param>
        public static DownloadDedupManifestArtifactOptions CreateWithManifestId(
            DedupIdentifier manifestId,
            string targetDirectory,
            Uri proxyUri = null,
            IEnumerable<string> minimatchPatterns = null,
            string artifactName = "",
            bool minimatchFilterWithArtifactName = true)
        {
            return new DownloadDedupManifestArtifactOptions
            {
                ManifestId = manifestId,
                TargetDirectory = targetDirectory,
                ProxyUri = proxyUri,
                MinimatchPatterns = minimatchPatterns,
                ArtifactName = artifactName,
                MinimatchFilterWithArtifactName = minimatchFilterWithArtifactName
            };
        }

        public static DownloadDedupManifestArtifactOptions CreateWithMultiManifestIds(
            IDictionary<string, DedupIdentifier> artifactNameAndManifestIds,
            string targetDirectory,
            Uri proxyUri = null,
            IEnumerable<string> minimatchPatterns = null,
            bool minimatchFilterWithArtifactName = true)
        {
            return new DownloadDedupManifestArtifactOptions
            {
                ArtifactNameAndManifestIds = artifactNameAndManifestIds,
                TargetDirectory = targetDirectory,
                ProxyUri = proxyUri,
                MinimatchPatterns = minimatchPatterns,
                MinimatchFilterWithArtifactName = minimatchFilterWithArtifactName
            };
        }

        public static DownloadDedupManifestArtifactOptions CreateWithManifestPath(
            string absoluteManifestPath,
            string targetDirectory,
            Uri proxyUri = null,
            IEnumerable<string> minimatchPatterns = null)
        {
            return new DownloadDedupManifestArtifactOptions
            {
                AbsoluteManifestPath = absoluteManifestPath,
                TargetDirectory = targetDirectory,
                MinimatchPatterns = minimatchPatterns,
                ProxyUri = proxyUri
            };
        }

        public void SetAbsoluteManifestPathAndRemoveManifestId(string absoluteManifestPath)
        {
            AbsoluteManifestPath = absoluteManifestPath;
            ManifestId = null;
        }
    }
}
