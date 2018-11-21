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

#if OS_WINDOWS
        private Dictionary<string, string> _pathMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#else
        private Dictionary<string, string> _pathMappings = new Dictionary<string, string>();
#endif

        public ContainerInfo(IHostContext hostContext, Pipelines.ContainerResource container)
        {
            this.ContainerName = container.Alias;

            string containerImage = container.Properties.Get<string>("image");
            ArgUtil.NotNullOrEmpty(containerImage, nameof(containerImage));

            this.ContainerImage = containerImage;
            this.ContainerDisplayName = $"{container.Alias}_{Pipelines.Validation.NameValidation.Sanitize(containerImage)}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            this.ContainerRegistryEndpoint = container.Endpoint?.Id ?? Guid.Empty;
            this.ContainerCreateOptions = container.Properties.Get<string>("options");
            this.SkipContainerImagePull = container.Properties.Get<bool>("localimage");
            this.ContainerEnvironmentVariables = container.Environment;

#if OS_WINDOWS            
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Tools)] = "C:\\__t"; // Tool cache folder may come from ENV, so we need a unique folder to avoid collision
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Work)] = "C:\\__w";
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Root)] = "C:\\__a";
#else
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Tools)] = "/__t"; // Tool cache folder may come from ENV, so we need a unique folder to avoid collision
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Work)] = "/__w";
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Root)] = "/__a";
#endif            
        }

        public string ContainerId { get; set; }
        public string ContainerDisplayName { get; private set; }
        public string ContainerNetwork { get; set; }
        public string ContainerImage { get; set; }
        public string ContainerName { get; set; }
        public string ContainerBringNodePath { get; set; }
        public Guid ContainerRegistryEndpoint { get; private set; }
        public string ContainerCreateOptions { get; private set; }
        public bool SkipContainerImagePull { get; private set; }
        public IDictionary<string, string> ContainerEnvironmentVariables { get; private set; }
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

        public string TranslateToContainerPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var mapping in _pathMappings)
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
                foreach (var mapping in _pathMappings)
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