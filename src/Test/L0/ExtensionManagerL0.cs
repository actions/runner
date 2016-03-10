using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class ExtensionManagerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void LoadsTypeFromString()
        {
            // Arrange.
            using (TestHostContext tc = new TestHostContext(this))
            {
                var manager = new ExtensionManager();
                manager.Initialize(tc);

                // Act.
                List<IJobExtension> extensions = manager.GetExtensions<IJobExtension>();

                // Assert.
                Assert.True(
                    extensions.Any(x => x is BuildJobExtension),
                    $"Expected {nameof(BuildJobExtension)} extension to be returned as a job extension.");
            }
        }
    }
}
