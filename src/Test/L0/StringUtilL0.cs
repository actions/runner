using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{            
    public class StringUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void FormatMessage()
        {
            using (TestHostContext thc = new TestHostContext(nameof(StringUtilL0)))
            {
                TraceSource trace = thc.GetTrace();

                String message = StringUtil.Format("Test {0}", "Test");
                trace.Info(message);

                Assert.Equal("Test Test", message); 
            }
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void FormatMessageWithNoArgs()
        {
            using (TestHostContext thc = new TestHostContext(nameof(StringUtilL0)))
            {
                TraceSource trace = thc.GetTrace();

                String message = StringUtil.Format("Test {0}");
                trace.Info(message);

                Assert.Equal("Test {0}", message); 
            }
        }
    }
}
