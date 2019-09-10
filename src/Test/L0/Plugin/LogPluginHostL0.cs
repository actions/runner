﻿using GitHub.DistributedTask.WebApi;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using System.Collections.ObjectModel;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Tests.LogPluginHost
{
    public sealed class LogPluginHostL0
    {
        public class TestTrace : IRunnerLogPluginTrace
        {
            private Tracing _trace;
            public TestTrace(TestHostContext testHostContext)
            {
                _trace = testHostContext.GetTrace();
            }

            public List<string> Outputs = new List<string>();

            public void Output(string message)
            {
                Outputs.Add(message);
                _trace.Info(message);
            }

            public void Trace(string message)
            {
                Outputs.Add(message);
                _trace.Info(message);
            }
        }

        public class TestPlugin1 : IRunnerLogPlugin
        {
            public string FriendlyName => "Test1";

            public Task FinalizeAsync(IRunnerLogPluginContext context)
            {
                context.Output("Done");
                return Task.CompletedTask;
            }

            public Task<bool> InitializeAsync(IRunnerLogPluginContext context)
            {
                return Task.FromResult(true);
            }

            public Task ProcessLineAsync(IRunnerLogPluginContext context, Pipelines.ActionStepDefinitionReference step, string line)
            {
                context.Output(line);
                return Task.CompletedTask;
            }
        }

        public class TestPlugin2 : IRunnerLogPlugin
        {
            public string FriendlyName => "Test2";

            public Task FinalizeAsync(IRunnerLogPluginContext context)
            {
                context.Output("Done");
                return Task.CompletedTask;
            }

            public Task<bool> InitializeAsync(IRunnerLogPluginContext context)
            {
                return Task.FromResult(true);
            }

            public Task ProcessLineAsync(IRunnerLogPluginContext context, Pipelines.ActionStepDefinitionReference step, string line)
            {
                context.Output(line);
                return Task.CompletedTask;
            }
        }

        public class TestPluginSlow : IRunnerLogPlugin
        {
            public string FriendlyName => "TestSlow";

            public Task FinalizeAsync(IRunnerLogPluginContext context)
            {
                context.Output("Done");
                return Task.CompletedTask;
            }

            public Task<bool> InitializeAsync(IRunnerLogPluginContext context)
            {
                return Task.FromResult(true);
            }

            public async Task ProcessLineAsync(IRunnerLogPluginContext context, Pipelines.ActionStepDefinitionReference step, string line)
            {
                context.Output("BLOCK");
                await Task.Delay(-1);
            }
        }

        public class TestPluginSlowRecover : IRunnerLogPlugin
        {
            private int _counter = 0;
            public string FriendlyName => "TestSlowRecover";

            public Task FinalizeAsync(IRunnerLogPluginContext context)
            {
                context.Output("Done");
                return Task.CompletedTask;
            }

            public Task<bool> InitializeAsync(IRunnerLogPluginContext context)
            {
                return Task.FromResult(true);
            }

            public async Task ProcessLineAsync(IRunnerLogPluginContext context, Pipelines.ActionStepDefinitionReference step, string line)
            {
                if (_counter++ < 1)
                {
                    context.Output("SLOW");
                    await Task.Delay(400);
                }
                else
                {
                    context.Output(line);
                }
            }
        }

        public class TestPluginNotInitialized : IRunnerLogPlugin
        {
            public string FriendlyName => "TestNotInitialized";

            public Task FinalizeAsync(IRunnerLogPluginContext context)
            {
                context.Output("Done");
                return Task.CompletedTask;
            }

            public Task<bool> InitializeAsync(IRunnerLogPluginContext context)
            {
                return Task.FromResult(false);
            }

            public Task ProcessLineAsync(IRunnerLogPluginContext context, Pipelines.ActionStepDefinitionReference step, string line)
            {
                context.Output(line);
                return Task.CompletedTask;
            }
        }

        public class TestPluginException : IRunnerLogPlugin
        {
            public string FriendlyName => "TestException";

            public Task FinalizeAsync(IRunnerLogPluginContext context)
            {
                if (context.Variables.ContainsKey("throw_finalize"))
                {
                    throw new NotSupportedException();
                }
                else
                {
                    context.Output("Done");
                    return Task.CompletedTask;
                }
            }

            public Task<bool> InitializeAsync(IRunnerLogPluginContext context)
            {
                if (context.Variables.ContainsKey("throw_initialize"))
                {
                    throw new NotSupportedException();
                }
                else
                {
                    return Task.FromResult(true);
                }
            }

            public Task ProcessLineAsync(IRunnerLogPluginContext context, Pipelines.ActionStepDefinitionReference step, string line)
            {
                if (context.Variables.ContainsKey("throw_process"))
                {
                    throw new NotSupportedException();
                }
                else
                {
                    context.Output(line);
                    return Task.CompletedTask;
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task LogPluginHost_RunSinglePlugin()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                RunnerLogPluginHostContext hostContext = CreateTestLogPluginHostContext();
                List<IRunnerLogPlugin> plugins = new List<IRunnerLogPlugin>() { new TestPlugin1() };

                TestTrace trace = new TestTrace(tc);
                RunnerLogPluginHost logPluginHost = new RunnerLogPluginHost(hostContext, plugins, trace);
                var task = logPluginHost.Run();
                for (int i = 0; i < 1000; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
                }

                await Task.Delay(1000);
                logPluginHost.Finish();
                await task;

                Assert.True(trace.Outputs.Contains("Test1: 0"));
                Assert.True(trace.Outputs.Contains("Test1: 999"));
                Assert.True(trace.Outputs.Contains("Test1: Done"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task LogPluginHost_RunSinglePluginWithEmptyLinesInput()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                RunnerLogPluginHostContext hostContext = CreateTestLogPluginHostContext();
                List<IRunnerLogPlugin> plugins = new List<IRunnerLogPlugin>() { new TestPlugin1() };

                TestTrace trace = new TestTrace(tc);
                RunnerLogPluginHost logPluginHost = new RunnerLogPluginHost(hostContext, plugins, trace);
                var task = logPluginHost.Run();
                for (int i = 0; i < 100; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
                }

                for (int i = 0; i < 100; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
                }

                for (int i = 0; i < 10; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:");
                }

                for (int i = 100; i < 200; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
                }

                await Task.Delay(1000);
                logPluginHost.Finish();
                await task;

                Assert.True(trace.Outputs.Contains("Test1: 0"));
                Assert.True(trace.Outputs.Contains("Test1: 99"));
                Assert.True(trace.Outputs.Contains("Test1: "));
                Assert.True(trace.Outputs.Contains("Test1: 100"));
                Assert.True(trace.Outputs.Contains("Test1: 199"));
                Assert.Equal(10, trace.Outputs.FindAll(x => x == "Test1: ").Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task LogPluginHost_RunMultiplePlugins()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                RunnerLogPluginHostContext hostContext = CreateTestLogPluginHostContext();
                List<IRunnerLogPlugin> plugins = new List<IRunnerLogPlugin>() { new TestPlugin1(), new TestPlugin2() };

                TestTrace trace = new TestTrace(tc);
                RunnerLogPluginHost logPluginHost = new RunnerLogPluginHost(hostContext, plugins, trace);
                var task = logPluginHost.Run();
                for (int i = 0; i < 1000; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
                }

                await Task.Delay(1000);
                logPluginHost.Finish();
                await task;

                Assert.True(trace.Outputs.Contains("Test1: 0"));
                Assert.True(trace.Outputs.Contains("Test1: 999"));
                Assert.True(trace.Outputs.Contains("Test1: Done"));
                Assert.True(trace.Outputs.Contains("Test2: 0"));
                Assert.True(trace.Outputs.Contains("Test2: 999"));
                Assert.True(trace.Outputs.Contains("Test2: Done"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task LogPluginHost_ShortCircuitSlowPlugin()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                RunnerLogPluginHostContext hostContext = CreateTestLogPluginHostContext();
                List<IRunnerLogPlugin> plugins = new List<IRunnerLogPlugin>() { new TestPlugin1(), new TestPluginSlow() };

                TestTrace trace = new TestTrace(tc);
                RunnerLogPluginHost logPluginHost = new RunnerLogPluginHost(hostContext, plugins, trace, 100, 100);
                var task = logPluginHost.Run();
                for (int i = 0; i < 1000; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
                }

                await Task.Delay(1000);
                logPluginHost.Finish();
                await task;

                // regular one still running
                Assert.True(trace.Outputs.Contains("Test1: 0"));
                Assert.True(trace.Outputs.Contains("Test1: 999"));
                Assert.True(trace.Outputs.Contains("Test1: Done"));

                // slow one got killed
                Assert.False(trace.Outputs.Contains("TestSlow: Done"));
                Assert.True(trace.Outputs.Exists(x => x.Contains("Plugin has been short circuited")));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task LogPluginHost_SlowPluginRecover()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                RunnerLogPluginHostContext hostContext = CreateTestLogPluginHostContext();
                List<IRunnerLogPlugin> plugins = new List<IRunnerLogPlugin>() { new TestPlugin1(), new TestPluginSlowRecover() };

                TestTrace trace = new TestTrace(tc);
                RunnerLogPluginHost logPluginHost = new RunnerLogPluginHost(hostContext, plugins, trace, 950, 100);
                var task = logPluginHost.Run();
                for (int i = 0; i < 1000; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
                }

                await Task.Delay(1000);
                logPluginHost.Finish();
                await task;

                // regular one still running
                Assert.True(trace.Outputs.Contains("Test1: 0"));
                Assert.True(trace.Outputs.Contains("Test1: 999"));
                Assert.True(trace.Outputs.Contains("Test1: Done"));

                Assert.True(trace.Outputs.Contains("TestSlowRecover: Done"));
                Assert.True(trace.Outputs.Exists(x => x.Contains("TestPluginSlowRecover' has too many buffered outputs.")));
                Assert.True(trace.Outputs.Exists(x => x.Contains("TestPluginSlowRecover' has cleared out buffered outputs.")));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task LogPluginHost_NotInitialized()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                RunnerLogPluginHostContext hostContext = CreateTestLogPluginHostContext();
                List<IRunnerLogPlugin> plugins = new List<IRunnerLogPlugin>() { new TestPlugin1(), new TestPluginNotInitialized() };

                TestTrace trace = new TestTrace(tc);
                RunnerLogPluginHost logPluginHost = new RunnerLogPluginHost(hostContext, plugins, trace);
                var task = logPluginHost.Run();
                for (int i = 0; i < 1000; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
                }

                await Task.Delay(1000);
                logPluginHost.Finish();
                await task;

                // regular one still running
                Assert.True(trace.Outputs.Contains("Test1: 0"));
                Assert.True(trace.Outputs.Contains("Test1: 999"));
                Assert.True(trace.Outputs.Contains("Test1: Done"));

                Assert.True(!trace.Outputs.Contains("TestNotInitialized: 0"));
                Assert.True(!trace.Outputs.Contains("TestNotInitialized: Done"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task LogPluginHost_HandleInitialExceptions()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                RunnerLogPluginHostContext hostContext = CreateTestLogPluginHostContext();
                hostContext.Variables["throw_initialize"] = "1";

                List<IRunnerLogPlugin> plugins = new List<IRunnerLogPlugin>() { new TestPlugin1(), new TestPluginException() };

                TestTrace trace = new TestTrace(tc);
                RunnerLogPluginHost logPluginHost = new RunnerLogPluginHost(hostContext, plugins, trace);
                var task = logPluginHost.Run();
                for (int i = 0; i < 1000; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
                }

                await Task.Delay(1000);
                logPluginHost.Finish();
                await task;

                // regular one still running
                Assert.True(trace.Outputs.Contains("Test1: 0"));
                Assert.True(trace.Outputs.Contains("Test1: 999"));
                Assert.True(trace.Outputs.Contains("Test1: Done"));

                Assert.True(!trace.Outputs.Contains("TestException: 0"));
                Assert.True(!trace.Outputs.Contains("TestException: Done"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task LogPluginHost_HandleProcessExceptions()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                RunnerLogPluginHostContext hostContext = CreateTestLogPluginHostContext();
                hostContext.Variables["throw_process"] = "1";

                List<IRunnerLogPlugin> plugins = new List<IRunnerLogPlugin>() { new TestPlugin1(), new TestPluginException() };

                TestTrace trace = new TestTrace(tc);
                RunnerLogPluginHost logPluginHost = new RunnerLogPluginHost(hostContext, plugins, trace);
                var task = logPluginHost.Run();
                for (int i = 0; i < 1000; i++)
                {
                    logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
                }

                await Task.Delay(1000);
                logPluginHost.Finish();
                await task;

                // regular one still running
                Assert.True(trace.Outputs.Contains("Test1: 0"));
                Assert.True(trace.Outputs.Contains("Test1: 999"));
                Assert.True(trace.Outputs.Contains("Test1: Done"));

                Assert.True(!trace.Outputs.Contains("TestException: 0"));
                Assert.True(!trace.Outputs.Contains("TestException: 999"));
                Assert.True(trace.Outputs.Contains("TestException: Done"));
            }
        }

        // potential bug in XUnit cause the test failure.
        // [Fact]
        // [Trait("Level", "L0")]
        // [Trait("Category", "Plugin")]
        // public async Task LogPluginHost_HandleFinalizeExceptions()
        // {
        //     using (TestHostContext tc = new TestHostContext(this))
        //     {
        //         AgentLogPluginHostContext hostContext = CreateTestLogPluginHostContext();
        //         hostContext.Variables["throw_finalize"] = "1";

        //         List<IAgentLogPlugin> plugins = new List<IAgentLogPlugin>() { new TestPlugin1(), new TestPluginException() };

        //         TestTrace trace = new TestTrace(tc);
        //         AgentLogPluginHost logPluginHost = new AgentLogPluginHost(hostContext, plugins, trace);
        //         var task = logPluginHost.Run();
        //         for (int i = 0; i < 1000; i++)
        //         {
        //             logPluginHost.EnqueueOutput($"{Guid.Empty.ToString("D")}:{i}");
        //         }

        //         await Task.Delay(1000);
        //         logPluginHost.Finish();
        //         await task;

        //         // regular one still running
        //         Assert.True(trace.Outputs.Contains("Test1: 0"));
        //         Assert.True(trace.Outputs.Contains("Test1: 999"));
        //         Assert.True(trace.Outputs.Contains("Test1: Done"));

        //         Assert.True(trace.Outputs.Contains("TestException: 0"));
        //         Assert.True(trace.Outputs.Contains("TestException: 999"));
        //         Assert.True(!trace.Outputs.Contains("TestException: Done"));
        //     }
        // }

        private RunnerLogPluginHostContext CreateTestLogPluginHostContext()
        {
            RunnerLogPluginHostContext hostContext = new RunnerLogPluginHostContext()
            {
                Endpoints = new List<ServiceEndpoint>(),
                PluginAssemblies = new List<string>(),
                Variables = new Dictionary<string, VariableValue>(),
                Steps = new Dictionary<string, Pipelines.ActionStepDefinitionReference>(),
                Context = new Pipelines.ContextData.DictionaryContextData() { { "runner", new Pipelines.ContextData.DictionaryContextData() } }
            };

            hostContext.Steps[Guid.Empty.ToString("D")] = new Pipelines.RepositoryPathReference()
            {
                RepositoryType = "GitHub",
                Ref = "master"
            };

            var systemConnection = new ServiceEndpoint()
            {
                Name = WellKnownServiceEndpointNames.SystemVssConnection,
                Id = Guid.NewGuid(),
                Url = new Uri("https://dev.azure.com/test"),
                Authorization = new EndpointAuthorization()
                {
                    Scheme = EndpointAuthorizationSchemes.OAuth,
                    Parameters = { { EndpointAuthorizationParameters.AccessToken, "Test" } }
                }
            };

            hostContext.Endpoints.Add(systemConnection);

            return hostContext;
        }
    }
}
