using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using System;
using Xunit;

namespace GitHub.Runner.Common.Tests.Util
{
    public sealed class ArgUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Equal_MatchesObjectEquality()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange.
                string expected = "Some string".ToLower();  // ToLower is required to avoid reference equality
                string actual = "Some string".ToLower();    // due to compile-time string interning.

                // Act/Assert.
                ArgUtil.Equal(expected: expected, actual: actual, name: "Some parameter");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Equal_MatchesReferenceEquality()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange.
                object expected = new object();
                object actual = expected;

                // Act/Assert.
                ArgUtil.Equal(expected: expected, actual: actual, name: "Some parameter");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Equal_MatchesStructEquality()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange.
                int expected = 123;
                int actual = expected;

                // Act/Assert.
                ArgUtil.Equal(expected: expected, actual: actual, name: "Some parameter");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Equal_ThrowsWhenActualObjectIsNull()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange.
                object expected = new object();
                object actual = null;

                // Act/Assert.
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    ArgUtil.Equal(expected: expected, actual: actual, name: "Some parameter");
                });
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Equal_ThrowsWhenExpectedObjectIsNull()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange.
                object expected = null;
                object actual = new object();

                // Act/Assert.
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    ArgUtil.Equal(expected: expected, actual: actual, name: "Some parameter");
                });
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Equal_ThrowsWhenObjectsAreNotEqual()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange.
                object expected = new object();
                object actual = new object();

                // Act/Assert.
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    ArgUtil.Equal(expected: expected, actual: actual, name: "Some parameter");
                });
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Equal_ThrowsWhenStructsAreNotEqual()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange.
                int expected = 123;
                int actual = 456;

                // Act/Assert.
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    ArgUtil.Equal(expected: expected, actual: actual, name: "Some parameter");
                });
            }
        }
    }
}
