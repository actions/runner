using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class VariablesL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_AppliesMaskHints()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, VariableValue>
                {
                    { "MySecretName", new VariableValue("My secret value", true) },
                    { "MyPublicVariable", "My public value" },
                };
                var variables = new Variables(hc, copy);

                // Assert.
                Assert.Equal(2, variables.AllVariables.Count());
                Assert.Equal("My public value", variables.Get("MyPublicVariable"));
                Assert.Equal("My secret value", variables.Get("MySecretName"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_HandlesNullValue()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, VariableValue>
                {
                    { "variable1",  new VariableValue(null, false) },
                    { "variable2", "some variable 2 value" },
                };

                // Act.
                var variables = new Variables(hc, copy);

                // Assert.
                Assert.Equal(string.Empty, variables.Get("variable1"));
                Assert.Equal("some variable 2 value", variables.Get("variable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_SetsNullAsEmpty()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, VariableValue>
                {
                    { "variable1", new VariableValue(null, false) },
                };

                // Act.
                var variables = new Variables(hc, copy);

                // Assert.
                Assert.Equal(string.Empty, variables.Get("variable1"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_SetsOrdinalIgnoreCaseComparer()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                CultureInfo currentCulture = CultureInfo.CurrentCulture;
                CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
                try
                {
                    CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                    CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                    var copy = new Dictionary<string, VariableValue>
                    {
                        { "i", "foo" },
                        { "I", "foo" },
                    };

                    // Act.
                    var variables = new Variables(hc, copy);

                    // Assert.
                    Assert.Equal(1, variables.AllVariables.Count());
                }
                finally
                {
                    // Cleanup.
                    CultureInfo.CurrentCulture = currentCulture;
                    CultureInfo.CurrentUICulture = currentUICulture;
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_SkipVariableWithEmptyName()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, VariableValue>
                {
                    { "", "" },
                    { "   ", "" },
                    { "MyPublicVariable", "My public value" },
                };

                var variables = new Variables(hc, copy);

                // Assert.
                Assert.Equal(1, variables.AllVariables.Count());
                Assert.Equal("MyPublicVariable", variables.AllVariables.Single().Name);
                Assert.Equal("My public value", variables.AllVariables.Single().Value);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Get_ReturnsNullIfNotFound()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var variables = new Variables(hc, new Dictionary<string, VariableValue>());

                // Act.
                string actual = variables.Get("no such");

                // Assert.
                Assert.Null(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetBoolean_DoesNotThrowWhenNull()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var variables = new Variables(hc, new Dictionary<string, VariableValue>());

                // Act.
                bool? actual = variables.GetBoolean("no such");

                // Assert.
                Assert.Null(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetEnum_DoesNotThrowWhenNull()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var variables = new Variables(hc, new Dictionary<string, VariableValue>());

                // Act.
                System.IO.FileShare? actual = variables.GetEnum<System.IO.FileShare>("no such");

                // Assert.
                Assert.Null(actual);
            }
        }
    }
}
