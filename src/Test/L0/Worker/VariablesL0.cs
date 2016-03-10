using Microsoft.VisualStudio.Services.Agent.Worker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{            
    public class VariablesL0
    {
        // TODO: (eric) I'll fix in follow-up PR which focuses on tests.
/*        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CanGetFromExecutionContext()
        {
            // Arrange.
            using (TestHostContext hc = new TestHostContext(nameof(StringUtilL0)))
            {
                // Act.
                ExecutionContext ec = new ExecutionContext();
                ec.Initialize(hc);

                // Assert.
                Assert.NotNull(ec.Variables); 
            }
        }*/

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CanSetAndGet()
        {
            // Arrange.
            using (TestHostContext hc = new TestHostContext(nameof(VariablesL0)))
            {
                var variables = new Variables(hc, new Dictionary<string, string>());

                // Act.
                variables.Set("foo", "bar");
                string actual = variables.Get("foo");

                // Assert.
                Assert.Equal("bar", actual); 
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetBooleanDoesNotThrowWhenNull()
        {
            // Arrange.
            using (TestHostContext hc = new TestHostContext(nameof(VariablesL0)))
            {
                var variables = new Variables(hc, new Dictionary<string, string>());

                // Act.
                bool? actual = variables.GetBoolean("no such");

                // Assert.
                Assert.Null(actual); 
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetEnumDoesNotThrowWhenNull()
        {
            // Arrange.
            using (TestHostContext hc = new TestHostContext(nameof(VariablesL0)))
            {
                var variables = new Variables(hc, new Dictionary<string, string>());

                // Act.
                System.IO.FileShare? actual = variables.GetEnum<System.IO.FileShare>("no such");

                // Assert.
                Assert.Null(actual); 
            }
        }
    }
}
