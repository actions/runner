using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker.Handlers
{
    public sealed class NodeHandlerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void NodeJSActionExecutionDataSupportsNode24()
        {
            // Create NodeJSActionExecutionData with node24
            var nodeJSData = new NodeJSActionExecutionData
            {
                NodeVersion = "node24",
                Script = "test.js"
            };
            
            // Act & Assert
            Assert.Equal("node24", nodeJSData.NodeVersion);
            Assert.Equal(ActionExecutionType.NodeJS, nodeJSData.ExecutionType);
        }
    }
}
