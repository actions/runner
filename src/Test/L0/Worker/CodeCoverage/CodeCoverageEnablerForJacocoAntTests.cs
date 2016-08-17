using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageEnablerForJacocoAntTests : IDisposable
    {
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private string _classDirectories = "class1,class2";
        private string _srcDirectory = "src";
        private string _summaryFile = "summary.xml";
        private string _reportDirectory = "codeCoverage";
        private string _cCReportTask = "CodeCoverageReport";
        private string _classFilter = "+:com.*.*,+:app.me*.*,-:me.*.*,-:a.b.*,-:my.com.*.*";
        private string _include = "com.*.*:app.me*.*";
        private string _exclude = "me.*.*:a.b.*:my.com.*.*";
        private string _sampleBuildFilePath;
        private string _sampleReportBuildFilePath;
        private string _sourceDirectory = Path.Combine(Path.GetTempPath(), "AntJacoco");

        public CodeCoverageEnablerForJacocoAntTests()
        {
            Directory.CreateDirectory(_sourceDirectory);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForJacocoTest()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath);
            VerifyJacocoReportForAnt();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForJacocoWithBuildFileNotInRepository()
        {
            var sourceDirectory = Path.Combine(Path.GetTempPath(), "sourceDirectory2");
            Directory.CreateDirectory(sourceDirectory);
            SetupMocks(sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath);
            VerifyJacocoReportForAnt();
            Directory.Delete(sourceDirectory, true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForJacocoTestWithSingle_includeFilter()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", "+:app.com.SampleTest");
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath, includes: "app.com.SampleTest", excludes: string.Empty);
            VerifyJacocoReportForAnt("**/app/com/SampleTest.class", string.Empty);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForJacocoTestWithSingleExcludeFilter()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", "-:app.com.SampleTest");
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath, includes: string.Empty, excludes: "app.com.SampleTest");
            VerifyJacocoReportForAnt(string.Empty, "**/app/com/SampleTest.class");
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForJacocoTestWithNo_includeExcludeFilters()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath, includes: string.Empty, excludes: string.Empty);
            VerifyJacocoReportForAnt(string.Empty, string.Empty);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForJacocoTestWithFullClassNameFilters()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", "+:app.com.SampleTest,+:app.*.UtilTest,+:app2*,-:app.com.SampleTest,-:app.*.UtilTest,-:app3*");
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath, includes: "app.com.SampleTest:app.*.UtilTest:app2*", excludes: "app.com.SampleTest:app.*.UtilTest:app3*");
            VerifyJacocoReportForAnt("**/app/com/SampleTest.class,**/app/*/UtilTest.class,**/app2*/**", "**/app/com/SampleTest.class,**/app/*/UtilTest.class,**/app3*/**");
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForJacocoWhenCodeCoverageIsAlreadyEnabled()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildWithCCJacocoXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath);
            VerifyJacocoReportForAnt();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageWithMultipleTestNodesTest()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildWithMultipleNodesXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 2, buildFilePath: _sampleBuildFilePath);
            VerifyJacocoReportForAnt();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntJacocoThrowsIf_classDirectoriesIsInvalid()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", "class*");
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForJacocoWhenReportBuildFileIsNotPassed()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithNoClassDirectory()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithNoSummaryFile()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithNoReportDirectory()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithNoReportTask()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithInvalidBuildXml()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.InvalidBuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            Assert.Throws<XmlException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntDoesNotThrowWithNoTests()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildWithNoTestsXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            try
            {
                enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            }
            catch (Exception ex)
            {
                Assert.True(false, "Expected no exception. But got " + ex.GetType());
            }
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableJacocoCodeCoverageForAntIfSourceDirectoryIsNotAvailable()
        {
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            SetupMocks("");
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", "");
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntWithMoreThanOneBuildFiles()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            LoadBuildFile(CodeCoverageTestConstants.BuildWithCCJacocoXml, "buildWithCC.xml");
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: Path.Combine(_sourceDirectory, "build.xml"));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: Path.Combine(_sourceDirectory, "buildWithCC.xml"));
            VerifyJacocoReportForAnt();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void MoreThanOneBuildFilesWithAnInvalidXmlDoesnotThrow()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var invalidXml = Path.Combine(_sourceDirectory, "invalid.xml");
            File.WriteAllText(invalidXml, "invalidXmlData");
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: Path.Combine(_sourceDirectory, "build.xml"));
            VerifyJacocoReportForAnt();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void MoreThanOneBuildFilesWithAnInvalidMainBuildXmlThrowsException()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var invalidXml = Path.Combine(_sourceDirectory, "invalid.xml");
            File.WriteAllText(invalidXml, "invalidXmlData");
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", invalidXml);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);
            Assert.Throws<XmlException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void MoreThanOneBuildFilesExistInBuildSources()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var buildFile2 = Path.Combine(_sourceDirectory, "build2.xml");
            File.WriteAllText(buildFile2, CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);

            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath);
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: buildFile2);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void VerifyBuildFileOutsideSourcesShouldNotBeModified()
        {
            SetupMocks(_sourceDirectory);
            LoadBuildFile(CodeCoverageTestConstants.BuildXml);
            var buildFile2 = Path.Combine(Path.GetTempPath(), "build2.xml");
            File.WriteAllText(buildFile2, CodeCoverageTestConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoAnt();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("sourcedirectories", _srcDirectory);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ccreporttask", _cCReportTask);
            ccInputs.Add("reportbuildfile", _sampleReportBuildFilePath);

            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Ant", ccInputs));
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath);
            VerifyJacocoCoverageForAnt(numberOfTestNodes: 1, buildFilePath: buildFile2, jacocoEnabled: false);
        }

        public void Dispose()
        {
            Directory.Delete(_sourceDirectory, true);
            _sampleBuildFilePath = null;
            _sampleReportBuildFilePath = null;
        }

        private void LoadBuildFile(string buildXml, string buildFileName = "build.xml")
        {
            _sampleBuildFilePath = Path.Combine(_sourceDirectory, buildFileName);
            File.WriteAllText(_sampleBuildFilePath, buildXml);
            _sampleReportBuildFilePath = Path.Combine(_sourceDirectory, "jacocoReport.xml");
        }

        private void VerifyJacocoCoverageForAnt(int numberOfTestNodes, string buildFilePath, string includes = null, string excludes = null, bool jacocoEnabled = true)
        {
            var _includeFilter = (includes == null) ? _include : includes;
            var excludeFilter = (excludes == null) ? _exclude : excludes;

            var buildXmlDoc = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(buildFilePath))
            {
                buildXmlDoc.Load(reader);
            }
            var junitNodes = buildXmlDoc.SelectNodes("//junit");
            Assert.Equal(junitNodes.Count, numberOfTestNodes);

            if (!jacocoEnabled && numberOfTestNodes > 0)
            {
                Assert.Equal("target", junitNodes[0].ParentNode.Name);
                return;
            }

            foreach (XmlElement junitNode in junitNodes)
            {
                Assert.Equal("true", junitNode.Attributes["fork"].Value);
                var jacocoCoverage = junitNode.ParentNode;
                Assert.Equal("jacoco:coverage", jacocoCoverage.Name);

                if (!string.IsNullOrWhiteSpace(_includeFilter))
                {
                    Assert.Equal(_includeFilter, jacocoCoverage.Attributes["includes"].Value);
                }
                if (!string.IsNullOrWhiteSpace(excludeFilter))
                {
                    Assert.Equal(excludeFilter, jacocoCoverage.Attributes["excludes"].Value);
                }

                Assert.Equal(Path.Combine(_reportDirectory, "jacoco.exec"), jacocoCoverage.Attributes["destfile"].Value);
                Assert.Equal("antlib:org.jacoco.ant", jacocoCoverage.Attributes["xmlns:jacoco"].Value);
                Assert.Equal("true", jacocoCoverage.Attributes["append"].Value);
            }

            var batchTestNodes = buildXmlDoc.SelectNodes("//batchtest");
            Assert.True(batchTestNodes.Count > 0, "There should be atleast batch node under test");
            foreach (XmlElement batchTestNode in batchTestNodes)
            {
                Assert.Equal("true", batchTestNode.Attributes["fork"].Value);
            }

            // verify if report nodes are removed from build file
            var nsmgr = new XmlNamespaceManager(buildXmlDoc.NameTable);
            nsmgr.AddNamespace("jacoco", "antlib:org.jacoco.ant");
            var buildReportNodes = buildXmlDoc.SelectNodes("//jacoco:report", nsmgr);
            Assert.Equal(0, buildReportNodes.Count);
        }

        private void VerifyJacocoReportForAnt(string includes = "**/com/*/*/**,**/app/me*/*/**", string excludes = "**/me/*/*/**,**/a/b/*/**,**/my/com/*/*/**")
        {
            // verify if the report build file is populated appropriately
            var reportXmlDoc = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(_sampleReportBuildFilePath))
            {
                reportXmlDoc.Load(reader);
            }

            var reportNode = reportXmlDoc.SelectSingleNode("//target[@name='" + _cCReportTask + @"']");

            var executionNodes = reportNode.SelectNodes("//executiondata");
            Assert.Equal(1, executionNodes.Count);

            var fileNodes = executionNodes[0].SelectNodes("file");
            Assert.Equal(1, fileNodes.Count);
            Assert.Equal(Path.Combine(_reportDirectory, "jacoco.exec"), fileNodes[0].Attributes["file"].Value);

            var classFilesNodes = reportNode.SelectNodes("//structure/classfiles");
            Assert.Equal(1, classFilesNodes.Count);

            var fileSetNodes = classFilesNodes[0].SelectNodes("//fileset");
            Assert.Equal(3, fileSetNodes.Count);
            Assert.Equal("class1", fileSetNodes[0].Attributes["dir"].Value);

            if (!string.IsNullOrWhiteSpace(includes))
            {
                Assert.Equal(includes, fileSetNodes[0].Attributes["includes"].Value);
            }
            if (!string.IsNullOrWhiteSpace(excludes))
            {
                Assert.Equal(excludes, fileSetNodes[0].Attributes["excludes"].Value);
            }

            Assert.Equal("class2", fileSetNodes[1].Attributes["dir"].Value);

            if (!string.IsNullOrWhiteSpace(includes))
            {
                Assert.Equal(includes, fileSetNodes[1].Attributes["includes"].Value);
            }
            if (!string.IsNullOrWhiteSpace(excludes))
            {
                Assert.Equal(excludes, fileSetNodes[1].Attributes["excludes"].Value);
            }

            Assert.Equal("src", fileSetNodes[2].Attributes["dir"].Value);

            var htmlNodes = reportNode.SelectNodes("//html");
            Assert.Equal(1, htmlNodes.Count);
            Assert.Equal(_reportDirectory, htmlNodes[0].Attributes["destdir"].Value);

            var csvNodes = reportNode.SelectNodes("//csv");
            Assert.Equal(1, csvNodes.Count);
            Assert.Equal(Path.Combine(_reportDirectory, @"report.csv"), csvNodes[0].Attributes["destfile"].Value);

            var xmlNodes = reportNode.SelectNodes("//xml");
            Assert.Equal(1, xmlNodes.Count);
            Assert.Equal(Path.Combine(_reportDirectory, _summaryFile), xmlNodes[0].Attributes["destfile"].Value);

            var reportNsmgr = new XmlNamespaceManager(reportXmlDoc.NameTable);
            reportNsmgr.AddNamespace("jacoco", "antlib:org.jacoco.ant");
            var reportNodes = reportXmlDoc.SelectNodes("//jacoco:report", reportNsmgr);
            Assert.Equal(1, reportNodes.Count);
        }

        private void SetupMocks(string sourceDirectory, [CallerMemberName] string name = "")
        {
            _hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();

            _warnings = new List<string>();
            _errors = new List<string>();

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
