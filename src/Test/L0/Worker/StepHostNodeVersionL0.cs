using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Moq;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class StepHostNodeVersionL0
    {
        private Mock<IExecutionContext> _ec;
        private DefaultStepHost _defaultStepHost;

        public StepHostNodeVersionL0()
        {
            _ec = new Mock<IExecutionContext>();
            _defaultStepHost = new DefaultStepHost();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_Node24OnArm32Linux()
        {
            // Test via NodeUtil directly
            string preferredVersion = "node24";
            var (nodeVersion, warningMessage) = Common.Util.NodeUtil.CheckNodeVersionForLinuxArm32(preferredVersion);

            // On ARM32 Linux, we should fall back to node20
            bool isArm32 = RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                          Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.Contains("ARM") == true;
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isArm32 && isLinux)
            {
                // Should downgrade to node20 on ARM32 Linux
                Assert.Equal("node20", nodeVersion);
                Assert.NotNull(warningMessage);
                Assert.Contains("Node 24 is not supported on Linux ARM32 platforms", warningMessage);
            }
            else
            {
                // On non-ARM32 platforms, should pass through the version unmodified
                Assert.Equal("node24", nodeVersion);
                Assert.Null(warningMessage);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_PassThroughNonNode24Versions()
        {
            string preferredVersion = "node20";
            var (nodeVersion, warningMessage) = Common.Util.NodeUtil.CheckNodeVersionForLinuxArm32(preferredVersion);

            // Should never modify the version for non-node24 inputs
            Assert.Equal("node20", nodeVersion);
            Assert.Null(warningMessage);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_DeprecationFlagShowsWarning()
        {
            string preferredVersion = "node24";
            var (nodeVersion, warningMessage) = Common.Util.NodeUtil.CheckNodeVersionForLinuxArm32(preferredVersion, deprecateArm32: true);

            bool isArm32 = RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                          Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.Contains("ARM") == true;
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isArm32 && isLinux)
            {
                Assert.Equal("node20", nodeVersion);
                Assert.NotNull(warningMessage);
                Assert.Contains("deprecated", warningMessage);
                Assert.Contains("no longer be supported", warningMessage);
            }
            else
            {
                Assert.Equal("node24", nodeVersion);
                Assert.Null(warningMessage);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_DeprecationFlagWithNode20PassesThrough()
        {
            // Even with deprecation flag, node20 should pass through (not downgraded further)
            string preferredVersion = "node20";
            var (nodeVersion, warningMessage) = Common.Util.NodeUtil.CheckNodeVersionForLinuxArm32(preferredVersion, deprecateArm32: true);

            bool isArm32 = RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                          Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.Contains("ARM") == true;
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isArm32 && isLinux)
            {
                Assert.Equal("node20", nodeVersion);
                Assert.NotNull(warningMessage);
                Assert.Contains("deprecated", warningMessage);
            }
            else
            {
                Assert.Equal("node20", nodeVersion);
                Assert.Null(warningMessage);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_KillFlagReturnsNull()
        {
            string preferredVersion = "node24";
            var (nodeVersion, warningMessage) = Common.Util.NodeUtil.CheckNodeVersionForLinuxArm32(preferredVersion, killArm32: true);

            bool isArm32 = RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                          Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.Contains("ARM") == true;
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isArm32 && isLinux)
            {
                Assert.Null(nodeVersion);
                Assert.NotNull(warningMessage);
                Assert.Contains("no longer supported", warningMessage);
            }
            else
            {
                Assert.Equal("node24", nodeVersion);
                Assert.Null(warningMessage);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_KillTakesPrecedenceOverDeprecation()
        {
            string preferredVersion = "node20";
            var (nodeVersion, warningMessage) = Common.Util.NodeUtil.CheckNodeVersionForLinuxArm32(preferredVersion, deprecateArm32: true, killArm32: true);

            bool isArm32 = RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                          Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.Contains("ARM") == true;
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isArm32 && isLinux)
            {
                Assert.Null(nodeVersion);
                Assert.NotNull(warningMessage);
                Assert.Contains("no longer supported", warningMessage);
            }
            else
            {
                Assert.Equal("node20", nodeVersion);
                Assert.Null(warningMessage);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_ServerOverridableDateUsedInDeprecationWarning()
        {
            string preferredVersion = "node24";
            string customDate = "December 1st, 2027";
            var (nodeVersion, warningMessage) = Common.Util.NodeUtil.CheckNodeVersionForLinuxArm32(
                preferredVersion, deprecateArm32: true, node20RemovalDate: customDate);

            bool isArm32 = RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                          Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.Contains("ARM") == true;
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isArm32 && isLinux)
            {
                Assert.Equal("node20", nodeVersion);
                Assert.NotNull(warningMessage);
                Assert.Contains(customDate, warningMessage);
                Assert.DoesNotContain(Constants.Runner.NodeMigration.Node20RemovalDate, warningMessage);
            }
            else
            {
                Assert.Equal("node24", nodeVersion);
                Assert.Null(warningMessage);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_FallbackDateUsedWhenNoOverride()
        {
            string preferredVersion = "node24";
            var (nodeVersion, warningMessage) = Common.Util.NodeUtil.CheckNodeVersionForLinuxArm32(
                preferredVersion, deprecateArm32: true);

            bool isArm32 = RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                          Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.Contains("ARM") == true;
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isArm32 && isLinux)
            {
                Assert.Equal("node20", nodeVersion);
                Assert.NotNull(warningMessage);
                Assert.Contains(Constants.Runner.NodeMigration.Node20RemovalDate, warningMessage);
            }
            else
            {
                Assert.Equal("node24", nodeVersion);
                Assert.Null(warningMessage);
            }
        }
    }
}
