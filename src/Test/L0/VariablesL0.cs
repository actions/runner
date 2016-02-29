using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Diagnostics;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{            
    public class VariablesL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CanGetFromContext()
        {
            using (TestHostContext hc = new TestHostContext(nameof(StringUtilL0)))
            {
                Assert.NotNull(hc.Variables); 
            }
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CanSetAndGet()
        {
            using (TestHostContext hc = new TestHostContext(nameof(StringUtilL0)))
            {
                hc.Variables.Set("foo", "bar");
                Assert.Equal(hc.Variables.Get("foo"), "bar"); 
            }
        }
    }
}
