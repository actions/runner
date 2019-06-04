using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Build;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker.Build
{
    public sealed class SourceProviderL0
    {
        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Worker")]
        // public void ReturnsCorrectHashKey()
        // {
        //     using (TestHostContext tc = new TestHostContext(this))
        //     {
        //         // Arrange.
        //         var executionContext = new Mock<IExecutionContext>();
        //         List<string> warnings;
        //         executionContext
        //             .Setup(x => x.Variables)
        //             .Returns(new Variables(tc, copy: new Dictionary<string, VariableValue>(), warnings: out warnings));
        //         executionContext.Object.Variables.Set(Constants.Variables.System.CollectionId, "7aee6dde-6381-4098-93e7-50a8264cf066");
        //         executionContext.Object.Variables.Set(Constants.Variables.System.DefinitionId, "7");

        //         Pipelines.RepositoryResource repository = new Pipelines.RepositoryResource() { Url = new Uri("http://contoso:8080/tfs/DefaultCollection/gitTest/_git/gitTest") };
                
        //         // Act.
        //         string hashKey = repository.GetSourceDirectoryHashKey(executionContext.Object);

        //         // Assert.
        //         Assert.Equal("5c5c3d7ac33cca6604736eb3af977f23f1cf1146", hashKey);
        //     }
        // }
    }
}
