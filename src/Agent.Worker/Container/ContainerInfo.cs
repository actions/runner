using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Services.Agent.Util;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Container
{
    public class ContainerInfo
    {
        private List<MountVolume> _mountVolumes;
        private Dictionary<string, string> _pathMappings;

        public ContainerInfo(Pipelines.ContainerReference container)
        {
            this.ContainerName = container.Name;

            container.Data.TryGetValue("image", out string containerImage);
            ArgUtil.NotNullOrEmpty(containerImage, nameof(containerImage));
            this.ContainerImage = containerImage;

            this.ContainerDisplayName = $"{container.Name}_{Pipelines.Validation.NameValidation.Sanitize(containerImage)}";

            if (container.Data.TryGetValue("registry", out string containerRegistry))
            {
                this.ContainerRegistryEndpoint = containerRegistry;
            }

            if (container.Data.TryGetValue("options", out string containerCreateOptions))
            {
                this.ContainerCreateOptions = containerCreateOptions;
            }

            if (container.Data.TryGetValue("localimage", out string localImage))
            {
                this.SkipContainerImagePull = StringUtil.ConvertToBoolean(localImage);
            }
        }

        public string ContainerId { get; set; }
        public string ContainerDisplayName { get; set; }
        public string ContainerNetwork { get; set; }
        public string ContainerImage { get; set; }
        public string ContainerName { get; set; }
        public string ContainerRegistryEndpoint { get; set; }
        public string ContainerCreateOptions { get; set; }
        public bool SkipContainerImagePull { get; set; }
#if !OS_WINDOWS
        public string CurrentUserName { get; set; }
        public string CurrentUserId { get; set; }
#endif
        public List<MountVolume> MountVolumes
        {
            get
            {
                if (_mountVolumes == null)
                {
                    _mountVolumes = new List<MountVolume>();
                }

                return _mountVolumes;
            }
        }

        public Dictionary<string, string> PathMappings
        {
            get
            {
                if (_pathMappings == null)
                {
#if OS_WINDOWS
                    _pathMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#else
                    _pathMappings = new Dictionary<string, string>();
#endif
                }

                return _pathMappings;
            }
        }

        public string TranslateToContainerPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var mapping in PathMappings)
                {
#if OS_WINDOWS
                    if (string.Equals(path, mapping.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.Value;
                    }

                    if (path.StartsWith(mapping.Key + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith(mapping.Key + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.Value + path.Remove(0, mapping.Key.Length);
                    }
#else
                    if (string.Equals(path, mapping.Key))
                    {
                        return mapping.Value;
                    }

                    if (path.StartsWith(mapping.Key + Path.DirectorySeparatorChar))
                    {
                        return mapping.Value + path.Remove(0, mapping.Key.Length);
                    }
#endif
                }
            }

            return path;
        }

        public string TranslateToHostPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var mapping in PathMappings)
                {
#if OS_WINDOWS
                    if (string.Equals(path, mapping.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.Key;
                    }

                    if (path.StartsWith(mapping.Value + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith(mapping.Value + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.Key + path.Remove(0, mapping.Value.Length);
                    }
#else
                    if (string.Equals(path, mapping.Value))
                    {
                        return mapping.Key;
                    }

                    if (path.StartsWith(mapping.Value + Path.DirectorySeparatorChar))
                    {
                        return mapping.Key + path.Remove(0, mapping.Value.Length);
                    }
#endif
                }
            }

            return path;
        }
    }

    public class MountVolume
    {
        public MountVolume(string sourceVolumePath, string targetVolumePath, bool readOnly = false)
        {
            this.SourceVolumePath = sourceVolumePath;
            this.TargetVolumePath = targetVolumePath;
            this.ReadOnly = readOnly;
        }

        public string SourceVolumePath { get; set; }
        public string TargetVolumePath { get; set; }
        public bool ReadOnly { get; set; }
    }

    public class DockerVersion
    {
        public DockerVersion(Version serverVersion, Version clientVersion)
        {
            this.ServerVersion = serverVersion;
            this.ClientVersion = clientVersion;
        }

        public Version ServerVersion { get; set; }
        public Version ClientVersion { get; set; }
    }
}