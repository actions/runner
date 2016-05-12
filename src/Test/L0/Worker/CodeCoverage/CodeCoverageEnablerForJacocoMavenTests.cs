using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageEnablerForJacocoMavenTests : IDisposable
    {
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private string _summaryFile = "summary.xml";
        private string _reportDirectory = "codeCoverage";
        private string _include = "com.*.*:app.me*.*";
        private string _exclude = "me.*.*:a.b.*:my.com.*.*";
        private string _sampleBuildFilePath;
        private string _reportBuildFilePath;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_EnableCodeCoverageForJacocoTest()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false));
            VerifyJacocoCoverageForMaven(checkForPluginManagement: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Jacoco_EnableCodeCoverageForCodeSearchPlugin()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.CodeSearchPomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false));
            VerifyJacocoCoverageForMaven(checkForPluginManagement: false);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Jacoco_EnableCodeCoverageForLog4JAppender()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.LogAppenderPomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false));
            VerifyJacocoCoverageForMaven(checkForPluginManagement: false);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_EnableCodeCoverageForJacocoWhenCodeCoverageIsAlreadyEnabled()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomWithJacocoCCXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false));
            VerifyJacocoCoverageForMaven(numberOfExecutionElements: 4, numberOfExecutionConfigurations: 3, verifyVersion: false, checkForPluginManagement: true, numOfJacocoPluginsInPluginManagement: 1);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_JaCoCo_EnableCodeCoverageForMultiModulePom()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomWithMultiModuleXml);
            _reportBuildFilePath = Path.Combine(Path.GetTempPath(), "MultiModuleReport.xml");
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, _summaryFile, _reportDirectory, null, _reportBuildFilePath, false));
            VerifyJacocoCoverageForMaven(numberOfExecutionElements: 1, numberOfExecutionConfigurations: 1, verifyVersion: false);
            VerifyMultiModuleReports();
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_JaCoCo_EnableCodeCoverageForMultiModulePomWithCCEnabled()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomWithMultiModuleWithCCJacocoXml);
            _reportBuildFilePath = Path.Combine(Path.GetTempPath(), "MultiModuleReport.xml");
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, _summaryFile, _reportDirectory, null, _reportBuildFilePath, false));
            VerifyJacocoCoverageForMaven(numberOfExecutionElements: 3, numberOfExecutionConfigurations: 3, verifyVersion: false);
            VerifyMultiModuleReports(expectedModelVersion: "4.0.0", expectedGroupId: "reports", expectedArtifactId: "report", expectedVersion: "1.0");
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_EnableCodeCoverageForJacocoWhenCodeCoverageIsIncorrectlyEnabled()
        {
            SetupMocks();
            // missing artifactId tag
            LoadBuildFile(CodeCoverageConstants.PomWithInvalidCCXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false));
            VerifyJacocoCoverageForMaven(numberOfExecutionElements: 4, verifyVersion: false);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForMavenJacocoThrowsIfClassDirectoriesIsInvalid()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, "Clas1/class2*", _include, _exclude, string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForMavenJacocoThrowsWithNoSummaryFile()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, null, _reportDirectory, null, string.Empty, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void EnableCodeCoverageForMavenJacocoThrowsWithNoReportDirectory()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, _summaryFile, null, null, string.Empty, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_JaCoCo_EnableCodeCoverageForEmptyBuildFile()
        {
            SetupMocks();
            _sampleBuildFilePath = Path.Combine(Path.GetTempPath(), "pomEmpty.xml");
            File.WriteAllText(_sampleBuildFilePath, string.Empty);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<XmlException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_JaCoCo_EnableCodeCoverageForInvalidBuildFile()
        {
            SetupMocks();
            _sampleBuildFilePath = Path.Combine(Path.GetTempPath(), "pomInvalid.xml");
            File.WriteAllText(_sampleBuildFilePath, @"This is not valid xml file contents");
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            Assert.Throws<XmlException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, _include, _exclude, string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_JaCoCo_EnableCodeCoverageWithSingleIncludeFilter()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, "app.com.SampleTest", "", string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false));
            VerifyJacocoCoverageForMaven(includes: "**/app/com/SampleTest.class", excludes: string.Empty, numOfIncludes: 1, numOfExcludes: 0, checkForPluginManagement: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_JaCoCo_EnableCodeCoverageWithSingleExcludeFilter()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, "", "app.com.SampleTest", string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false));
            VerifyJacocoCoverageForMaven(includes: string.Empty, excludes: "**/app/com/SampleTest.class", numOfIncludes: 0, numOfExcludes: 1, checkForPluginManagement: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_JaCoCo_EnableCodeCoverageWithNoIncludeExcludeFilters()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, "", "", string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false));
            VerifyJacocoCoverageForMaven(includes: string.Empty, excludes: string.Empty, numOfIncludes: 0, numOfExcludes: 0, checkForPluginManagement: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_JaCoCo_EnableCodeCoverageWithFullClassNameFilters()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoMaven();
            enableCodeCoverage.Initialize(_hc);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_sampleBuildFilePath, string.Empty, "app.com.SampleTest:app.*.UtilTest:app2*", "app.com.SampleTest:app.*.UtilTest:app3*", string.Empty, _summaryFile, _reportDirectory, null, string.Empty, false));
            VerifyJacocoCoverageForMaven(includes: "**/app/com/SampleTest.class,**/app/*/UtilTest.class,**/app2*/**", excludes: "**/app/com/SampleTest.class,**/app/*/UtilTest.class,**/app3*/**", numOfIncludes: 3, numOfExcludes: 3, checkForPluginManagement: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        public void Dispose()
        {
            File.Delete(_sampleBuildFilePath);
            if (!string.IsNullOrWhiteSpace(_reportBuildFilePath))
            {
                File.Delete(_reportBuildFilePath);
            }
            _sampleBuildFilePath = null;
            _reportBuildFilePath = null;
        }

        private void LoadBuildFile(string pomXml)
        {
            _sampleBuildFilePath = Path.Combine(Path.GetTempPath(), "pomJacoco.xml");
            File.WriteAllText(_sampleBuildFilePath, pomXml);
        }

        private void VerifyMultiModuleReports(string expectedModelVersion = "4.0.0", string expectedGroupId = "com.mycompany.app", string expectedArtifactId = "my-app", string expectedVersion = "1.0-SNAPSHOT")
        {
            var jacocoPluginCount = 0;
            var xelement = XElement.Load(_reportBuildFilePath);
            XNamespace xNameSpace = xelement.Attribute("xmlns").Value;

            var modelVersion = xelement.Element(xNameSpace + "modelVersion");
            Assert.Equal(expectedModelVersion, modelVersion.Value);

            var projectGroupId = xelement.Element(xNameSpace + "groupId");
            Assert.Equal(expectedGroupId, projectGroupId.Value);

            var projectAtifactId = xelement.Element(xNameSpace + "artifactId");
            Assert.Equal(expectedArtifactId, projectAtifactId.Value);

            var projectVersion = xelement.Element(xNameSpace + "version");
            Assert.Equal(expectedVersion, projectVersion.Value);

            var packaging = xelement.Element(xNameSpace + "packaging");
            Assert.Equal("pom", packaging.Value);

            var build = xelement.Element(xNameSpace + "build");
            var plugins = build.Element(xNameSpace + "plugins");
            IList<XElement> pluginList = plugins.Elements(xNameSpace + "plugin").ToList();

            foreach (var plugin in pluginList.Where(plugin =>
            {
                var groupId = plugin.Element(xNameSpace + "groupId");
                var artifactId = plugin.Element(xNameSpace + "artifactId");
                return ((artifactId != null && artifactId.Value == "maven-antrun-plugin")
                    || (groupId != null && groupId.Value == "org.apache.maven.plugins"));
            }))
            {
                Assert.False(jacocoPluginCount > 0, "Jacoco plugin should not be included more than once.");
                jacocoPluginCount++;

                var groupId = plugin.Element(xNameSpace + "groupId");
                Assert.Equal("org.apache.maven.plugins", groupId.Value);
                var artifactId = plugin.Element(xNameSpace + "artifactId");
                Assert.Equal("maven-antrun-plugin", artifactId.Value);
                var version = plugin.Element(xNameSpace + "version");
                Assert.Equal("1.8", version.Value);

                var executionsElements = plugin.Elements(xNameSpace + "executions").ToList();
                Assert.Equal(1, executionsElements.Count);

                var executionElements = executionsElements[0].Elements(xNameSpace + "execution").ToList();
                Assert.Equal(1, executionElements.Count);

                var goalsElements = executionElements[0].Elements(xNameSpace + "goals").ToList();
                Assert.Equal(1, goalsElements.Count);

                var goalElements = goalsElements[0].Elements(xNameSpace + "goal").ToList();
                Assert.Equal(1, goalElements.Count);
                Assert.Equal("run", goalElements[0].Value);

                var configurationElements = executionElements[0].Elements(xNameSpace + "configuration").ToList();
                Assert.Equal(1, configurationElements.Count);

                var targetElements = configurationElements[0].Elements(xNameSpace + "target").ToList();
                Assert.Equal(1, targetElements.Count);

                var taskdefElements = targetElements[0].Elements(xNameSpace + "taskdef").ToList();
                Assert.Equal(1, taskdefElements.Count);
                Assert.Equal("report", taskdefElements[0].Attribute("name").Value);
                Assert.Equal("org.jacoco.ant.ReportTask", taskdefElements[0].Attribute("classname").Value);
            }
        }

        // By default number of configurations are 2(report and prepare agent)
        private void VerifyJacocoCoverageForMaven(string includes = "**/com/*/*/**,**/app/me*/*/**", string excludes = "**/me/*/*/**,**/a/b/*/**,**/my/com/*/*/**", int numberOfExecutionElements = 2, int numberOfExecutionConfigurations = 1, bool verifyVersion = true, int numOfIncludes = 2, int numOfExcludes = 3, bool checkForPluginManagement = false, int numOfJacocoPluginsInPluginManagement = 0)
        {
            try
            {
                var xelement = XElement.Load(_sampleBuildFilePath);
                XNamespace xNameSpace = xelement.Attribute("xmlns").Value;

                var build = xelement.Element(xNameSpace + "build");

                if (checkForPluginManagement)
                {
                    var pluginManagements = build.Element(xNameSpace + "pluginManagement");
                    var pluginList = pluginManagements.Element(xNameSpace + "plugins");
                    VerifyJacocoPlugin(pluginList, xNameSpace, includes, excludes, numberOfExecutionElements, numberOfExecutionConfigurations, verifyVersion, numOfIncludes, numOfExcludes, numOfJacocoPluginsInPluginManagement);
                }

                var plugins = build.Element(xNameSpace + "plugins");
                VerifyJacocoPlugin(plugins, xNameSpace, includes, excludes, numberOfExecutionElements, numberOfExecutionConfigurations, verifyVersion, numOfIncludes, numOfExcludes, 1);
            }
            catch (XmlException)
            {
                Assert.True(false, "Xml processing should not have thrown exception.");
            }
        }

        private void VerifyJacocoPlugin(XElement plugins, XNamespace xNameSpace, string includes, string excludes, int numberOfExecutionElements, int numberOfExecutionConfigurations, bool verifyVersion, int numOfIncludes, int numOfExcludes, int numOfJacocoPlugins)
        {
            var jacocoPluginCount = 0;
            IList<XElement> pluginList = plugins.Elements(xNameSpace + "plugin").ToList();

            foreach (var plugin in pluginList.Where(plugin =>
            {
                var groupId = plugin.Element(xNameSpace + "groupId");
                var artifactId = plugin.Element(xNameSpace + "artifactId");
                return ((artifactId != null && artifactId.Value == "jacoco-maven-plugin")
                    || (groupId != null && groupId.Value == "org.jacoco"));
            }))
            {
                Assert.False(jacocoPluginCount > 0, "Jacoco plugin should not be included more than once.");
                jacocoPluginCount++;

                var groupId = plugin.Element(xNameSpace + "groupId");
                Assert.Equal("org.jacoco", groupId.Value);
                var artifactId = plugin.Element(xNameSpace + "artifactId");
                Assert.Equal("jacoco-maven-plugin", artifactId.Value);
                if (verifyVersion)
                {
                    var version = plugin.Element(xNameSpace + "version");
                    Assert.Equal("0.7.5.201505241946", version.Value);
                }

                // verify root configuration
                var rootConfigurations = plugin.Elements(xNameSpace + "configuration").ToList();
                Assert.Equal(rootConfigurations.Count, 1);
                Assert.Equal(Path.Combine(_reportDirectory, "jacoco.exec"), rootConfigurations[0].Element(xNameSpace + "destFile").Value);
                Assert.Equal(_reportDirectory, rootConfigurations[0].Element(xNameSpace + "outputDirectory").Value);
                Assert.Equal(Path.Combine(_reportDirectory, "jacoco.exec"), rootConfigurations[0].Element(xNameSpace + "dataFile").Value);
                Assert.NotNull(rootConfigurations[0].Element(xNameSpace + "excludes"));
                Assert.NotNull(rootConfigurations[0].Element(xNameSpace + "includes"));
                Assert.Equal(numOfExcludes, rootConfigurations[0].Element(xNameSpace + "excludes").Elements(xNameSpace + "exclude").Count());
                Assert.Equal(numOfIncludes, rootConfigurations[0].Element(xNameSpace + "includes").Elements(xNameSpace + "include").Count());

                string[] includeDirs = includes.Split(',');
                string[] excludeDirs = excludes.Split(',');

                var excludeElements = rootConfigurations[0].Element(xNameSpace + "excludes").Elements(xNameSpace + "exclude");
                var includeElements = rootConfigurations[0].Element(xNameSpace + "includes").Elements(xNameSpace + "include");

                var excludeElementsList = excludeElements.ToList();
                var includeElementsList = includeElements.ToList();

                for (int index = 0; index < excludeElements.Count(); index++)
                {
                    Assert.Equal(excludeElementsList[index].Value, excludeDirs[index]);
                }

                for (int index = 0; index < includeElements.Count(); index++)
                {
                    Assert.Equal(includeElementsList[index].Value, includeDirs[index]);
                }

                // verify the executions
                var executionsElements = plugin.Elements(xNameSpace + "executions").ToList();
                Assert.Equal(1, executionsElements.Count);
                var executionElements = executionsElements[0].Elements(xNameSpace + "execution").ToList();
                Assert.Equal(numberOfExecutionElements, executionElements.Count);

                int numberOfConfigurations = 0;
                // verify the configurations under executions
                foreach (var executionElement in executionElements)
                {
                    var configurations = executionElement.Elements(xNameSpace + "configuration").ToList();
                    numberOfConfigurations += configurations.Count;

                    VerifyPrepareAgentHack(executionElement, configurations, xNameSpace);
                }

                Assert.Equal(numberOfExecutionConfigurations, numberOfConfigurations);
            }

            Assert.Equal(numOfJacocoPlugins, jacocoPluginCount);
        }

        private void VerifyPrepareAgentHack(XElement executionElement, List<XElement> configurations, XNamespace xNameSpace)
        {
            var isPrepareAgentExecution = false;
            var goalsElement = executionElement.Element(xNameSpace + "goals");
            if (goalsElement != null)
            {
                var goalElement = goalsElement.Element(xNameSpace + "goal");
                if (goalElement != null)
                {
                    if (goalElement.Value.Equals("prepare-agent", StringComparison.OrdinalIgnoreCase))
                    {
                        isPrepareAgentExecution = true;
                    }
                    if (isPrepareAgentExecution)
                    {
                        foreach (var configuration in configurations)
                        {
                            var includesElement = configuration.Element(xNameSpace + "includes");
                            Assert.NotNull(includesElement);
                            var includeElement = includesElement.Element(xNameSpace + "include");
                            Assert.NotNull(includeElement);
                            Assert.Equal("**/*", includeElement.Value);
                        }
                    }
                }
            }
        }

        private void SetupMocks([CallerMemberName] string name = "")
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
