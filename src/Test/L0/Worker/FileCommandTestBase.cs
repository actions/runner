using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using Moq;
using Xunit;
using DTWebApi = GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common.Tests.Worker
{
    public abstract class FileCommandTestBase<T>
        where T : IFileCommandExtension, new()
    {

        protected void TestDirectoryNotFound()
        {
            using (var hostContext = Setup())
            {
                var stateFile = Path.Combine(_rootDirectory, "directory-not-found", "env");
                _fileCmdExtension.ProcessCommand(_executionContext.Object, stateFile, null);
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _store.Count);
            }
        }

        protected TestHostContext Setup([CallerMemberName] string name = "")
        {
            _issues = new List<Tuple<DTWebApi.Issue, string>>();

            var hostContext = new TestHostContext(this, name);

            // Trace
            _trace = hostContext.GetTrace();

            // Directory for test data
            var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
            ArgUtil.NotNullOrEmpty(workDirectory, nameof(workDirectory));
            Directory.CreateDirectory(workDirectory);
            _rootDirectory = Path.Combine(workDirectory, nameof(T));
            Directory.CreateDirectory(_rootDirectory);

            // Execution context
            _executionContext = new Mock<IExecutionContext>();
            _executionContext.Setup(x => x.Global)
                .Returns(new GlobalContext
                {
                    EnvironmentVariables = new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer),
                    WriteDebug = true,
                });
            _executionContext.Setup(x => x.AddIssue(It.IsAny<DTWebApi.Issue>(), It.IsAny<ExecutionContextLogOptions>()))
                .Callback((DTWebApi.Issue issue, ExecutionContextLogOptions logOptions) =>
                {
                    var resolvedMessage = issue.Message;
                    if (logOptions.WriteToLog && !string.IsNullOrEmpty(logOptions.LogMessageOverride))
                    {
                        resolvedMessage = logOptions.LogMessageOverride;
                    }
                    _issues.Add(new(issue, resolvedMessage));
                    _trace.Info($"Issue '{issue.Type}': {resolvedMessage}");
                });
            _executionContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string tag, string message) =>
                {
                    _trace.Info($"{tag}{message}");
                });

            _store = PostSetup();

            _fileCmdExtension = new T();
            _fileCmdExtension.Initialize(hostContext);

            return hostContext;
        }

        protected abstract IDictionary<string, string> PostSetup();

        protected static readonly string BREAK = Environment.NewLine;

        protected IFileCommandExtension _fileCmdExtension {get; private set; }
        protected Mock<IExecutionContext> _executionContext {get; private set; }
        protected List<Tuple<DTWebApi.Issue, string>> _issues {get; private set; }
        protected IDictionary<string, string> _store { get; private set; }
        protected string _rootDirectory {get; private set; }
        protected ITraceWriter _trace {get; private set; }
    }
}
