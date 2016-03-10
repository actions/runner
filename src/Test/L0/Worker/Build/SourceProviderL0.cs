using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Build
{
    public sealed class SourceProviderL0
    {
        private sealed class ConcreteSourceProvider : SourceProvider
        {
            public override string RepositoryType => "Some repository type";
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ReturnsCorrectHashKey()
        {
            // Arrange.
            using (TestHostContext tc = new TestHostContext(this))
            {
                var executionContext = new Mock<IExecutionContext>();
                executionContext
                    .Setup(x => x.Variables)
                    .Returns(new Variables(tc, copy: new Dictionary<string, string>()));
                executionContext.Object.Variables.Set(Constants.Variables.System.CollectionId, "7aee6dde-6381-4098-93e7-50a8264cf066");
                executionContext.Object.Variables.Set(Constants.Variables.System.DefinitionId, "7");
                var endpoint = new ServiceEndpoint
                {
                    Url = new Uri("http://contoso:8080/tfs/DefaultCollection/gitTest/_git/gitTest"),
                };
                var sourceProvider = new ConcreteSourceProvider();
                sourceProvider.Initialize(tc);

                // Act.
                string hashKey = sourceProvider.GetBuildDirectoryHashKey(executionContext.Object, endpoint);

                // Assert.
                Assert.Equal("5c5c3d7ac33cca6604736eb3af977f23f1cf1146", hashKey);
            }
        }
    }
}
