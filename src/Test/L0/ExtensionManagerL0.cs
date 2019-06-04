﻿using GitHub.Runner.Common.Capabilities;
using GitHub.Runner.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GitHub.Runner.Common.Tests
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
                List<ICapabilitiesProvider> extensions = manager.GetExtensions<ICapabilitiesProvider>();

                // Assert.
                Assert.True(
                    extensions.Any(x => x is RunnerCapabilitiesProvider),
                    $"Expected {nameof(RunnerCapabilitiesProvider)} extension to be returned as a job extension.");
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
                AssertContains<GitHub.Runner.Common.Capabilities.ICapabilitiesProvider>(
                    manager,
                    concreteType: typeof(GitHub.Runner.Common.Capabilities.RunnerCapabilitiesProvider));
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
