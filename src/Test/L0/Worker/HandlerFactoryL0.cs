using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Moq;
using Xunit;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class HandlerFactoryL0
    {
        private Mock<IExecutionContext> _ec;
        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hostContext = new TestHostContext(this, testName);
            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            _ec.Object.Initialize(hostContext);
            var handler = new Mock<INodeScriptActionHandler>();
            handler.SetupAllProperties();
            hostContext.EnqueueInstance(handler.Object);
            //hostContext.EnqueueInstance(new ActionCommandManager() as IActionCommandManager);

            return hostContext;
        }
    }
}
