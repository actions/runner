using System;
using System.Collections.Generic;
using System.IO;
using GitHub.Runner.Common.Util;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Container
{
    public class ContainerInfo
    {
        private IDictionary<string, string> _userMountVolumes;
        private List<MountVolume> _mountVolumes;
        private IDictionary<string, string> _userPortMappings;
        private List<PortMapping> _portMappings;
        private IDictionary<string, string> _environmentVariables;

#if OS_WINDOWS
        private Dictionary<string, string> _pathMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#else
        private Dictionary<string, string> _pathMappings = new Dictionary<string, string>();
#endif

        public ContainerInfo()
        {
            
        }

        public ContainerInfo(IHostContext hostContext, Pipelines.ContainerResource container, Boolean isJobContainer = true)
        {
            this.ContainerName = container.Alias;

            string containerImage = container.Properties.Get<string>("image");
            ArgUtil.NotNullOrEmpty(containerImage, nameof(containerImage));

            this.ContainerImage = containerImage;
            this.ContainerDisplayName = $"{container.Alias}_{Pipelines.Validation.NameValidation.Sanitize(containerImage)}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            this.ContainerRegistryEndpoint = container.Endpoint?.Id ?? Guid.Empty;
            this.ContainerCreateOptions = container.Properties.Get<string>("options");
            this.SkipContainerImagePull = container.Properties.Get<bool>("localimage");
            _environmentVariables = container.Environment;
            this.ContainerEntryPoint = container.Properties.Get<string>("entrypoint", defaultValue: "");
            this.ContainerWorkDirectory = container.Properties.Get<string>("workdir", defaultValue: "");
            this.IsJobContainer = isJobContainer;

#if OS_WINDOWS
            this.ContainerWorkDirectory = "C:\\__w";
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Work)] = this.ContainerWorkDirectory;
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Tools)] = "C:\\__t"; // Tool cache folder may come from ENV, so we need a unique folder to avoid collision
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Root)] = "C:\\__a";
            // add -v '\\.\pipe\docker_engine:\\.\pipe\docker_engine' when they are available (17.09)
#else
            this.ContainerWorkDirectory = "/__w";
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Work)] = this.ContainerWorkDirectory;
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Tools)] = "/__t"; // Tool cache folder may come from ENV, so we need a unique folder to avoid collision
            _pathMappings[hostContext.GetDirectory(WellKnownDirectory.Root)] = "/__a";
            if (this.IsJobContainer)
            {
                this.MountVolumes.Add(new MountVolume("/var/run/docker.sock", "/var/run/docker.sock"));
            }
#endif
            if (container.Ports?.Count > 0)
            {
                foreach (var port in container.Ports)
                {
                    UserPortMappings[port] = port;
                }
            }
            if (container.Volumes?.Count > 0)
            {
                foreach (var volume in container.Volumes)
                {
                    UserMountVolumes[volume] = volume;
                }
            }
        }

        public string ContainerId { get; set; }
        public string ContainerDisplayName { get; set; }
        public string ContainerNetwork { get; set; }
        public string ContainerNetworkAlias { get; set; }
        public string ContainerImage { get; set; }
        public string ContainerName { get; set; }
        public string ContainerEntryPointArgs { get; set; }
        public string ContainerEntryPoint { get; set; }
        public string ContainerWorkDirectory { get; set; }
        public string ContainerBringNodePath { get; set; }
        public Guid ContainerRegistryEndpoint { get; private set; }
        public string ContainerCreateOptions { get; private set; }
        public bool SkipContainerImagePull { get; private set; }
#if !OS_WINDOWS
        public string CurrentUserName { get; set; }
        public string CurrentUserId { get; set; }
#endif
        public bool IsJobContainer { get; set; }

        public IDictionary<string, string> ContainerEnvironmentVariables
        {
            get
            {
                if (_environmentVariables == null)
                {
                    _environmentVariables = new Dictionary<string, string>();
                }

                return _environmentVariables;
            }
        }

        public IDictionary<string, string> UserMountVolumes
        {
            get
            {
                if (_userMountVolumes == null)
                {
                    _userMountVolumes = new Dictionary<string, string>();
                }
                return _userMountVolumes;
            }
        }

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

        public IDictionary<string, string> UserPortMappings
        {
            get
            {
                if (_userPortMappings == null)
                {
                    _userPortMappings = new Dictionary<string, string>();
                }

                return _userPortMappings;
            }
        }

        public List<PortMapping> PortMappings
        {
            get
            {
                if (_portMappings == null)
                {
                    _portMappings = new List<PortMapping>();
                }

                return _portMappings;
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

        public void AddPortMappings(List<PortMapping> portMappings)
        {
            foreach (var port in portMappings)
            {
                PortMappings.Add(port);
            }
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

        public MountVolume(string fromString)
        {
            ParseVolumeString(fromString);
        }

        private void ParseVolumeString(string volume)
        {
            var volumeSplit = volume.Split(":");
            if (volumeSplit.Length == 3)
            {
                // source:target:ro
                SourceVolumePath = volumeSplit[0];
                TargetVolumePath = volumeSplit[1];
                ReadOnly = String.Equals(volumeSplit[2], "ro", StringComparison.OrdinalIgnoreCase);
            }
            else if (volumeSplit.Length == 2)
            {
                if (String.Equals(volumeSplit[1], "ro", StringComparison.OrdinalIgnoreCase))
                {
                    // target:ro
                    TargetVolumePath = volumeSplit[0];
                    ReadOnly = true;
                }
                else
                {
                    // source:target
                    SourceVolumePath = volumeSplit[0];
                    TargetVolumePath = volumeSplit[1];
                    ReadOnly = false;
                }
            }
            else
            {
                // target - or, default to passing straight through
                TargetVolumePath = volume;
                ReadOnly = false;
            }
        }

        public string SourceVolumePath { get; set; }
        public string TargetVolumePath { get; set; }
        public bool ReadOnly { get; set; }
    }

    public class PortMapping
    {
        public PortMapping(string hostPort, string containerPort, string protocol)
        {
            this.HostPort = hostPort;
            this.ContainerPort = containerPort;
            this.Protocol = protocol;
        }

        public string HostPort { get; set; }
        public string ContainerPort { get; set; }
        public string Protocol { get; set; }
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
