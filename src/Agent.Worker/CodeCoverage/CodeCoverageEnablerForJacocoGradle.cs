using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public sealed class CodeCoverageEnablerForJacocoGradle : AgentService, ICodeCoverageEnabler
    {
        public Type ExtensionType => typeof(ICodeCoverageSummaryReader);
        public string Name => "JaCoCo_Gradle";

        private readonly string _jacocoExecPrefix = "jacoco";
        private readonly string _summaryFile = "summary.xml";

        public void EnableCodeCoverage(IExecutionContext context, CodeCoverageEnablerInputs ccInputs)
        {
            Trace.Entering();

            ccInputs.VerifyInputsForJacocoGradle();

            context.Debug(StringUtil.Format(CodeCoverageConstants.EnablingEditingTemplate, "jacoco", "gradle", ccInputs.BuildFile));

            var buildScript = new FileInfo(ccInputs.BuildFile);

            if (buildScript.Length == 0)
            {
                throw new InvalidOperationException(StringUtil.Loc("CodeCoverageBuildFileIsEmpty", ccInputs.BuildFile));
            }

            // see jacoco gradle documentation for more details. https://docs.gradle.org/current/userguide/jacoco_plugin.html

            var jacocoExclude = CodeCoverageUtilities.TrimToEmptyString(ccInputs.Exclude).Replace('.', '/');
            var jacocoInclude = CodeCoverageUtilities.TrimToEmptyString(ccInputs.Include).Replace('.', '/');
            var exclude = string.IsNullOrEmpty(jacocoExclude) ? string.Empty : string.Join(",", jacocoExclude.Split(':').Select(
                exclPackage => exclPackage.EndsWith("*") ? ("'" + exclPackage + "/**'") : ("'" + exclPackage + ".class'")));
            var include = string.IsNullOrEmpty(jacocoInclude) ? string.Empty : string.Join(",", jacocoInclude.Split(':').Select(
                inclPackage => inclPackage.EndsWith("*") ? ("'" + inclPackage + "/**'") : ("'" + inclPackage + ".class'")));

            var enableJacoco = string.Empty;

            if (ccInputs.IsMultiModule)
            {
                enableJacoco = @"
                    allprojects { apply plugin: 'jacoco' }

                    allprojects {
	                    repositories {
                            mavenCentral()
                        }
                    }

                    def jacocoExcludes = [" + CodeCoverageUtilities.TrimToEmptyString(exclude) + @"]
                    def jacocoIncludes = [" + CodeCoverageUtilities.TrimToEmptyString(include) + @"]

                    subprojects {
	
                        jacocoTestReport {
		                    doFirst {
			                    classDirectories = fileTree(dir: """ + ccInputs.ClassFilesDirectories + @""").exclude(jacocoExcludes).include(jacocoIncludes)
		                    }
		
		                    reports {
			                    html.enabled = true
			                    html.destination ""${buildDir}/jacocoHtml""
                                xml.enabled = true    
	                            xml.destination ""${buildDir}" + "/" + _summaryFile + @"""
                            }
                        }
	
	                    test {
		                    jacoco {
			                    append = true
			                    destinationFile = file(""" + ccInputs.ReportDirectory + "/" + _jacocoExecPrefix + ".exec\"" + @")
		                    }
	                    }
                    }" + @"
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
		                    xml.destination """ + ccInputs.ReportDirectory + "/" + _summaryFile + @"""
		                    html.destination """ + ccInputs.ReportDirectory + @"""
	                    }
                    }
                    ";
            }
            else
            {
                enableJacoco = @"
                    allprojects { apply plugin: 'jacoco' }

                    allprojects {
	                    repositories {
                            mavenCentral()
                        }
                    }

                    def jacocoExcludes = [" + CodeCoverageUtilities.TrimToEmptyString(exclude) + @"]
                    def jacocoIncludes = [" + CodeCoverageUtilities.TrimToEmptyString(include) + @"]


	
                    jacocoTestReport {
	                    doFirst {
		                    classDirectories = fileTree(dir: """ + ccInputs.ClassFilesDirectories + @""").exclude(jacocoExcludes).include(jacocoIncludes)
	                    }
		
	                    reports {
	                        html.enabled = true
                            xml.enabled = true    
	                        xml.destination """ + ccInputs.ReportDirectory + "/" + _summaryFile + @"""
	                        html.destination """ + ccInputs.ReportDirectory + @"""
                        }
                    }
	
                    test {
                        finalizedBy jacocoTestReport
	                    jacoco {
		                    append = true
		                    destinationFile = file(""" + ccInputs.ReportDirectory + "/" + _jacocoExecPrefix + ".exec\"" + @")
	                    }
                    }
                    ";
            }
            File.AppendAllText(ccInputs.BuildFile, enableJacoco);
            context.Output(StringUtil.Loc("CodeCoverageEnabled", "jacoco", "gradle"));
        }
    }
}
