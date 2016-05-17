using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CodeCoverageEnablerForJacocoGradleTests : IDisposable
    {
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private string _classDirectories = "class1,class2";
        private string _summaryFile = "summary.xml";
        private string _reportDirectory = "codeCoverage";
        private string _classFilter = "+:com.*.*,+:app.me*.*,-:me.*.*,-:a.b.*,-:my.com.*.Test";
        private string _includePackages = "'com/*/*/**','app/me*/*/**'";
        private string _excludePackages = "'me/*/*/**','a/b/*/**','my/com/*/Test.class'";
        private string _sampleBuildFilePath;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_SingleModule_EnableCodeCoverageForJacocoTest()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "false");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs));
            VerifyJacocoCoverageForGradle(true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_SingleModule_EnableCodeCoverageForJacocoWhenCodeCoverageIsAlreadyEnabled()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildWithCCJacocoGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "false");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs));
            VerifyJacocoCoverageForGradle(true);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_MultiModule_EnableCodeCoverageForJacocoTest()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildMultiModuleGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "true");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs));
            VerifyJacocoCoverageForGradle(false);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_MultiModule_EnableCodeCoverageForJacocoWhenCodeCoverageIsAlreadyEnabled()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildWithCCMultiModuleGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "true");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs));
            VerifyJacocoCoverageForGradle(false);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_MultiModule_EnableJacocoCodeCoverageForSingleModule()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildMultiModuleGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "false");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs));
            VerifyFailureJacocoCoverageForGradle(false);
            Assert.Equal(_warnings.Count, 0);
            Assert.Equal(_errors.Count, 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_SingleModule_EnableJacocoCodeCoverageFoMultiModule()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "true");
            enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs));
            VerifyFailureJacocoCoverageForGradle(true);
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
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
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
            _sampleBuildFilePath = Path.Combine(Path.GetTempPath(), "buildEmpty.gradle");
            File.WriteAllText(_sampleBuildFilePath, string.Empty);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
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
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
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
        public void Gradle_EnableCodeCoverageWithNoSummaryFile()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("reportdirectory", _reportDirectory);
            ccInputs.Add("ismultimodule", "true");
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs)));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnableCodeCoverage")]
        public void Gradle_EnableCodeCoverageWithNoReportDirectory()
        {
            SetupMocks();
            LoadBuildFile(CodeCoverageTestConstants.BuildGradle);
            var enableCodeCoverage = new CodeCoverageEnablerForJacocoGradle();
            enableCodeCoverage.Initialize(_hc);
            var ccInputs = new Dictionary<string, string>();
            ccInputs.Add("buildfile", _sampleBuildFilePath);
            ccInputs.Add("classfilesdirectories", _classDirectories);
            ccInputs.Add("classfilter", _classFilter);
            ccInputs.Add("summaryfile", _summaryFile);
            ccInputs.Add("ismultimodule", "true");
            Assert.Throws<ArgumentException>(() => enableCodeCoverage.EnableCodeCoverage(_ec.Object, new CodeCoverageEnablerInputs(_ec.Object, "Gradle", ccInputs)));
        }
        public void Dispose()
        {
            File.Delete(_sampleBuildFilePath);
            _sampleBuildFilePath = null;
        }

        private void LoadBuildFile(string gradleBuild)
        {
            _sampleBuildFilePath = Path.Combine(Path.GetTempPath(), "buildJacoco.gradle");
            File.WriteAllText(_sampleBuildFilePath, gradleBuild);
        }

        private void VerifyJacocoCoverageForGradle(bool isSingleModule)
        {
            Assert.True(IsContainsIgnoreSpace(File.ReadAllText(_sampleBuildFilePath), ("apply plugin: 'jacoco'")), "Jacoco plugin not added correctly");
            Assert.True(IsContainsIgnoreSpace(File.ReadAllText(_sampleBuildFilePath), GetJacocoEnablingScriptForGradle()), "Jacoco enabling script not added correctly");
            string test = File.ReadAllText(_sampleBuildFilePath);
            string t = GetJacocoReportingScriptForSingleModuleGradle();
            Assert.True(IsContainsIgnoreSpace(File.ReadAllText(_sampleBuildFilePath), (isSingleModule ? GetJacocoReportingScriptForSingleModuleGradle() : GetJacocoReportingScriptForMultiModuleGradle())), "Jacoco reporting script not added correctly");
        }

        private void VerifyFailureJacocoCoverageForGradle(bool isSingleModule)
        {
            Assert.True(IsContainsIgnoreSpace(File.ReadAllText(_sampleBuildFilePath), ("apply plugin: 'jacoco'")), "Jacoco plugin not added correctly");
            Assert.True(IsContainsIgnoreSpace(File.ReadAllText(_sampleBuildFilePath), GetJacocoEnablingScriptForGradle()), "Jacoco enabling script not added correctly");
            Assert.False(IsContainsIgnoreSpace(File.ReadAllText(_sampleBuildFilePath), (isSingleModule ? GetJacocoReportingScriptForSingleModuleGradle() : GetJacocoReportingScriptForMultiModuleGradle())), "Jacoco reporting script not added correctly");
        }

        private string GetJacocoEnablingScriptForGradle()
        {
            return
                @"def jacocoExcludes = [" + _excludePackages + @"]
                  def jacocoIncludes = [" + _includePackages + @"]";
        }

        private string GetJacocoReportingScriptForSingleModuleGradle()
        {
            return
                @"
                jacocoTestReport {
	                doFirst {
		                classDirectories = fileTree(dir: """ + _classDirectories + @""").exclude(jacocoExcludes).include(jacocoIncludes)
	                }
		
	                reports {
	                    html.enabled = true
                        xml.enabled = true    
	                    xml.destination """ + _reportDirectory + "/" + "summary.xml" + @"""
	                    html.destination """ + _reportDirectory + @"""
                    }
                }
	
                test {
                    finalizedBy jacocoTestReport
	                jacoco {
		                append = true
		                destinationFile = file(""" + _reportDirectory + "/" + "jacoco.exec\"" + @")
	                }
                }
                ";
        }

        private string GetJacocoReportingScriptForMultiModuleGradle()
        {
            return
                @"
                task jacocoRootReport(type: org.gradle.testing.jacoco.tasks.JacocoReport) {
	                dependsOn = subprojects.test
	                executionData = files(subprojects.jacocoTestReport.executionData)
	                sourceDirectories = files(subprojects.sourceSets.main.allSource.srcDirs)
	                classDirectories = files()
	
	                doFirst {
		                subprojects.each {
			                if (new File(""${it.sourceSets.main.output.classesDir}"").exists()) {
				                logger.info(""Class directory exists in sub project: ${it.name}"")
				                logger.info(""Adding class files ${it.sourceSets.main.output.classesDir}"")
				                classDirectories += fileTree(dir: ""${it.sourceSets.main.output.classesDir}"", includes: jacocoIncludes, excludes: jacocoExcludes)
			                } else {
				                logger.error(""Class directory does not exist in sub project: ${it.name}"")
			                }
		                }
	                }
	
	                reports {
		                html.enabled = true
                        xml.enabled = true    
		                xml.destination """ + _reportDirectory + "/" + "summary.xml" + @"""
		                html.destination """ + _reportDirectory + @"""
	                }
                }
                ";
        }

        // Returns true if input1 contains input2 with ignore space else false
        private bool IsContainsIgnoreSpace(string input1, string input2)
        {
            var string1 = Regex.Replace(input1, @"\s", "");
            var string2 = Regex.Replace(input2, @"\s", "");

            return string1.Contains(string2);
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
