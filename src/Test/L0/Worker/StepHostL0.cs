using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Moq;
using Xunit;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using GitHub.Runner.Worker.Container;
using GitHub.DistributedTask.Pipelines.ContextData;
using System.Linq;
using GitHub.DistributedTask.Pipelines;
namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class StepHostL0
    {
        private Mock<IExecutionContext> _ec;
        private Mock<IDockerCommandManager> _dc;
        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);

            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            _ec.Setup(x => x.Global).Returns(new GlobalContext { WriteDebug = true });
            var trace = hc.GetTrace();
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });

            _dc = new Mock<IDockerCommandManager>();
            hc.SetSingleton(_dc.Object);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DetermineNodeRuntimeVersionInContainerAsync()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var sh = new ContainerStepHost();
                sh.Initialize(hc);
                sh.Container = new ContainerInfo() { ContainerId = "1234abcd" };

                _dc.Setup(d => d.DockerExec(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                               .ReturnsAsync(0);

                // Act.
                var nodeVersion = await sh.DetermineNodeRuntimeVersion(_ec.Object, "node12");

                // Assert.
                Assert.Equal("node12", nodeVersion);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ContainerStepHost_GetExpressionValues()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                const string source = "/home/username/Projects/work/runner/_layout";
                var containerInfo = new ContainerInfo();
                containerInfo.ContainerId = "test";

                containerInfo.AddPathTranslateMapping($"{source}/_work", "/__w");
                containerInfo.AddPathTranslateMapping($"{source}/_temp", "/__t");
                containerInfo.AddPathTranslateMapping($"{source}/externals", "/__e");

                containerInfo.AddPathTranslateMapping($"{source}/_work/_temp/_github_home", "/github/home");
                containerInfo.AddPathTranslateMapping($"{source}/_work/_temp/_github_workflow", "/github/workflow");


                foreach (var v in new List<string>() {
                    $"{source}/_work",
                    $"{source}/externals",
                    $"{source}/_work/_temp",
                    $"{source}/_work/_actions",
                    $"{source}/_work/_tool",
                })
                {
                    containerInfo.MountVolumes.Add(new MountVolume(v, containerInfo.TranslateToContainerPath(v)));
                };


                var sh = new ContainerStepHost();
                sh.Container = containerInfo;

                var expressionValues = new DictionaryContextData();

                var inputGithubContext = new GitHubContext();
                var inputeRunnerContext = new RunnerContext();
                expressionValues["github"] = inputGithubContext;
                expressionValues["runner"] = inputeRunnerContext;
                inputGithubContext["action_path"] = new StringContextData("/home/username/Projects/work/runner/_layout/_work/_actions/owner/composite/main");
                inputGithubContext["action"] = new StringContextData("__owner_composite");
                inputGithubContext["api_url"] = new StringContextData("https://api.github.com/custom/path");
                inputGithubContext["env"] = new StringContextData("/home/username/Projects/work/runner/_layout/_work/_temp/_runner_file_commands/set_env_265698aa-7f38-40f5-9316-5c01a3153672");
                inputGithubContext["path"] = new StringContextData("/home/username/Projects/work/runner/_layout/_work/_temp/_runner_file_commands/add_path_265698aa-7f38-40f5-9316-5c01a3153672");
                inputGithubContext["event_path"] = new StringContextData("/home/username/Projects/work/runner/_layout/_work/_temp/_github_workflow/event.json");
                inputGithubContext["repository"] = new StringContextData("owner/repo-name");
                inputGithubContext["run_id"] = new StringContextData("2033211332");
                inputGithubContext["workflow"] = new StringContextData("Name of Workflow");
                inputGithubContext["workspace"] = new StringContextData("/home/username/Projects/work/runner/_layout/_work/step-order/step-order");
                inputeRunnerContext["temp"] = new StringContextData("/home/username/Projects/work/runner/_layout/_work/_temp");
                inputeRunnerContext["tool_cache"] = new StringContextData("/home/username/Projects/work/runner/_layout/_work/_tool");


                var expectedGithubContext = new GitHubContext();
                var expectedRunnerContext = new RunnerContext();
                expectedGithubContext["action_path"] = new StringContextData("/__w/_actions/owner/composite/main");
                expectedGithubContext["action"] = new StringContextData("__owner_composite");
                expectedGithubContext["api_url"] = new StringContextData("https://api.github.com/custom/path");
                expectedGithubContext["env"] = new StringContextData("/__w/_temp/_runner_file_commands/set_env_265698aa-7f38-40f5-9316-5c01a3153672");
                expectedGithubContext["path"] = new StringContextData("/__w/_temp/_runner_file_commands/add_path_265698aa-7f38-40f5-9316-5c01a3153672");
                expectedGithubContext["event_path"] = new StringContextData("/github/workflow/event.json");
                expectedGithubContext["repository"] = new StringContextData("owner/repo-name");
                expectedGithubContext["run_id"] = new StringContextData("2033211332");
                expectedGithubContext["workflow"] = new StringContextData("Name of Workflow");
                expectedGithubContext["workspace"] = new StringContextData("/__w/step-order/step-order");
                expectedRunnerContext["temp"] = new StringContextData("/__w/_temp");
                expectedRunnerContext["tool_cache"] = new StringContextData("/__w/_tool");

                _ec.Setup(x => x.ExpressionValues).Returns(expressionValues);

                var translatedExpressionValues = sh.GetExpressionValues(_ec.Object);

                var dict = translatedExpressionValues["github"].AssertDictionary($"expected context github to be a dictionary");
                foreach (var key in dict.Keys.ToList())
                {
                    var expect = dict[key].AssertString("expect string");
                    var outcome = expectedGithubContext[key].AssertString("expect string");
                    Assert.Equal(expect.Value, outcome.Value);
                }

                dict = translatedExpressionValues["runner"].AssertDictionary($"expected context runner to be a dictionary");
                foreach (var key in dict.Keys.ToList())
                {
                    var expect = dict[key].AssertString("expect string");
                    var outcome = expectedRunnerContext[key].AssertString("expect string");
                    Assert.Equal(expect.Value, outcome.Value);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DetermineNodeRuntimeVersionInAlpineContainerAsync()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var sh = new ContainerStepHost();
                sh.Initialize(hc);
                sh.Container = new ContainerInfo() { ContainerId = "1234abcd" };

                _dc.Setup(d => d.DockerExec(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                    .Callback((IExecutionContext ec, string id, string options, string command, List<string> output) =>
                    {
                        output.Add("alpine");
                    })
                    .ReturnsAsync(0);

                // Act.
                var nodeVersion = await sh.DetermineNodeRuntimeVersion(_ec.Object, "node16");

                // Assert.
                Assert.Equal("node16_alpine", nodeVersion);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DetermineNodeRuntimeVersionInUnknowContainerAsync()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var sh = new ContainerStepHost();
                sh.Initialize(hc);
                sh.Container = new ContainerInfo() { ContainerId = "1234abcd" };

                _dc.Setup(d => d.DockerExec(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                    .Callback((IExecutionContext ec, string id, string options, string command, List<string> output) =>
                    {
                        output.Add("github");
                    })
                    .ReturnsAsync(0);

                // Act.
                var nodeVersion = await sh.DetermineNodeRuntimeVersion(_ec.Object, "node16");

                // Assert.
                Assert.Equal("node16", nodeVersion);
            }
        }
    }
}
