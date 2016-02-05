using System;
using System.Collections.Generic;
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
            String message = StringUtil.Format("Test {0}", "Test");
            
            Assert.Equal("Test Test", message); 
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void FormatMessageWithNoArgs()
        {
            String message = StringUtil.Format("Test {0}");
            
            Assert.Equal("Test {0}", message); 
        }
    }
}
