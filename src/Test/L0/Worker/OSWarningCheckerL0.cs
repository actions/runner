using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class OSWarningCheckerL0
    {
        private CancellationTokenSource _ecTokenSource;
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;
        private OSWarningChecker _osWarningChecker;
        private List<Issue> _issues;
        private string _workFolder;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void CheckOS_FileNotExists()
        {
            try
            {
                // Arrange
                Setup();
                var osWarnings = new List<OSWarning>
                {
                    new OSWarning
                    {
                        FilePath = Path.Combine(_workFolder, "os-release"),
                        RegularExpression = "some OS version",
                        Warning = "Some OS version will be deprecated soon"
                    },
                };

                // Act
                await _osWarningChecker.CheckOSAsync(_ec.Object, osWarnings);

                // Assert
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _ec.Object.Global.JobTelemetry.Count);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void CheckOS_CaseInsensitive()
        {
            try
            {
                // Arrange
                Setup();
                var osWarnings = new List<OSWarning>
                {
                    new OSWarning
                    {
                        FilePath = Path.Combine(_workFolder, "os-release"),
                        RegularExpression = "some OS verSION",
                        Warning = "Some OS version will be deprecated soon"
                    },
                };
                File.WriteAllText(Path.Combine(_workFolder, "os-release"), "some OS version\n");

                // Act
                await _osWarningChecker.CheckOSAsync(_ec.Object, osWarnings);

#if OS_WINDOWS || OS_OSX
                // Assert
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _ec.Object.Global.JobTelemetry.Count);
#else
                // Assert
                Assert.Equal(1, _issues.Count);
                Assert.Equal(IssueType.Warning, _issues[0].Type);
                Assert.Equal("Some OS version will be deprecated soon", _issues[0].Message);
                Assert.Equal(1, _ec.Object.Global.JobTelemetry.Count);
                Assert.Equal(JobTelemetryType.General, _ec.Object.Global.JobTelemetry[0].Type);
                Assert.Equal("OS warning: Some OS version will be deprecated soon", _ec.Object.Global.JobTelemetry[0].Message);
#endif
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void CheckOS_MatchesOnceWithinAFile()
        {
            try
            {
                // Arrange
                Setup();
                var osWarnings = new List<OSWarning>
                {
                    new OSWarning
                    {
                        FilePath = Path.Combine(_workFolder, "os-release"),
                        RegularExpression = "some OS version",
                        Warning = "Some OS version will be deprecated soon"
                    },
                };
                File.WriteAllText(Path.Combine(_workFolder, "os-release"), "some OS version\nsome OS version\n");

                // Act
                await _osWarningChecker.CheckOSAsync(_ec.Object, osWarnings);

#if OS_WINDOWS || OS_OSX
                // Assert
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _ec.Object.Global.JobTelemetry.Count);
#else
                // Assert
                Assert.Equal(1, _issues.Count);
                Assert.Equal(IssueType.Warning, _issues[0].Type);
                Assert.Equal("Some OS version will be deprecated soon", _issues[0].Message);
                Assert.Equal(1, _ec.Object.Global.JobTelemetry.Count);
                Assert.Equal(JobTelemetryType.General, _ec.Object.Global.JobTelemetry[0].Type);
                Assert.Equal("OS warning: Some OS version will be deprecated soon", _ec.Object.Global.JobTelemetry[0].Message);
#endif
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void CheckOS_MatchesOnceAcrossFiles()
        {
            try
            {
                // Arrange
                Setup();
                var osWarnings = new List<OSWarning>
                {
                    new OSWarning
                    {
                        FilePath = Path.Combine(_workFolder, "os-release"),
                        RegularExpression = "some OS version",
                        Warning = "Some OS version will be deprecated soon"
                    },
                    new OSWarning
                    {
                        FilePath = Path.Combine(_workFolder, "os-release-2"),
                        RegularExpression = "some OS version",
                        Warning = "Some OS version will be deprecated soon"
                    },
                };
                File.WriteAllText(Path.Combine(_workFolder, "os-release"), "some OS version\n");
                File.WriteAllText(Path.Combine(_workFolder, "os-release-2"), "some OS version\n");

                // Act
                await _osWarningChecker.CheckOSAsync(_ec.Object, osWarnings);

#if OS_WINDOWS || OS_OSX
                // Assert
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _ec.Object.Global.JobTelemetry.Count);
#else
                // Assert
                Assert.Equal(1, _issues.Count);
                Assert.Equal(IssueType.Warning, _issues[0].Type);
                Assert.Equal("Some OS version will be deprecated soon", _issues[0].Message);
                Assert.Equal(1, _ec.Object.Global.JobTelemetry.Count);
                Assert.Equal(JobTelemetryType.General, _ec.Object.Global.JobTelemetry[0].Type);
                Assert.Equal("OS warning: Some OS version will be deprecated soon", _ec.Object.Global.JobTelemetry[0].Message);
#endif
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void CheckOS_LogsTelemetryUponException()
        {
            try
            {
                // Arrange
                Setup();
                var osWarnings = new List<OSWarning>
                {
                    new OSWarning
                    {
                        FilePath = Path.Combine(_workFolder, "os-release"),
                        RegularExpression = "abc[", // Invalid pattern
                        Warning = "Some OS version will be deprecated soon"
                    },
                };
                File.WriteAllText(Path.Combine(_workFolder, "os-release"), "some OS version\n");

                // Act
                await _osWarningChecker.CheckOSAsync(_ec.Object, osWarnings);

#if OS_WINDOWS || OS_OSX
                // Assert
                Assert.Equal(0, _issues.Count);
                Assert.Equal(0, _ec.Object.Global.JobTelemetry.Count);
#else
                // Assert
                Assert.Equal(0, _issues.Count);
                Assert.Equal(1, _ec.Object.Global.JobTelemetry.Count);
                Assert.Equal(JobTelemetryType.General, _ec.Object.Global.JobTelemetry[0].Type);
                Assert.Equal(
                    $"An error occurred while checking OS warnings for file '{osWarnings[0].FilePath}' and regex '{osWarnings[0].RegularExpression}': Invalid pattern 'abc[' at offset 4. Unterminated [] set.",
                    _ec.Object.Global.JobTelemetry[0].Message);
#endif
            }
            finally
            {
                Teardown();
            }
        }

        private void Setup([CallerMemberName] string name = "")
        {
            _issues = new List<Issue>();

            // Test host context
            _hc = new TestHostContext(this, name);

            // Random work folder
            _workFolder = _hc.GetDirectory(WellKnownDirectory.Work);
            Directory.CreateDirectory(_workFolder);

            // Execution context token source
            _ecTokenSource?.Dispose();
            _ecTokenSource = new CancellationTokenSource();

            // Execution context
            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Global).Returns(new GlobalContext());
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);
            _ec.Object.Global.JobTelemetry = new List<JobTelemetry>();
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<ExecutionContextLogOptions>())).Callback((Issue issue, ExecutionContextLogOptions logOptions) => { _issues.Add(issue); });

            // OS warning checker
            _osWarningChecker = new OSWarningChecker();
            _osWarningChecker.Initialize(_hc);
        }

        private void Teardown()
        {
            _hc?.Dispose();
            if (!string.IsNullOrEmpty(_workFolder) && Directory.Exists(_workFolder))
            {
                Directory.Delete(_workFolder, recursive: true);
            }
        }
    }
}
