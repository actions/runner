namespace GitHub.Services.BlobStore.Common
{
    /// <summary>
    /// This class encapsulates the available artifact publish options.
    /// These in the future may be used by both drop.exe and DedupManifestArtifactClient.
    /// </summary>
    public sealed class ArtifactPublishOptions
    {
        // Ignore the .git folder by default.
        public bool HonorIgnoreOptions { get; set; } = true;

        public ArtifactPublishOptions() {}

        public ArtifactPublishOptions(bool honorIgnoreOptions)
        {
            HonorIgnoreOptions = honorIgnoreOptions;
        }     
    }
}
