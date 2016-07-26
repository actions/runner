using System;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    public class ContainerItem
    {
        public ItemType ItemType { get; set; }
        public string Path { get; set; }
        public long FileLength { get; set; }

        // TODO(omeshp): Figure a way to remove these dependencies with server drop artifact
        public long ContainerId { get; set; }
        public Guid ScopeIdentifier { get; set; }
    }
}