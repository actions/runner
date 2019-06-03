using Runner.Common.Worker;
using Runner.Common.Worker.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Runner.Common.Tests
{
    public sealed class ExtensionManagerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void LoadsTypeFromString()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                // Arrange.
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void LoadsTypes()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                // Arrange.
                var manager = new ExtensionManager();
                manager.Initialize(tc);

                // Act/Assert.
                AssertContains<Runner.Common.Capabilities.ICapabilitiesProvider>(
                    manager,
                    concreteType: typeof(Runner.Common.Capabilities.AgentCapabilitiesProvider));
                AssertContains<Runner.Common.Worker.IJobExtension>(
                    manager,
                    concreteType: typeof(Runner.Common.Worker.Build.BuildJobExtension));
            }
        }

        private static void AssertContains<T>(ExtensionManager manager, Type concreteType) where T : class, IExtension
        {
            // Act.
            List<T> extensions = manager.GetExtensions<T>();

            // Assert.
            Assert.True(
                extensions.Any(x => x.GetType() == concreteType),
                $"Expected '{typeof(T).FullName}' extensions to contain concrete type '{concreteType.FullName}'.");
        }
    }
}
