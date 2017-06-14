using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Container
{
    public class ContainerInfo
    {
        private List<MountVolume> _mountVolumes;

        public string ContainerImage { get; set; }
        public string ContainerId { get; set; }
        public string ContainerName { get; set; }
        public string CurrentUserName { get; set; }
        public string CurrentUserId { get; set; }
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
    }

    public class MountVolume
    {
        public MountVolume(string volumePath, bool readOnly = false)
        {
            this.VolumePath = volumePath;
            this.ReadOnly = readOnly;
        }

        public string VolumePath { get; set; }
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