using GitHub.Runner.Common.Tests;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Handlers;
using Moq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
            // Test the NodeCompatibilityChecker directly
            string preferredVersion = "node24";
            string result = NodeCompatibilityChecker.CheckNodeVersionForLinusArm32(_ec.Object, preferredVersion);

            // On ARM32 Linux, we should fall back to node20
            bool isArm32 = RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                          Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.Contains("ARM") == true;
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isArm32 && isLinux)
            {
                // Should downgrade to node20 on ARM32 Linux
                Assert.Equal("node20", result);
            }
            else
            {
                // On non-ARM32 platforms, should pass through the version unmodified
                Assert.Equal("node24", result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_PassThroughNonNode24Versions()
        {
            string preferredVersion = "node20";
            string result = NodeCompatibilityChecker.CheckNodeVersionForLinusArm32(_ec.Object, preferredVersion);

            // Should never modify the version for non-node24 inputs
            Assert.Equal("node20", result);
        }
    }
}
