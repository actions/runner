using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageEnablerForCoberturaAntTests : IDisposable
    {
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private string _classDirectories = "class1,class2";
        private string _srcDirectory = "src";
        private string _coberturaDataFile = "cobertura.ser";
        private string _reportDirectory = Path.Combine(Path.GetTempPath(), "CodeCoverageReport");
        private string _cCReportTask = "CodeCoverageReport";
        private string _include = "com.*.*:app.me*.*";
        private string _exclude = "me.*.*:a.b.*:my.com.*.*";
        private string _coberturaClassPath = @"<fileset dir=""${env.COBERTURA_HOME}""><include name=""cobertura*.jar"" /><include name=""**/lib/**/*.jar"" /></fileset>";
        private string _sourceDirectory = Path.Combine(Path.GetTempPath(), "AntCobertura");
        private string _sampleBuildFilePath;
        private string _sampleReportBuildFilePath;
        private string _warningMessage = string.Empty;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForCoberturaTest()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false));
            VerifyCoberturaCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath);
            VerifyCoberturaReport();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForCoberturaWhenCodeCoverageIsAlreadyEnabled()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildWithCCCoberturaXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false));
            VerifyCoberturaCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath);
            VerifyCoberturaReport();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageWithMultipleTestNodesTest()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildWithMultipleNodesXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false));
            VerifyCoberturaCoverageForAnt(numberOfTestNodes: 2, buildFilePath: _sampleBuildFilePath);
            VerifyCoberturaReport();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntCoberturaThrowsIfClassDirectoriesIsInvalid()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, "clas*,classFiles", _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntCoberturaWithSingleIncludeFilter()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, "app.com.SampleTest", "", _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false));
            VerifyCoberturaCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath, includes: "**/app/com/SampleTest.class", excludes: string.Empty);
            VerifyCoberturaReport();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntCoberturaTestWithSingleExcludeFilter()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, "", "app.com.SampleTest", _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false));
            VerifyCoberturaCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath, includes: string.Empty, excludes: "**/app/com/SampleTest.class");
            VerifyCoberturaReport();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntCoberturaTestWithNoIncludeExcludeFilters()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, "", "", _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false));
            VerifyCoberturaCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath, includes: string.Empty, excludes: string.Empty);
            VerifyCoberturaReport();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntCoberturaTestWithFullClassNameFilters()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, "app.com.SampleTest:app.*.UtilTest:app2*", "app.com.SampleTest:app.*.UtilTest:app3*", _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false));
            VerifyCoberturaCoverageForAnt(numberOfTestNodes: 1, buildFilePath: _sampleBuildFilePath, includes: "**/app/com/SampleTest.class,**/app/*/UtilTest.class,**/app2*/**", excludes: "**/app/com/SampleTest.class,**/app/*/UtilTest.class,**/app3*/**");
            VerifyCoberturaReport();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithNoClassDirectory()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, null, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithNonExistingBuildFile()
        {
            SetupMocks();
            var buildFile = "invalid.xml";
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<FileNotFoundException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(buildFile, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithNoBuildFile()
        {
            SetupMocks();
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(null, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithNoReportDirectory()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, _include, _exclude, _srcDirectory, null, null, _cCReportTask, _sampleReportBuildFilePath, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithNoCCReportTask()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, null, _sampleReportBuildFilePath, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithNoCCReportBuildFile()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, null, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntThrowsExceptionWithInvalidBuildXml()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.InvalidBuildXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<XmlException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForAntDoesNotThrowExceptionWithNoTests()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildWithNoTestsXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            try
            {
                enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false));
            }
            catch (Exception ex)
            {
                Assert.True(false, string.Format("No exception was expected! The exception message: {0}", ex.Message));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForCoberturaWithMultipleBuildFiles()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            LoadBuildFile(CodeCoverageConstants.BuildWithCCCoberturaXml, "buildWithCCCobertura.xml");
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false));
            VerifyCoberturaCoverageForAnt(numberOfTestNodes: 1, buildFilePath: Path.Combine(_sourceDirectory, "build.xml"));
            VerifyCoberturaCoverageForAnt(numberOfTestNodes: 1, buildFilePath: Path.Combine(_sourceDirectory, "buildWithCCCobertura.xml"));
            VerifyCoberturaReport();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void MultipleBuildFilesWithOneOfThemBeingInvalidShouldNotThrow()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var invalidXml = Path.Combine(_sourceDirectory, "invalid.xml");
            File.WriteAllText(invalidXml, "invalidXmlData");
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false));
            VerifyCoberturaCoverageForAnt(numberOfTestNodes: 1, buildFilePath: Path.Combine(_sourceDirectory, "build.xml"));
            VerifyCoberturaReport();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void MultipleBuildFilesWithMAinBuildFileBeingInvalidShouldThrow()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.BuildXml);
            var invalidXml = Path.Combine(_sourceDirectory, "invalid.xml");
            File.WriteAllText(invalidXml, "invalidXmlData");
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaAnt();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<XmlException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(invalidXml, _classDirectories, _include, _exclude, _srcDirectory, null, _reportDirectory, _cCReportTask, _sampleReportBuildFilePath, false)));
        }

        public void Dispose()
        {
            if (Directory.Exists(_sourceDirectory))
            {
                Directory.Delete(_sourceDirectory, true);
            }
            _sampleBuildFilePath = null;
            _sampleReportBuildFilePath = null;
        }

        private void LoadBuildFile(string buildXml, string buildFileName = "build.xml")
        {
            Directory.CreateDirectory(_sourceDirectory);
            _sampleBuildFilePath = Path.Combine(_sourceDirectory, buildFileName);
            File.WriteAllText(_sampleBuildFilePath, buildXml);
            _sampleReportBuildFilePath = Path.Combine(_sourceDirectory, "coberturaReport.xml");
        }

        private void LogWarning(object sender, string e)
        {
            _warningMessage = e;
        }

        private void VerifyCoberturaCoverageForAnt(int numberOfTestNodes, string buildFilePath, string includes = "**/com/*/*/**,**/app/me*/*/**", string excludes = "**/me/*/*/**,**/a/b/*/**,**/my/com/*/*/**")
        {
            var buildXmlDoc = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(buildFilePath))
            {
                buildXmlDoc.Load(reader);
            }

            // verifying the javac nodes
            var javacNodes = buildXmlDoc.SelectNodes("//javac");
            foreach (XElement javacNode in javacNodes)
            {
                Assert.Equal("true", javacNode.Attribute("debug").Value);
            }

            //Verifying the test nodes
            var junitNodes = buildXmlDoc.SelectNodes("//junit");
            Assert.Equal(junitNodes.Count, numberOfTestNodes);

            foreach (XmlElement junitNode in junitNodes)
            {
                Assert.Equal("true", junitNode.Attributes["fork"].Value);

                var sysnodes = junitNode.SelectNodes("sysproperty");
                Assert.Equal(1, sysnodes.Count);
                Assert.Equal("net.sourceforge.cobertura.datafile", sysnodes[0].Attributes["key"].Value);
                Assert.Equal(Path.Combine(_reportDirectory, _coberturaDataFile), sysnodes[0].Attributes["file"].Value);

                var instrumentnode = junitNode.SelectSingleNode("classpath");
                Assert.Equal(Path.Combine(_sourceDirectory, "InstrumentedClasses"), instrumentnode.Attributes["location"].Value);

                var classpathNode = junitNode.LastChild;
                Assert.Equal(Agent.Worker.CodeCoverage.CodeCoverageConstants.CoberturaClassPathString, classpathNode.Attributes["refid"].Value);


                // verify if instrumentation is done correctly
                var coberturaInstrumentNode = junitNode.PreviousSibling;

                Assert.Equal("cobertura-instrument", coberturaInstrumentNode.LocalName);

                Assert.Equal(Path.Combine(_sourceDirectory, "InstrumentedClasses"), coberturaInstrumentNode.Attributes["todir"].Value);
                Assert.Equal(Path.Combine(_reportDirectory, _coberturaDataFile), coberturaInstrumentNode.Attributes["datafile"].Value);

                var fileSetNodes = coberturaInstrumentNode.SelectNodes("fileset");
                Assert.Equal(2, fileSetNodes.Count);

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
            }

            var batchTestNodes = buildXmlDoc.SelectNodes("//batchtest");
            Assert.True(batchTestNodes.Count > 0, "There should be atleast batch node under test");
            foreach (XmlElement batchTestNode in batchTestNodes)
            {
                Assert.Equal("true", batchTestNode.Attributes["fork"].Value);
            }

            //verifying the path node
            var pathNode = buildXmlDoc.SelectSingleNode("project/path[1]");
            Assert.Equal(Agent.Worker.CodeCoverage.CodeCoverageConstants.CoberturaClassPathString, pathNode.Attributes["id"].Value);
            Assert.Equal(pathNode.InnerXml, _coberturaClassPath);

            //Verifying the TaskDef node
            var taskdefNode = buildXmlDoc.SelectSingleNode("project/taskdef[1]");
            Assert.Equal(Agent.Worker.CodeCoverage.CodeCoverageConstants.CoberturaClassPathString, taskdefNode.Attributes["classpathref"].Value);
            Assert.Equal("tasks.properties", taskdefNode.Attributes["resource"].Value);

            //Verify if cobertura report nodes are removed         
            var reportNodes = buildXmlDoc.SelectNodes("//target[@name='" + _cCReportTask + @"']");
            Assert.Equal(0, reportNodes.Count);
        }

        private void VerifyCoberturaReport()
        {
            // verify if the report build file is populated appropriately
            var reportXmlDoc = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(_sampleReportBuildFilePath))
            {
                reportXmlDoc.Load(reader);
            }

            var reportNode = reportXmlDoc.SelectSingleNode("//target[@name='" + _cCReportTask + @"']");
            var coberturaReportNode = reportNode.FirstChild;
            Assert.Equal("html", coberturaReportNode.Attributes["format"].Value);
            Assert.Equal(_reportDirectory, coberturaReportNode.Attributes["destdir"].Value);
            Assert.Equal(Path.Combine(_reportDirectory, _coberturaDataFile), coberturaReportNode.Attributes["datafile"].Value);
            Assert.Equal(_srcDirectory, coberturaReportNode.Attributes["srcdir"].Value);
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            _hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();

            _warnings = new List<string>();
            _errors = new List<string>();

            List<string> warnings;
            var variables = new Variables(_hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
            variables.Set("build.sourcesdirectory", _sourceDirectory);
            _ec.Setup(x => x.Variables).Returns(variables);

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
