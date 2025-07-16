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
            string result = NodeCompatibilityChecker.CheckNodeVersionForArm32(_ec.Object, preferredVersion);

            // Verify we called Warning if on ARM32
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm &&
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _ec.Verify(x => x.Warning(It.IsAny<string>()), Times.Once);
                Assert.Equal("node20", result);
            }
            else
            {
                // On non-ARM32 platforms, should pass through the version unmodified
                _ec.Verify(x => x.Warning(It.IsAny<string>()), Times.Never);
                Assert.Equal("node24", result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_PassThroughNonNode24Versions()
        {
            string preferredVersion = "node20";
            string result = NodeCompatibilityChecker.CheckNodeVersionForArm32(_ec.Object, preferredVersion);

            // Should never warn or modify the version for non-node24 inputs
            _ec.Verify(x => x.Warning(It.IsAny<string>()), Times.Never);
            Assert.Equal("node20", result);
        }
    }
}
