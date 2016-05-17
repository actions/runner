using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public sealed class CodeCoverageEnablerForCoberturaGradle : AgentService, ICodeCoverageEnabler
    {
        public Type ExtensionType => typeof(ICodeCoverageSummaryReader);
        public string Name => "Cobertura_Gradle";

        public void EnableCodeCoverage(IExecutionContext context, CodeCoverageEnablerInputs ccInputs)
        {
            Trace.Entering();

            ccInputs.VerifyInputsForCoberturaGradle();

            context.Debug(StringUtil.Format(CodeCoverageConstants.EnablingEditingTemplate, "cobertura", "gradle", ccInputs.BuildFile));
            var buildScript = new FileInfo(ccInputs.BuildFile);

            if (buildScript.Length == 0)
            {
                throw new InvalidOperationException(StringUtil.Loc("CodeCoverageBuildFileIsEmpty", ccInputs.BuildFile));
            }

            CodeCoverageUtilities.PrependDataToFile(ccInputs.BuildFile, GetCoberturaPluginDefination());
            File.AppendAllText(ccInputs.BuildFile, GetGradleCoberturaReport(ccInputs));
            context.Output(StringUtil.Loc("CodeCoverageEnabled", "cobertura", "gradle"));
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

        private string GetCoberturaPluginScriptForGradle(string includes, string excludes)
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

                    cobertura.coverageIncludes = [" + includes + @"]
                    cobertura.coverageExcludes = [" + excludes + @"]
                }
                ";
        }

        private string GetCoberturaReportingScriptForSingleModuleGradle(string classDir, string sourceDir, string reportDir)
        {
            return
                @"
                cobertura {
                    coverageDirs = [" + (string.IsNullOrWhiteSpace(classDir) ? @"${project.sourceSets.main.output.classesDir}" : classDir) + @"]
                    coverageSourceDirs = " + (string.IsNullOrWhiteSpace(sourceDir) ? @"project.sourceSets.main.java.srcDirs" : sourceDir) + @"
                    coverageReportDir = new File('" + reportDir + @"')
                    coverageFormats = ['xml', 'html']
                }
                ";
        }

        private string GetCoberturaReportingScriptForMultiModuleGradle(string classDir, string sourceDir, string reportDir)
        {
            var data =
                @"
                test {
                    dependsOn = subprojects.test
                }

                cobertura {	
                    coverageSourceDirs = []";

            if (string.IsNullOrWhiteSpace(classDir))
            {
                data +=
                    @"
                    rootProject.subprojects.each {
		                coverageDirs << file(""${it.sourceSets.main.output.classesDir}"")
                    }";
            }
            else
            {
                data +=
                    @"
                    coverageDirs = [""" + classDir + @"""]";
            }
            if (string.IsNullOrWhiteSpace(sourceDir))
            {
                data +=
                    @"
                    rootProject.subprojects.each {
	                    coverageSourceDirs += it.sourceSets.main.java.srcDirs
                    }";
            }
            else
            {
                data +=
                    @"
                    coverageSourceDirs = [""" + sourceDir + @"""]";
            }

            return data + @"
                        coverageFormats = [ 'xml', 'html' ]
                        coverageMergeDatafiles = subprojects.collect { new File(it.projectDir, '/build/cobertura/cobertura.ser') }
                        coverageReportDir = new File('" + reportDir + @"')
                    }
                    ";
        }

        private string GetGradleCoberturaReport(CodeCoverageEnablerInputs gradleCCParams)
        {
            var coberturaExclude = CodeCoverageUtilities.TrimToEmptyString(gradleCCParams.Exclude);
            var coberturaInclude = CodeCoverageUtilities.TrimToEmptyString(gradleCCParams.Include);
            var exclude = string.IsNullOrEmpty(coberturaExclude) ? string.Empty : string.Join(",", coberturaExclude.Split(':').Select(exclPackage => exclPackage.EndsWith("*") ? "'.*" + exclPackage.TrimEnd('*') + ".*'" : "'.*" + exclPackage + "'"));
            var include = string.IsNullOrEmpty(coberturaInclude) ? string.Empty : string.Join(",", coberturaInclude.Split(':').Select(inclPackage => inclPackage.EndsWith("*") ? "'.*" + inclPackage.TrimEnd('*') + ".*'" : "'.*" + inclPackage + "'"));
            var classDirectories = string.Join(",", gradleCCParams.ClassFilesDirectories.Split(',').Select(exclPackage => "'" + exclPackage + "'"));
            var enableCobertura = GetCoberturaPluginScriptForGradle(include, exclude);

            if (!gradleCCParams.IsMultiModule)
            {
                enableCobertura = string.Concat(enableCobertura, GetCoberturaReportingScriptForSingleModuleGradle(classDirectories, null,
                    gradleCCParams.ReportDirectory));
            }
            else
            {
                enableCobertura = string.Concat(enableCobertura, GetCoberturaReportingScriptForMultiModuleGradle(classDirectories, null,
                    gradleCCParams.ReportDirectory));
            }

            return enableCobertura;
        }
    }
}
