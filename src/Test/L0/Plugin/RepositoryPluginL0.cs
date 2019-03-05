using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Agent.Plugins.Repository;
using Agent.Sdk;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Plugin
{
    public sealed class RepositoryPluginL0
    {
        private CheckoutTask _checkoutTask;
        private AgentTaskPluginExecutionContext _executionContext;
        private Mock<ISourceProvider> _sourceProvider;
        private Mock<ISourceProviderFactory> _sourceProviderFactory;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_CheckoutTask_MergesCheckoutOptions_Basic()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                Setup(tc);
                var repository = _executionContext.Repositories.Single();
                repository.Properties.Set(
                    Pipelines.RepositoryPropertyNames.CheckoutOptions,
                    new JObject
                    {
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Clean, "clean value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth, "fetch depth value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs, "lfs value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials, "persist credentials value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules, "submodules value" },
                    });

                await _checkoutTask.RunAsync(_executionContext, CancellationToken.None);

                Assert.Equal("clean value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Clean]);
                Assert.Equal("fetch depth value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth]);
                Assert.Equal("lfs value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs]);
                Assert.Equal("persist credentials value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials]);
                Assert.Equal("submodules value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_CheckoutTask_MergesCheckoutOptions_CaseInsensitive()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                Setup(tc);
                var repository = _executionContext.Repositories.Single();
                repository.Properties.Set(
                    Pipelines.RepositoryPropertyNames.CheckoutOptions,
                    new JObject
                    {
                        { "CLean", "clean value" },
                        { "FETCHdepth", "fetch depth value" },
                        { "LFs", "lfs value" },
                        { "PERSISTcredentials", "persist credentials value" },
                        { "SUBmodules", "submodules value" },
                    });

                await _checkoutTask.RunAsync(_executionContext, CancellationToken.None);

                Assert.Equal("clean value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Clean]);
                Assert.Equal("fetch depth value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth]);
                Assert.Equal("lfs value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs]);
                Assert.Equal("persist credentials value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials]);
                Assert.Equal("submodules value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_CheckoutTask_MergesCheckoutOptions_DoesNotClobberExistingValue()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                Setup(tc);
                var repository = _executionContext.Repositories.Single();
                repository.Properties.Set(
                    Pipelines.RepositoryPropertyNames.CheckoutOptions,
                    new JObject
                    {
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Clean, "clean value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth, "fetch depth value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs, "lfs value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials, "persist credentials value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules, "submodules value" },
                    });
                _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Clean] = "existing clean value";
                _executionContext.Inputs["FETCHdepth"] = "existing fetch depth value";
                _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs] = string.Empty;
                _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials] = null;

                await _checkoutTask.RunAsync(_executionContext, CancellationToken.None);

                Assert.Equal("existing clean value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Clean]);
                Assert.Equal("existing fetch depth value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth]);
                Assert.Equal("lfs value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs]);
                Assert.Equal("persist credentials value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials]);
                Assert.Equal("submodules value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_CheckoutTask_MergesCheckoutOptions_FeatureFlagOff()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                Setup(tc);
                var repository = _executionContext.Repositories.Single();
                repository.Properties.Set(
                    Pipelines.RepositoryPropertyNames.CheckoutOptions,
                    new JObject
                    {
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Clean, "clean value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth, "fetch depth value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs, "lfs value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials, "persist credentials value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules, "submodules value" },
                    });
                _executionContext.Variables["MERGE_CHECKOUT_OPTIONS"] = "FALse";

                await _checkoutTask.RunAsync(_executionContext, CancellationToken.None);

                Assert.False(_executionContext.Inputs.ContainsKey(Pipelines.PipelineConstants.CheckoutTaskInputs.Clean));
                Assert.False(_executionContext.Inputs.ContainsKey(Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth));
                Assert.False(_executionContext.Inputs.ContainsKey(Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs));
                Assert.False(_executionContext.Inputs.ContainsKey(Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials));
                Assert.False(_executionContext.Inputs.ContainsKey(Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_CheckoutTask_MergesCheckoutOptions_UnexpectedCheckoutOption()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                Setup(tc);
                var repository = _executionContext.Repositories.Single();
                repository.Properties.Set(
                    Pipelines.RepositoryPropertyNames.CheckoutOptions,
                    new JObject
                    {
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Clean, "clean value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth, "fetch depth value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs, "lfs value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials, "persist credentials value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules, "submodules value" },
                        { "unexpected", "unexpected value" },
                    });

                await _checkoutTask.RunAsync(_executionContext, CancellationToken.None);

                Assert.Equal("clean value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Clean]);
                Assert.Equal("fetch depth value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth]);
                Assert.Equal("lfs value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs]);
                Assert.Equal("persist credentials value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials]);
                Assert.Equal("submodules value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules]);
                Assert.False(_executionContext.Inputs.ContainsKey("unexpected"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_CleanupTask_MergesCheckoutOptions()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                Setup(tc);
                var repository = _executionContext.Repositories.Single();
                repository.Properties.Set(
                    Pipelines.RepositoryPropertyNames.CheckoutOptions,
                    new JObject
                    {
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Clean, "clean value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth, "fetch depth value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs, "lfs value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials, "persist credentials value" },
                        { Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules, "submodules value" },
                    });

                await _checkoutTask.RunAsync(_executionContext, CancellationToken.None);

                Assert.Equal("clean value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Clean]);
                Assert.Equal("fetch depth value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth]);
                Assert.Equal("lfs value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs]);
                Assert.Equal("persist credentials value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials]);
                Assert.Equal("submodules value", _executionContext.Inputs[Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_NoPathInput()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                Setup(tc);
                var repository = _executionContext.Repositories.Single();
                var currentPath = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
                Directory.CreateDirectory(currentPath);

                await _checkoutTask.RunAsync(_executionContext, CancellationToken.None);

                var actualPath = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);

                Assert.Equal(actualPath, currentPath);

                var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                File.Copy(tc.TraceFileName, temp);
                Assert.True(File.ReadAllText(temp).Contains($"##vso[plugininternal.updaterepositorypath alias=myRepo;]{actualPath}"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_PathInputMoveFolder()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                Setup(tc);
                var repository = _executionContext.Repositories.Single();
                var currentPath = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
                Directory.CreateDirectory(currentPath);

                _executionContext.Inputs["Path"] = "test";

                await _checkoutTask.RunAsync(_executionContext, CancellationToken.None);

                var actualPath = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);

                Assert.NotEqual(actualPath, currentPath);
                Assert.Equal(actualPath, Path.Combine(tc.GetDirectory(WellKnownDirectory.Work), "1", "test"));
                Assert.True(Directory.Exists(actualPath));
                Assert.False(Directory.Exists(currentPath));

                var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                File.Copy(tc.TraceFileName, temp);
                Assert.True(File.ReadAllText(temp).Contains($"##vso[plugininternal.updaterepositorypath alias=myRepo;]{actualPath}"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_NoPathInputMoveBackToDefault()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                var trace = tc.GetTrace();
                Setup(tc);
                var repository = _executionContext.Repositories.Single();
                repository.Properties.Set(Pipelines.RepositoryPropertyNames.Path, Path.Combine(tc.GetDirectory(WellKnownDirectory.Work), "1", "test"));
                var currentPath = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
                Directory.CreateDirectory(currentPath);

                await _checkoutTask.RunAsync(_executionContext, CancellationToken.None);
                var actualPath = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);

                Assert.Equal(actualPath, Path.Combine(tc.GetDirectory(WellKnownDirectory.Work), "1", "s"));
                Assert.True(Directory.Exists(actualPath));
                Assert.False(Directory.Exists(currentPath));

                var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                File.Copy(tc.TraceFileName, temp);
                Assert.True(File.ReadAllText(temp).Contains($"##vso[plugininternal.updaterepositorypath alias=myRepo;]{actualPath}"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_InvalidPathInput()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                var trace = tc.GetTrace();
                Setup(tc);
                var repository = _executionContext.Repositories.Single();
                var currentPath = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
                Directory.CreateDirectory(currentPath);
                _executionContext.Inputs["Path"] = "..";

                var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await _checkoutTask.RunAsync(_executionContext, CancellationToken.None));
                Assert.True(ex.Message.Contains("should resolve to a directory under"));

                var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                File.Copy(tc.TraceFileName, temp);
                Assert.False(File.ReadAllText(temp).Contains($"##vso[plugininternal.updaterepositorypath alias=myRepo;]"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task RepositoryPlugin_UpdatePathEvenCheckoutFail()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                var trace = tc.GetTrace();
                Setup(tc);

                _sourceProvider.Setup(x => x.GetSourceAsync(It.IsAny<AgentTaskPluginExecutionContext>(), It.IsAny<Pipelines.RepositoryResource>(), It.IsAny<CancellationToken>()))
                               .Throws(new InvalidOperationException("RIGHT"));

                var repository = _executionContext.Repositories.Single();
                var currentPath = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
                Directory.CreateDirectory(currentPath);

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _checkoutTask.RunAsync(_executionContext, CancellationToken.None));
                Assert.True(ex.Message.Contains("RIGHT"));

                var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                File.Copy(tc.TraceFileName, temp);
                Assert.True(File.ReadAllText(temp).Contains($"##vso[plugininternal.updaterepositorypath alias=myRepo;]{currentPath}"));
            }
        }

        private void Setup(TestHostContext hostContext)
        {
            var repo = new Pipelines.RepositoryResource()
            {
                Alias = "myRepo",
                Type = Pipelines.RepositoryTypes.Git,
            };

            repo.Properties.Set<string>(Pipelines.RepositoryPropertyNames.Path, Path.Combine(hostContext.GetDirectory(WellKnownDirectory.Work), "1", "s"));

            _executionContext = new AgentTaskPluginExecutionContext(hostContext.GetTrace())
            {
                Endpoints = new List<ServiceEndpoint>(),
                Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { Pipelines.PipelineConstants.CheckoutTaskInputs.Repository, "myRepo" },
                },
                Repositories = new List<Pipelines.RepositoryResource>
                {
                    repo
                },
                Variables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "agent.builddirectory",
                         Path.Combine(hostContext.GetDirectory(WellKnownDirectory.Work), "1")
                    },
                    {
                        "agent.workfolder",
                        hostContext.GetDirectory(WellKnownDirectory.Work)
                    },
                    {
                        "agent.tempdirectory",
                        hostContext.GetDirectory(WellKnownDirectory.Temp)
                    }
                },
            };

            _sourceProvider = new Mock<ISourceProvider>();

            _sourceProviderFactory = new Mock<ISourceProviderFactory>();
            _sourceProviderFactory
                .Setup(x => x.GetSourceProvider(It.IsAny<String>()))
                .Returns(_sourceProvider.Object);

            _checkoutTask = new CheckoutTask(_sourceProviderFactory.Object);
        }
    }
}
