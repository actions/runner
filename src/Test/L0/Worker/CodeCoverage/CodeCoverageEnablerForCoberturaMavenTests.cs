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
    public class CodeCoverageEnablerForCoberturaMavenTests : IDisposable
    {
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private string _classFilter = "+:com.*.*,+:app.me*.*,+:com.app.class2,-:me.*.*,-:a.b.*,-:my.com.*.*";
        private string _sampleBuildFilePath;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_EnableCodeCoverageForCoberturaTest()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", _classFilter);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs));
            VerifyCoberturaCoverageForMaven(checkForPluginManagement: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Cobertura_EnableCodeCoverageTestForCodeSearchPlugin()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.CodeSearchPomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", _classFilter);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs));
            VerifyCoberturaCoverageForMaven(checkForPluginManagement: false);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Cobertura_EnableCodeCoverageTestForLog4JAppender()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.LogAppenderPomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", _classFilter);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs));
            VerifyCoberturaCoverageForMaven(checkForPluginManagement: false);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_EnableCodeCoverageForCoberturaWhenCodeCoverageIsAlreadyEnabled()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.PomWithCCCoberturaXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", _classFilter);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs));
            VerifyCoberturaCoverageForMaven(verifyVersion: false, verifyUserTag: true, checkForPluginManagement: true, numOfCoberturaPluginsInPluginManagement: 1);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Cobertura_EnableCodeCoverageForEmptyBuildFile()
        {
            SetupMocks();
            _sampleBuildFilePath = Path.Combine(Path.GetTempPath(), "pom.xml");
            File.WriteAllText(_sampleBuildFilePath, string.Empty);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", _classFilter);
            Assert.Throws<XmlException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Cobertura_EnableCodeCoverageForInvalidBuildFile()
        {
            SetupMocks();
            _sampleBuildFilePath = Path.Combine(Path.GetTempPath(), "pom.xml");
            File.WriteAllText(_sampleBuildFilePath, @"This is not valid xml file contents");
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", _classFilter);
            Assert.Throws<XmlException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Cobertura_EnableCodeCoverageForMultiModulePom()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.PomWithMultiModuleXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", _classFilter);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs));
            VerifyCoberturaCoverageForMaven(verifyVersion: false, isMultiModule: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Cobertura_EnableCodeCoverageForMultiModulePomWithCCEnabled()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.PomWithMultiModuleWithCCCoberturaXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", _classFilter);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs));
            VerifyCoberturaCoverageForMaven(verifyVersion: false, isMultiModule: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Cobertura_EnableCodeCoverageWithSingleIncludeFilter()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", "+:app.com.SampleTest");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs));
            VerifyCoberturaCoverageForMaven(includes: "app/com/SampleTest.class", excludes: string.Empty, verifyVersion: false, numOfInclude: 1, numOfExclude: 0, checkForPluginManagement: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Cobertura_EnableCodeCoverageWithSingleExcludeFilter()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", "-:app.com.SampleTest");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs));
            VerifyCoberturaCoverageForMaven(includes: string.Empty, excludes: "app/com/SampleTest.class", verifyVersion: false, numOfInclude: 0, numOfExclude: 1, checkForPluginManagement: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Cobertura_EnableCodeCoverageWithNoIncludeExcludeFilters()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs));
            VerifyCoberturaCoverageForMaven(includes: string.Empty, excludes: string.Empty, verifyVersion: false, numOfInclude: 0, numOfExclude: 0, checkForPluginManagement: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Maven_Cobertura_EnableCodeCoverageWithFullClassNameFilters()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.PomXml);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaMaven();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", "+:app.com.SampleTest,+:app.*.UtilTest,+:app2*,-:app.com.SampleTest,-:app.*.UtilTest,-:app3*");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Maven", ccInputs));
            VerifyCoberturaCoverageForMaven(includes: "app/com/SampleTest.class,app/*/UtilTest.class,app2*/**", excludes: "app/com/SampleTest.class,app/*/UtilTest.class,app3*/**", verifyVersion: false, checkForPluginManagement: true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        public void Dispose()
        {
            File.Delete(_sampleBuildFilePath);
            _sampleBuildFilePath = null;
        }

        private void LoadBuildFile(string pomXml)
        {
            _sampleBuildFilePath = Path.Combine(Path.GetTempPath(), "pom.xml");
            File.WriteAllText(_sampleBuildFilePath, pomXml);
        }

        private void VerifyCoberturaCoverageForMaven(string includes = "com/*/*/**,app/me*/*/**,com/app/class2.class", string excludes = "me/*/*/**,a/b/*/**,my/com/*/*/**", bool verifyVersion = true, bool isMultiModule = false, bool verifyUserTag = false, int numOfInclude = 3, int numOfExclude = 3, bool checkForPluginManagement = false, int numOfCoberturaPluginsInPluginManagement = 0)
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
                    VerifyPlugins(xNameSpace, pluginList, includes, excludes, verifyVersion, isMultiModule, verifyUserTag, numOfInclude, numOfExclude, numOfCoberturaPluginsInPluginManagement);
                }

                var plugins = build.Element(xNameSpace + "plugins");
                VerifyPlugins(xNameSpace, plugins, includes, excludes, verifyVersion, isMultiModule, verifyUserTag, numOfInclude, numOfExclude, 1);
            }
            catch (XmlException ex)
            {
                Assert.True(false, "Xml processing should not have thrown exception. error:" + ex.Message);
            }
        }

        private void VerifyPlugins(XNamespace xNameSpace, XElement plugins, string includes, string excludes, bool verifyVersion, bool isMultiModule, bool verifyUserTag, int numOfInclude, int numOfExclude, int numOfCoberturaPlugins)
        {
            var coberturaPluginCount = 0;
            IList<XElement> pluginList = plugins.Elements(xNameSpace + "plugin").ToList();
            foreach (var plugin in pluginList.Where(plugin =>
            {
                var groupId = plugin.Element(xNameSpace + "groupId");
                var artifactId = plugin.Element(xNameSpace + "artifactId");
                return ((artifactId != null && artifactId.Value == "cobertura-maven-plugin")
                    || (groupId != null && groupId.Value == "org.codehaus.mojo"));
            }))
            {
                Assert.False(coberturaPluginCount > 0, "Cobertura plugin should not be included more than once.");
                coberturaPluginCount++;

                var groupId = plugin.Element(xNameSpace + "groupId");
                Assert.Equal("org.codehaus.mojo", groupId.Value);
                var artifactId = plugin.Element(xNameSpace + "artifactId");
                Assert.Equal("cobertura-maven-plugin", artifactId.Value);
                if (verifyVersion)
                {
                    var version = plugin.Element(xNameSpace + "version");
                    Assert.Equal("2.7", version.Value);
                }

                // verify root configuration
                var rootConfigurations = plugin.Elements(xNameSpace + "configuration").ToList();
                Assert.Equal(rootConfigurations.Count, 1);
                Assert.NotNull(rootConfigurations[0].Element(xNameSpace + "formats"));
                Assert.Equal(2, rootConfigurations[0].Element(xNameSpace + "formats").Elements(xNameSpace + "format").Count());

                Assert.NotNull(rootConfigurations[0].Element(xNameSpace + "instrumentation"));
                var instrumentationTag = rootConfigurations[0].Element(xNameSpace + "instrumentation");
                Assert.Equal(numOfExclude, instrumentationTag.Element(xNameSpace + "excludes").Elements(xNameSpace + "exclude").Count());
                Assert.Equal(numOfInclude, instrumentationTag.Element(xNameSpace + "includes").Elements(xNameSpace + "include").Count());

                string[] includeDirs = includes.Split(',');
                string[] excludeDirs = excludes.Split(',');

                var excludeElements = instrumentationTag.Element(xNameSpace + "excludes").Elements(xNameSpace + "exclude");
                var includeElements = instrumentationTag.Element(xNameSpace + "includes").Elements(xNameSpace + "include");

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

                if (verifyUserTag)
                {
                    Assert.NotNull(rootConfigurations[0].Element(xNameSpace + "RandomTag"));
                    Assert.Equal(1, rootConfigurations[0].Elements(xNameSpace + "RandomTag").Count());
                }

                if (isMultiModule)
                {
                    Assert.NotNull(rootConfigurations[0].Element(xNameSpace + "aggregate"));
                    Assert.Equal(1, rootConfigurations[0].Elements(xNameSpace + "aggregate").Count());
                }

                // verify the executions
                var executionsElements = plugin.Elements(xNameSpace + "executions").ToList();
                Assert.Equal(1, executionsElements.Count);
                var executionElements = executionsElements[0].Elements(xNameSpace + "execution").ToList();
                Assert.Equal(1, executionElements.Count);
                Assert.Equal("cobertura", executionsElements[0].Element(xNameSpace + "execution").Element(xNameSpace + "goals").Element(xNameSpace + "goal").Value);
                Assert.Equal("package", executionsElements[0].Element(xNameSpace + "execution").Element(xNameSpace + "phase").Value);
                Assert.Equal(numOfCoberturaPlugins, coberturaPluginCount);
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
