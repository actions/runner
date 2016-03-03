using Microsoft.VisualStudio.Services.Agent.Worker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
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
        [Trait("Category", "Common")]
        public void CanSetAndGet()
        {
            // Arrange.
            using (TestHostContext hc = new TestHostContext(nameof(StringUtilL0)))
            {
                var variables = new Variables(hc, new Dictionary<string, string>());

                // Act.
                variables.Set("foo", "bar");
                string actual = variables.Get("foo");

                // Assert.
                Assert.Equal("bar", actual); 
            }
        }
    }
}
