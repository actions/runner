using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Moq;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageCommandExtensionTests
    {
        private Mock<IExecutionContext> _ec;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private Mock<ICodeCoverageSummaryReader> _mocksummaryReader;
        private Mock<ICodeCoverageEnabler> _mockCodeCoverageEnabler;
        private Mock<IExtensionManager> _mockExtensionManager;
        private Mock<ICodeCoveragePublisher> _mockCodeCoveragePublisher;
        private Mock<IAsyncCommandContext> _mockCommandContext;
        private TestHostContext _hc;
        private List<CodeCoverageStatistics> _codeCoverageStatistics;
        private Variables _variables;
        private static string _includeFilter = "+:abc.sd,+:ab.as.*";
        private static string _excludeFilter = "-:abs.*.cf,-:sd.*";
        private static string _includes = (_includeFilter.Replace("+:", "")).Replace(",", ":");
        private static string _excludes = (_excludeFilter.Replace("-:", "")).Replace(",", ":");

        #region publish code coverage tests
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishCodeCoverageWithNoCCTool()
        {
            SetupMocks();
            var publishCCCommand = new CodeCoverageCommandExtension();
            publishCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "publish");
            command.Properties.Add("summaryfile", "a.xml");
            Assert.Throws<ArgumentException>(() => publishCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishCodeCoverageWithRelease()
        {
            SetupMocks();
            var publishCCCommand = new CodeCoverageCommandExtension();
            publishCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "publish");
            _variables.Set("system.hostType", "release");
            publishCCCommand.ProcessCommand(_ec.Object, command);
            Assert.Equal(1, _warnings.Count);
            Assert.Equal(0, _errors.Count);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishCodeCoverageWithNoSummaryFileInput()
        {
            SetupMocks();
            var publishCCCommand = new CodeCoverageCommandExtension();
            publishCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "publish");
            Assert.Throws<ArgumentException>(() => publishCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void PublishCodeCoverageWithInvalidCCTool()
        {
            SetupMocks();
            var publishCCCommand = new CodeCoverageCommandExtension();
            publishCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "publish");
            command.Properties.Add("codecoveragetool", "InvalidTool");
            command.Properties.Add("summaryfile", "a.xml");
            Assert.Throws<ArgumentException>(() => publishCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        public void Publish_CoberturaNewIndexFile()
        {
            SetupMocks();
            var reportDirectory = Path.Combine(Path.GetTempPath(), "reportDirectory");
            var coberturaXml = Path.Combine(reportDirectory, "coberturaValid.xml");

            try
            {
                Directory.CreateDirectory(reportDirectory);
                File.WriteAllText(coberturaXml, CodeCoverageTestConstants.ValidCoberturaXml);
                File.WriteAllText((Path.Combine(reportDirectory, "index.html")), string.Empty);
                File.WriteAllText((Path.Combine(reportDirectory, "frame-summary.html")), string.Empty);

                var publishCCCommand = new CodeCoverageCommandExtension();
                publishCCCommand.Initialize(_hc);
                var command = new Command("codecoverage", "publish");
                command.Properties.Add("codecoveragetool", "cobertura");
                command.Properties.Add("summaryfile", coberturaXml);
                command.Properties.Add("reportdirectory", reportDirectory);
                publishCCCommand.ProcessCommand(_ec.Object, command);
                Assert.Equal(0, _warnings.Count);
                Assert.Equal(0, _errors.Count);
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageSummaryAsync(It.IsAny<IEnumerable<CodeCoverageStatistics>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<List<Tuple<string, string>>>(), It.Is<bool>(browsable => browsable == true), It.IsAny<CancellationToken>()));
                Assert.True(File.Exists(Path.Combine(reportDirectory, "frame-summary.html")));
                Assert.True(File.Exists(Path.Combine(reportDirectory, "indexnew.html")));

            }
            finally
            {
                Directory.Delete(reportDirectory, true);
            }
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
                var publishCCCommand = new CodeCoverageCommandExtension();
                publishCCCommand.Initialize(_hc);
                var command = new Command("codecoverage", "publish");
                command.Properties.Add("codecoveragetool", "mockCCTool");
                command.Properties.Add("summaryfile", summaryFile);
                _mocksummaryReader.Setup(x => x.GetCodeCoverageSummary(It.IsAny<IExecutionContext>(), It.IsAny<string>()))
                   .Returns((List<CodeCoverageStatistics>)null);
                publishCCCommand.ProcessCommand(_ec.Object, command);
                Assert.Equal(1, _warnings.Count);
                Assert.Equal(0, _errors.Count);
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
                var publishCCCommand = new CodeCoverageCommandExtension();
                publishCCCommand.Initialize(_hc);
                var command = new Command("codecoverage", "publish");
                command.Properties.Add("codecoveragetool", "mockCCTool");
                command.Properties.Add("summaryfile", summaryFile);
                command.Properties.Add("reportdirectory", reportDirectory);
                publishCCCommand.ProcessCommand(_ec.Object, command);
                Assert.Equal(0, _warnings.Count);
                Assert.Equal(0, _errors.Count);
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageSummaryAsync(It.IsAny<IEnumerable<CodeCoverageStatistics>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(),
                    It.Is<List<Tuple<string, string>>>(files => files.Count == 1), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
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
                var publishCCCommand = new CodeCoverageCommandExtension();
                publishCCCommand.Initialize(_hc);
                var command = new Command("codecoverage", "publish");
                command.Properties.Add("codecoveragetool", "mockCCTool");
                command.Properties.Add("summaryfile", summaryFile);
                command.Properties.Add("additionalcodecoveragefiles", summaryFile);
                publishCCCommand.ProcessCommand(_ec.Object, command);
                Assert.Equal(0, _warnings.Count);
                Assert.Equal(0, _errors.Count);
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageSummaryAsync(It.IsAny<IEnumerable<CodeCoverageStatistics>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(),
                    It.Is<List<Tuple<string, string>>>(files => files.Count == 2), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
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
                var publishCCCommand = new CodeCoverageCommandExtension();
                publishCCCommand.Initialize(_hc);
                var command = new Command("codecoverage", "publish");
                command.Properties.Add("codecoveragetool", "mockCCTool");
                command.Properties.Add("summaryfile", summaryFile);
                command.Properties.Add("reportdirectory", reportDirectory);
                command.Properties.Add("additionalcodecoveragefiles", summaryFile);
                publishCCCommand.ProcessCommand(_ec.Object, command);
                Assert.Equal(0, _warnings.Count);
                Assert.Equal(0, _errors.Count);
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageSummaryAsync(It.IsAny<IEnumerable<CodeCoverageStatistics>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
                _mockCodeCoveragePublisher.Verify(x => x.PublishCodeCoverageFilesAsync(It.IsAny<IAsyncCommandContext>(), It.IsAny<Guid>(), It.IsAny<long>(),
                    It.Is<List<Tuple<string, string>>>(files => files.Count == 2), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
            }
            finally
            {
                Directory.Delete(reportDirectory, true);
            }
        }
        #endregion

        #region enable code coverage tests
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCCFailsWithErrorWhenBuildFileDoesnotExist()
        {
            SetupMocks();
            var enableCCCommand = new CodeCoverageCommandExtension();
            enableCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "enable");
            command.Properties.Add("buildtool", "mockBuildTool");
            command.Properties.Add("buildfile", "invalid.xml");
            command.Properties.Add("codecoveragetool", "mockCCTool");
            Assert.Throws<FileNotFoundException>(() => enableCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCCFailsWithErrorWhenBuildToolIsInvalid()
        {
            SetupMocks();
            var enableCCCommand = new CodeCoverageCommandExtension();
            enableCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "enable");
            command.Properties.Add("buildtool", "invalidBuildTool");
            command.Properties.Add("buildfile", Path.GetTempFileName());
            command.Properties.Add("codecoveragetool", "mockCCTool");
            Assert.Throws<ArgumentException>(() => enableCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCCWithInvalidCodeCoverageToolThrowsException()
        {
            SetupMocks();
            var enableCCCommand = new CodeCoverageCommandExtension();
            enableCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "enable");
            command.Properties.Add("buildtool", "mockBuildTool");
            command.Properties.Add("buildfile", Path.GetTempFileName());
            command.Properties.Add("codecoveragetool", "invalidCCTool");
            Assert.Throws<ArgumentException>(() => enableCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCCWhenNoCodeCoverageToolShouldPass()
        {
            SetupMocks();
            var enableCCCommand = new CodeCoverageCommandExtension();
            enableCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "enable");
            enableCCCommand.ProcessCommand(_ec.Object, command);
            Assert.Equal(_errors.Count, 0);
            Assert.Equal(_warnings.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCCWithemptyBuildFileThrowsException()
        {
            SetupMocks();
            var enableCCCommand = new CodeCoverageCommandExtension();
            enableCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "enable");
            command.Properties.Add("buildtool", "mockBuildTool");
            command.Properties.Add("buildfile", "");
            command.Properties.Add("codecoveragetool", "mockCCTool");
            Assert.Throws<ArgumentException>(() => enableCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCCWithInvalidClassFilterThrowsException()
        {
            SetupMocks();
            var enableCCCommand = new CodeCoverageCommandExtension();
            enableCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "enable");
            command.Properties.Add("buildtool", "mockBuildTool");
            command.Properties.Add("classfilter", "invalid");
            command.Properties.Add("codecoveragetool", "mockCCTool");
            Assert.Throws<ArgumentException>(() => enableCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCCWithSpacesInClassFilterThrowsException()
        {
            SetupMocks();
            var enableCCCommand = new CodeCoverageCommandExtension();
            enableCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "enable");
            command.Properties.Add("buildtool", "mockBuildTool");
            command.Properties.Add("classfilter", " ,   , ,, , ");
            command.Properties.Add("codecoveragetool", "mockCCTool");
            Assert.Throws<ArgumentException>(() => enableCCCommand.ProcessCommand(_ec.Object, command));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCCWithValidClassFilterShouldPass()
        {
            SetupMocks();
            var enableCCCommand = new CodeCoverageCommandExtension();
            enableCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "enable");
            command.Properties.Add("buildtool", "mockBuildTool");
            command.Properties.Add("classfilter", _includeFilter + "," + _excludeFilter);
            command.Properties.Add("codecoveragetool", "mockCCTool");
            command.Properties.Add("buildfile", Path.GetTempFileName());
            enableCCCommand.ProcessCommand(_ec.Object, command);
            _mockCodeCoverageEnabler.Verify(x => x.EnableCodeCoverage(It.IsAny<IExecutionContext>(), It.Is<CodeCoverageEnablerInputs>(ccInputs => ccInputs.Include == _includes && ccInputs.Exclude == _excludes)));
            Assert.Equal(0, _warnings.Count);
            Assert.Equal(0, _errors.Count);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCCWithValidClassIncludeFilterShouldPass()
        {
            SetupMocks();
            var enableCCCommand = new CodeCoverageCommandExtension();
            enableCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "enable");
            command.Properties.Add("buildtool", "mockBuildTool");
            command.Properties.Add("classfilter", _includeFilter);
            command.Properties.Add("codecoveragetool", "mockCCTool");
            command.Properties.Add("buildfile", Path.GetTempFileName());
            enableCCCommand.ProcessCommand(_ec.Object, command);
            _mockCodeCoverageEnabler.Verify(x => x.EnableCodeCoverage(It.IsAny<IExecutionContext>(), It.Is<CodeCoverageEnablerInputs>(ccInputs => ccInputs.Include == _includes && string.IsNullOrWhiteSpace(ccInputs.Exclude))));
            Assert.Equal(0, _warnings.Count);
            Assert.Equal(0, _errors.Count);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCCWithValidClassExcludeFilterShouldPass()
        {
            SetupMocks();
            var enableCCCommand = new CodeCoverageCommandExtension();
            enableCCCommand.Initialize(_hc);
            var command = new Command("codecoverage", "enable");
            command.Properties.Add("buildtool", "mockBuildTool");
            command.Properties.Add("classfilter", _excludeFilter);
            command.Properties.Add("codecoveragetool", "mockCCTool");
            command.Properties.Add("buildfile", Path.GetTempFileName());
            enableCCCommand.ProcessCommand(_ec.Object, command);
            _mockCodeCoverageEnabler.Verify(x => x.EnableCodeCoverage(It.IsAny<IExecutionContext>(), It.Is<CodeCoverageEnablerInputs>(ccInputs => ccInputs.Exclude == _excludes && string.IsNullOrWhiteSpace(ccInputs.Include))));
            Assert.Equal(0, _warnings.Count);
            Assert.Equal(0, _errors.Count);
        }
        #endregion

        private void SetupMocks([CallerMemberName] string name = "")
        {
            _hc = new TestHostContext(this, name);
            _codeCoverageStatistics = new List<CodeCoverageStatistics> { new CodeCoverageStatistics { Label = "label", Covered = 10, Total = 10, Position = 1 } };
            _mocksummaryReader = new Mock<ICodeCoverageSummaryReader>();
            if (String.Equals(name,"Publish_CoberturaNewIndexFile"))
            {
                _mocksummaryReader.Setup(x => x.Name).Returns("cobertura");
            }
            else _mocksummaryReader.Setup(x => x.Name).Returns("mockCCTool");
            _mocksummaryReader.Setup(x => x.GetCodeCoverageSummary(It.IsAny<IExecutionContext>(), It.IsAny<string>()))
                .Returns(_codeCoverageStatistics);
            _hc.SetSingleton(_mocksummaryReader.Object);

            _mockCodeCoverageEnabler = new Mock<ICodeCoverageEnabler>();
            _mockCodeCoverageEnabler.Setup(x => x.Name).Returns("mockCCTool_mockBuildTool");
            _mockCodeCoverageEnabler.Setup(x => x.EnableCodeCoverage(It.IsAny<IExecutionContext>(), It.IsAny<CodeCoverageEnablerInputs>()));
            _hc.SetSingleton(_mockCodeCoverageEnabler.Object);

            _mockExtensionManager = new Mock<IExtensionManager>();
            _mockExtensionManager.Setup(x => x.GetExtensions<ICodeCoverageSummaryReader>()).Returns(new List<ICodeCoverageSummaryReader> { _mocksummaryReader.Object });
            _mockExtensionManager.Setup(x => x.GetExtensions<ICodeCoverageEnabler>()).Returns(new List<ICodeCoverageEnabler> { _mockCodeCoverageEnabler.Object });
            _hc.SetSingleton(_mockExtensionManager.Object);

            _mockCodeCoveragePublisher = new Mock<ICodeCoveragePublisher>();
            _hc.SetSingleton(_mockCodeCoveragePublisher.Object);

            _mockCommandContext = new Mock<IAsyncCommandContext>();
            _hc.EnqueueInstance(_mockCommandContext.Object);

            var endpointAuthorization = new EndpointAuthorization()
            {
                Scheme = EndpointAuthorizationSchemes.OAuth
            };
            List<string> warnings;
            _variables = new Variables(_hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
            _variables.Set("build.buildId", "1");
            _variables.Set("build.containerId", "1");
            _variables.Set("system.teamProjectId", "46075F24-A6B9-447E-BEF0-E1D5592D9E39");
            _variables.Set("system.hostType", "build");
            endpointAuthorization.Parameters[EndpointAuthorizationParameters.AccessToken] = "accesstoken";

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Endpoints).Returns(new List<ServiceEndpoint> { new ServiceEndpoint { Url = new Uri("http://dummyurl"), Name = ServiceEndpoints.SystemVssConnection, Authorization = endpointAuthorization } });
            _ec.Setup(x => x.Variables).Returns(_variables);
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
