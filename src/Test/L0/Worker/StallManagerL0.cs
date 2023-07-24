using System;
using System.Timers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Xunit;
using DTWebApi = GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.DistributedTask.WebApi;
using System.Diagnostics;

namespace GitHub.Runner.Common.Tests.Worker
{

    public class MockTimer : ITimer
    {
        public bool _started = false;
        public bool _stopped = false;
        public bool _reset = false;
        public double Interval { get; set; }
        public event ElapsedEventHandler Elapsed;
        public bool AutoReset { get; set; }

        public MockTimer()
        {
            Interval = 1;
        }

        public void Dispose() { }

        public void Start()
        {
            _started = true;
            if (_stopped)
            {
                _stopped = false;
                _reset = true;
            }
        }
        public void Stop()
        {
            _reset = false;
            _started = false;
            _stopped = true;
        }

        public void TimeElapsed()
        {
            this.Elapsed.Invoke(this, new EventArgs() as ElapsedEventArgs);
        }
    }

    public sealed class StallManagerL0
    {
        private Mock<IExecutionContext> _executionContext;
        private List<Tuple<DTWebApi.Issue, string>> _issues;
        private Variables _variables;

        private TestHostContext Setup(
            [CallerMemberName] string name = "",
            ContainerInfo jobContainer = null,
            ContainerInfo stepContainer = null)
        {
            var hostContext = new TestHostContext(this, name);
            _executionContext = new Mock<IExecutionContext>();
            _issues = new List<Tuple<DTWebApi.Issue, string>>();

            // Variables to test for secret scrubbing & FF options
            _variables = new Variables(hostContext, new Dictionary<string, VariableValue>
                {
                    { "DistributedTask.AllowRunnerStallDetect", new VariableValue("true", true) },
                });

            _executionContext.Setup(x => x.Global)
                .Returns(new GlobalContext
                {
                    Container = jobContainer,
                    Variables = _variables,
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
                });

            return hostContext;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OutputWarningMessageOnTimeElapsed()
        {
            MockTimer timer = new MockTimer();
            using (Setup())
            using (StallManager manager = new StallManager(_executionContext.Object, TimeSpan.FromMinutes(10).TotalMilliseconds, timer))
            {

                timer.TimeElapsed();

                Assert.Equal(1, _issues.Count);
                Assert.Equal("No output has been detected in the last 10 minutes and the process has not yet exited. This step may have stalled and might require some investigation.", _issues[0].Item1.Message);
                Assert.Equal(DTWebApi.IssueType.Warning, _issues[0].Item1.Type);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ValidateTimerResetOnNewMessage()
        {

            MockTimer timer = new MockTimer();
            using (Setup())
            using (StallManager manager = new StallManager(_executionContext.Object, TimeSpan.FromMinutes(10).TotalMilliseconds, timer))
            {

                // Trigger 2 elapsed
                timer.TimeElapsed();
                timer.TimeElapsed();

                // Should have triggered 2 warnings
                Assert.Equal(2, _issues.Count);
                Assert.Equal("No output has been detected in the last 10 minutes and the process has not yet exited. This step may have stalled and might require some investigation.", _issues[0].Item1.Message);
                Assert.Equal("No output has been detected in the last 20 minutes and the process has not yet exited. This step may have stalled and might require some investigation.", _issues[1].Item1.Message);
                Assert.Equal(DTWebApi.IssueType.Warning, _issues[0].Item1.Type);
                Assert.Equal(DTWebApi.IssueType.Warning, _issues[1].Item1.Type);

                // Should reset timer
                manager.OnDataReceived(null, null);

                Assert.True(timer._reset);
                Assert.Equal(2, _issues.Count);

                // Trigger another elapsed interval
                timer.TimeElapsed();

                // Timer should have reset and one new warning should have been added
                Assert.Equal(3, _issues.Count);
                Assert.Equal("No output has been detected in the last 10 minutes and the process has not yet exited. This step may have stalled and might require some investigation.", _issues[2].Item1.Message);
                Assert.Equal(DTWebApi.IssueType.Warning, _issues[2].Item1.Type);

            }
        }
    }
}
