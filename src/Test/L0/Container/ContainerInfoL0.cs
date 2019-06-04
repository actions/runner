using System;
using GitHub.Runner.Worker.Container;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker.Container
{
    public sealed class ContainerInfoL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MountVolumeConstructorParsesStringInput()
        {
            // Arrange
            MountVolume target = new MountVolume("/dst/dir"); // Maps anonymous Docker volume into target dir
            MountVolume source_target = new MountVolume("/src/dir:/dst/dir"); // Maps source to target dir
            MountVolume target_ro = new MountVolume("/dst/dir:ro");
            MountVolume source_target_ro = new MountVolume("/src/dir:/dst/dir:ro");

            // Assert
            Assert.Null(target.SourceVolumePath);
            Assert.Equal("/dst/dir", target.TargetVolumePath);
            Assert.False(target.ReadOnly);

            Assert.Equal("/src/dir", source_target.SourceVolumePath);
            Assert.Equal("/dst/dir", source_target.TargetVolumePath);
            Assert.False(source_target.ReadOnly);

            Assert.Null(target_ro.SourceVolumePath);
            Assert.Equal("/dst/dir", target_ro.TargetVolumePath);
            Assert.True(target_ro.ReadOnly);

            Assert.Equal("/src/dir", source_target_ro.SourceVolumePath);
            Assert.Equal("/dst/dir", source_target_ro.TargetVolumePath);
            Assert.True(source_target_ro.ReadOnly);
        }
    }
}
