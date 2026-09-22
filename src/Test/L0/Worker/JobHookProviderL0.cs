using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class JobHookProviderL0
    {
        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData(ActionRunStage.Pre)]
        [InlineData(ActionRunStage.Post)]
        public async Task ExposesEnvironmentNameToHook(ActionRunStage stage)
        {
            var hookEnvironment = await RunHookAsync(new ActionsEnvironmentReference("testbench"), stage);

            Assert.Equal("testbench", hookEnvironment[Constants.Hooks.EnvironmentNameVariable]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ExposesEmptyEnvironmentNameWhenJobHasNoEnvironment()
        {
            var hookEnvironment = await RunHookAsync(null, ActionRunStage.Pre);

            Assert.Equal(string.Empty, hookEnvironment[Constants.Hooks.EnvironmentNameVariable]);
        }

        private async Task<Dictionary<string, string>> RunHookAsync(ActionsEnvironmentReference actionsEnvironment, ActionRunStage stage, [CallerMemberName] string name = "")
        {
            using (var hc = new TestHostContext(this, name))
            {
                var hookPath = Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), "hook.sh");
                Directory.CreateDirectory(Path.GetDirectoryName(hookPath));
                File.WriteAllText(hookPath, "exit 0");

                Dictionary<string, string> hookEnvironment = null;
                var handlerFactory = new Mock<IHandlerFactory>();
                handlerFactory.Setup(x => x.Create(It.IsAny<IExecutionContext>(), It.IsAny<ActionStepDefinitionReference>(), It.IsAny<IStepHost>(), It.IsAny<ActionExecutionData>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Variables>(), It.IsAny<string>(), It.IsAny<List<JobExtensionRunner>>()))
                              .Callback((IExecutionContext executionContext, ActionStepDefinitionReference action, IStepHost stepHost, ActionExecutionData data, Dictionary<string, string> inputs, Dictionary<string, string> environment, Variables runtimeVariables, string actionDirectory, List<JobExtensionRunner> localActionContainerSetupSteps) =>
                              {
                                  hookEnvironment = environment;
                              })
                              .Returns(new Mock<IHandler>().Object);
                hc.SetSingleton<IHandlerFactory>(handlerFactory.Object);
                hc.EnqueueInstance<IDefaultStepHost>(new Mock<IDefaultStepHost>().Object);
                hc.EnqueueInstance<IFileCommandManager>(new Mock<IFileCommandManager>().Object);

                // The environment is only known on the job-level context; the hook runs in a child context.
                var jobContext = new Mock<IExecutionContext>();
                jobContext.Setup(x => x.ActionsEnvironment).Returns(actionsEnvironment);
                var stepContext = new Mock<IExecutionContext>();
                stepContext.Setup(x => x.Root).Returns(jobContext.Object);
                stepContext.Setup(x => x.Global).Returns(new GlobalContext { PrependPath = new List<string>() });

                var hookProvider = new JobHookProvider();
                hookProvider.Initialize(hc);
                await hookProvider.RunHook(stepContext.Object, new JobHookData(stage, hookPath));

                Assert.NotNull(hookEnvironment);
                return hookEnvironment;
            }
        }
    }
}
