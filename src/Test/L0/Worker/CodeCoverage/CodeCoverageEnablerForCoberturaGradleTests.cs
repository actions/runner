using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageEnablerForCoberturaGradleTests : IDisposable
    {
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private string _classDirectories = "build/classes/classdir1,build/classes/classdir2";
        private string _classDir = "'build/classes/classdir1','build/classes/classdir2'";
        private string _summaryFile = "coverage.xml";
        private string _reportDirectory = "codeCoverage";
        private string _includePackages = "'.*com.*..*','.*app.me*..*'";
        private string _excludePackages = "'.*me.*..*','.*a.b..*','.*my.com.test'";
        private string _classFilter = "+:com.*.*,+:app.me*.*,-:me.*.*,-:a.b.*,-:my.com.test";
        private string _sampleBuildFilePath;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_SingleModule_EnableCodeCoverageForCoberturaTest()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs));
            VerifyCoberturaCoverageForGradle(true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_MultiModule_EnableCodeCoverageForCoberturaTest()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildMultiModuleGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "true");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs));
            VerifyCoberturaCoverageForGradle(false);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_SingleModule_EnableCodeCoverageForCoberturaWhenCodeCoverageIsAlreadyEnabled()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildWithCCCoberturaGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "false");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs));
            VerifyCoberturaCoverageForGradle(true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_MultiModule_EnableCodeCoverageForCoberturaWhenCodeCoverageIsAlreadyEnabled()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildWithCCMultiModuleGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "true");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs));
            VerifyCoberturaCoverageForGradle(false);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_EnableCodeCoverageForNotExistingBuildFile()
        {
            SetupMocks();
            _sampleBuildFilePath = Path.GetTempFileName();
            _sampleBuildFilePath = Path.ChangeExtension(_sampleBuildFilePath, ".gradle");
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "false");
            Assert.Throws<FileNotFoundException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_EnableCodeCoverageForEmptyBuildFile()
        {
            SetupMocks();
            _sampleBuildFilePath = Path.Combine(Path.GetTempPath(), "build.gradle");
            File.WriteAllText(_sampleBuildFilePath, string.Empty);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "false");
            Assert.Throws<InvalidOperationException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_EnableCodeCoverageWithNoClassDirectory()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "false");
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_EnableCodeCoverageWithNoReportDirectory()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForCoberturaGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("ismultimodule", "false");
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs)));
        }

        public void Dispose()
        {
            File.Delete(_sampleBuildFilePath);
            _sampleBuildFilePath = null;
        }

        private void LoadBuildFile(string gradleBuild)
        {
            _sampleBuildFilePath = Path.Combine(Path.GetTempPath(), "build.gradle");
            File.WriteAllText(_sampleBuildFilePath, gradleBuild);
        }

        private void VerifyCoberturaCoverageForGradle(bool isSingleModule)
        {
            Assert.True(File.ReadAllText(_sampleBuildFilePath).Contains(GetCoberturaPluginDefination()), "Cobertura plugin not added correctly");
            Assert.True(File.ReadAllText(_sampleBuildFilePath).Contains(GetCoberturaEnablingScriptForGradle()), "Cobertura enabling script not added correctly");
            Assert.True(
                isSingleModule
                    ? File.ReadAllText(_sampleBuildFilePath).Contains(GetCoberturaReportingScriptForSingleModuleGradle())
                    : File.ReadAllText(_sampleBuildFilePath).Contains(GetCoberturaReportingScriptForMultiModuleGradle()),
                "Cobertura reporting script not added correctly");
        }

        private string GetCoberturaPluginDefination()
        {
            return
                @"buildscript {
                    repositories {
                        mavenCentral()
                    }
                    dependencies {
                        classpath 'net.saliman:gradle-cobertura-plugin:2.2.7'
                    }
                }";
        }

        private string GetCoberturaEnablingScriptForGradle()
        {
            return
                @"
                allprojects {
                    repositories {
                        mavenCentral()
                    }
                    apply plugin: 'net.saliman.cobertura'
	
                    dependencies {
	                    testCompile 'org.slf4j:slf4j-api:1.7.12'
                    }

                    cobertura.coverageIncludes = [" + _includePackages + @"]
                    cobertura.coverageExcludes = [" + _excludePackages + @"]
                }";
        }

        private string GetCoberturaReportingScriptForSingleModuleGradle()
        {
            return
                @"
                cobertura {
                    coverageDirs = [" + (string.IsNullOrWhiteSpace(_classDir) ? @"${project.sourceSets.main.output.classesDir}" : _classDir) + @"]
                    coverageSourceDirs = project.sourceSets.main.java.srcDirs
                    coverageReportDir = new File('" + _reportDirectory + @"')
                    coverageFormats = ['xml', 'html']
                }";
        }

        private string GetCoberturaReportingScriptForMultiModuleGradle()
        {
            return
                @"test {
                    dependsOn = subprojects.test
                }

                cobertura {	
                    coverageSourceDirs = []
                    coverageDirs = [""" + (string.IsNullOrWhiteSpace(_classDir) ? @"${it.sourceSets.main.output.classesDir}" : _classDir) + @"""]
                    rootProject.subprojects.each {
	                    coverageSourceDirs += it.sourceSets.main.java.srcDirs
                    }
                        coverageFormats = [ 'xml', 'html' ]
                        coverageMergeDatafiles = subprojects.collect { new File(it.projectDir, '/build/cobertura/cobertura.ser') }
                        coverageReportDir = new File('" + _reportDirectory + @"')
                    }";
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
