using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageCommandsTests
    {
        private Mock<IExecutionContext> _ec;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private Mock<ICodeCoverageSummaryReader> _mocksummaryReader;
        private Mock<IExtensionManager> _mockExtensionManager;
        private Mock<ICodeCoveragePublisher> _mockCodeCoveragePublisher;
        private Mock<IAsyncCommandContext> _mockCommandContext;
        private TestHostContext hc;
        private List<CodeCoverageStatistics> _codeCoverageStatistics;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishCodeCoverageWithNoCCTool()
        {
            SetupMocks();
            var publishCCCommand = new CodeCoverageCommands();
            publishCCCommand.Initialize(hc);
            var command = new Command("codecoverage", "publish");
            command.Properties.Add("summaryfile", "a.xml");
            Assert.Throws<ArgumentException>(() => publishCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishCodeCoverageWithNoSummaryFileInput()
        {
            SetupMocks();
            var publishCCCommand = new CodeCoverageCommands();
            publishCCCommand.Initialize(hc);
            var command = new Command("codecoverage", "publish");
            Assert.Throws<ArgumentException>(() => publishCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishCodeCoverageWithInvalidCCTool()
        {
            SetupMocks();
            var publishCCCommand = new CodeCoverageCommands();
            publishCCCommand.Initialize(hc);
            var command = new Command("codecoverage", "publish");
            command.Properties.Add("codecoveragetool", "InvalidTool");
            command.Properties.Add("summaryfile", "a.xml");
            Assert.Throws<ArgumentException>(() => publishCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishesCCFilesWhenCodeCoverageDataIsNull()
        {
            SetupMocks();
            var summaryFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(summaryFile, "test");
                var publishCCCommand = new CodeCoverageCommands();
                publishCCCommand.Initialize(hc);
                var command = new Command("codecoverage", "publish");
                command.Properties.Add("codecoveragetool", "mockCCTool");
                command.Properties.Add("summaryfile", summaryFile);
                _mocksummaryReader.Setup(x => x.GetCodeCoverageSummary(It.IsAny<IExecutionContext>(), It.IsAny<string>()))
                   .Returns((List<CodeCoverageStatistics>)null);
                publishCCCommand.ProcessCommand(_ec.Object, command);
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<List<Tuple<string, string>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
            }
            finally
            {
                File.Delete(summaryFile);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishCCFilesWithOnlyReportDirectoryInput()
        {
            SetupMocks();
            var reportDirectory = Path.Combine(Path.GetTempPath(), "reportDirectory");
            var summaryFile = Path.Combine(reportDirectory, "summary.xml");
            try
            {
                Directory.CreateDirectory(reportDirectory);
                File.WriteAllText(summaryFile, "test");
                _mockCodeCoveragePublisher.Setup(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<List<Tuple<string, string>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                                          .Callback<IAsyncCommandContext, Guid, long, List<Tuple<string, string>>, bool, CancellationToken>
                                          ((context, project, containerId, files, browsable, cancellationToken) =>
                                          {
                                              Assert.NotNull(files);
                                              Assert.Equal(1, files.Count);
                                          });
                var publishCCCommand = new CodeCoverageCommands();
                publishCCCommand.Initialize(hc);
                var command = new Command("codecoverage", "publish");
                command.Properties.Add("codecoveragetool", "mockCCTool");
                command.Properties.Add("summaryfile", summaryFile);
                command.Properties.Add("reportdirectory", reportDirectory);
                publishCCCommand.ProcessCommand(_ec.Object, command);
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageSummaryAsync(It.IsAny<IEnumerable<CodeCoverageStatistics>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<List<Tuple<string, string>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
            }
            finally
            {
                Directory.Delete(reportDirectory, true);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishCCFilesWithOnlyAdditionalFilesInput()
        {
            SetupMocks();
            var summaryFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(summaryFile, "test");
                _mockCodeCoveragePublisher.Setup(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<List<Tuple<string, string>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                                          .Callback<IAsyncCommandContext, Guid, long, List<Tuple<string, string>>, bool, CancellationToken>
                                          ((context, project, containerId, files, browsable, cancellationToken) =>
                                          {
                                              Assert.NotNull(files);
                                              Assert.Equal(2, files.Count);
                                          });
                var publishCCCommand = new CodeCoverageCommands();
                publishCCCommand.Initialize(hc);
                var command = new Command("codecoverage", "publish");
                command.Properties.Add("codecoveragetool", "mockCCTool");
                command.Properties.Add("summaryfile", summaryFile);
                command.Properties.Add("additionalcodecoveragefiles", summaryFile);
                publishCCCommand.ProcessCommand(_ec.Object, command);
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageSummaryAsync(It.IsAny<IEnumerable<CodeCoverageStatistics>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<List<Tuple<string, string>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
            }
            finally
            {
                File.Delete(summaryFile);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishCCWithBothReportDirectoryAndAdditioanlFilesInputs()
        {
            SetupMocks();
            var reportDirectory = Path.Combine(Path.GetTempPath(), "reportDirectory");
            var summaryFile = Path.Combine(reportDirectory, "summary.xml");
            try
            {
                Directory.CreateDirectory(reportDirectory);
                File.WriteAllText(summaryFile, "test");
                _mockCodeCoveragePublisher.Setup(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<List<Tuple<string, string>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                                          .Callback<IAsyncCommandContext, Guid, long, List<Tuple<string, string>>, bool, CancellationToken>
                                          ((context, project, containerId, files, browsable, cancellationToken) =>
                                          {
                                              Assert.NotNull(files);
                                              Assert.Equal(2, files.Count);
                                          });
                var publishCCCommand = new CodeCoverageCommands();
                publishCCCommand.Initialize(hc);
                var command = new Command("codecoverage", "publish");
                command.Properties.Add("codecoveragetool", "mockCCTool");
                command.Properties.Add("summaryfile", summaryFile);
                command.Properties.Add("reportdirectory", reportDirectory);
                command.Properties.Add("additionalcodecoveragefiles", summaryFile);
                publishCCCommand.ProcessCommand(_ec.Object, command);
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageSummaryAsync(It.IsAny<IEnumerable<CodeCoverageStatistics>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<List<Tuple<string, string>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
            }
            finally
            {
                Directory.Delete(reportDirectory, true);
            }
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            hc = new TestHostContext(this, name);
            _codeCoverageStatistics = new List<CodeCoverageStatistics> { new CodeCoverageStatistics { Label = "label", Covered = 10, Total = 10, Position = 1 } };
            _mocksummaryReader = new Mock<ICodeCoverageSummaryReader>();
            _mocksummaryReader.Setup(x => x.Name).Returns("mockCCTool");
            _mocksummaryReader.Setup(x => x.GetCodeCoverageSummary(It.IsAny<IExecutionContext>(), It.IsAny<string>()))
                .Returns(_codeCoverageStatistics);
            hc.SetSingleton(_mocksummaryReader.Object);

            _mockExtensionManager = new Mock<IExtensionManager>();
            _mockExtensionManager.Setup(x => x.GetExtensions<ICodeCoverageSummaryReader>()).Returns(new List<ICodeCoverageSummaryReader> { _mocksummaryReader.Object });
            hc.SetSingleton(_mockExtensionManager.Object);

            _mockCodeCoveragePublisher = new Mock<ICodeCoveragePublisher>();
            hc.SetSingleton(_mockCodeCoveragePublisher.Object);

            _mockCommandContext = new Mock<IAsyncCommandContext>();
            hc.EnqueueInstance(_mockCommandContext.Object);

            var endpointAuthorization = new EndpointAuthorization()
            {
                Scheme = EndpointAuthorizationSchemes.OAuth
            };
            List<string> warnings;
            var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
            variables.Set("build.buildId", "1");
            variables.Set("build.containerId", "1");
            variables.Set("system.teamProjectId", "46075F24-A6B9-447E-BEF0-E1D5592D9E39");
            endpointAuthorization.Parameters[EndpointAuthorizationParameters.AccessToken] = "accesstoken";

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Endpoints).Returns(new List<ServiceEndpoint> { new ServiceEndpoint { Url = new Uri("http://dummyurl"), Name = ServiceEndpoints.SystemVssConnection, Authorization = endpointAuthorization } });
            _ec.Setup(x => x.Variables).Returns(variables);
            var asyncCommands = new List<IAsyncCommandContext>();
            _ec.Setup(x => x.AsyncCommands).Returns(asyncCommands);
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>()))
            .Callback<Issue>
            ((issue) =>
            {
                if (issue.Type == IssueType.Warning)
                {
                    _warnings.Add(issue.Message);
                }
                else if (issue.Type == IssueType.Error)
                {
                    _errors.Add(issue.Message);
                }
            });
        }
    }
}
