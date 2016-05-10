using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Xml;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public sealed class CodeCoverageEnablerForJacocoAnt : AgentService, ICodeCoverageEnabler
    {
        public Type ExtensionType => typeof(ICodeCoverageSummaryReader);
        public string Name => "JaCoCo_Ant";

        private IExecutionContext _executionContext;
        private readonly string _jacocoExecPrefix = "jacoco";

        public void EnableCodeCoverage(IExecutionContext context, CodeCoverageEnablerInputs ccInputs)
        {
            Trace.Entering();
            _executionContext = context;

            ccInputs.VerifyInputsForJacocoAnt(context);

            string sourcesDirectory = context.Variables.Build_SourcesDirectory;
            if (string.IsNullOrWhiteSpace(sourcesDirectory))
            {
                throw new InvalidOperationException(StringUtil.Loc("InvalidSourceDirectory"));
            }

            var buildFileDirectory = Path.GetDirectoryName(ccInputs.BuildFile);
            if (!buildFileDirectory.StartsWith(sourcesDirectory, StringComparison.OrdinalIgnoreCase))
            {
                // build file is not present in the repository.
                // Edit the build file though not present in repository. This will ensure that the single module ant project will work. 
                // Multi module ant project won't work if the build file is not in the repository. 
                EnableJacocoForBuildFile(ccInputs.BuildFile, ccInputs);
            }
            else
            {
                var buildFiles = Directory.GetFiles(sourcesDirectory, "*.xml", SearchOption.AllDirectories);
                foreach (var buildFile in buildFiles)
                {
                    EnableJacocoForBuildFile(buildFile, ccInputs);
                }
            }

            //add jacoco report
            CreateJacocoReport(ccInputs);
            context.Output(StringUtil.Loc("CodeCoverageEnabled", "jacoco", "ant"));
        }

        #region private methods
        private void EnableJacocoForBuildFile(string buildFile, CodeCoverageEnablerInputs antCCParams)
        {
            var buildXml = new XmlDocument();
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                };

                using (XmlReader reader = XmlReader.Create(buildFile, settings))
                {
                    buildXml.Load(reader);
                }
            }
            catch (XmlException e)
            {
                if (buildFile.Equals(antCCParams.BuildFile))
                {
                    _executionContext.Warning(StringUtil.Loc("InvalidBuildXml", buildFile, e.Message));
                    throw;
                }
                else
                {
                    _executionContext.Debug(StringUtil.Format(CodeCoverageConstants.InvalidXMLTemplate, buildFile, e.Message));
                }
                return;
            }

            // see jacoco ant documentation for more details. http://www.eclemma.org/jacoco/trunk/doc/ant.html
            if (!string.IsNullOrWhiteSpace(buildXml.OuterXml))
            {
                var junitNodes = buildXml.SelectNodes("project//junit");
                var javaNodes = buildXml.SelectNodes("project//java");
                var testngNodes = buildXml.SelectNodes("project//testng");

                //collect code coverage
                CollectCodeCoverageForNodes(junitNodes, antCCParams, buildXml, true);
                CollectCodeCoverageForNodes(javaNodes, antCCParams, buildXml);
                CollectCodeCoverageForNodes(testngNodes, antCCParams, buildXml);

                var batchTestNodes = buildXml.SelectNodes("project//batchtest");
                for (var index = 0; index < batchTestNodes.Count; index++)
                {
                    ((XmlElement)batchTestNodes[index]).SetAttribute("fork", "true");
                    _executionContext.Debug(StringUtil.Format(CodeCoverageConstants.SettingAttributeTemplate, "fork", "true", "batchtest"));
                }

                // remove existing jacoco reports
                var isReportNodeExists = RemoveCoverageReports(buildXml);

                if (!(junitNodes.Count == 0 && javaNodes.Count == 0 && testngNodes.Count == 0) || isReportNodeExists)
                {
                    // buildFile is edited
                    _executionContext.Debug(StringUtil.Format(CodeCoverageConstants.EnablingEditingTemplate, "jacoco", "ant", buildFile));
                }
                using (FileStream stream = new FileStream(buildFile, FileMode.OpenOrCreate))
                {
                    buildXml.Save(stream);
                }
            }
        }

        private void CreateJacocoReport(CodeCoverageEnablerInputs antCCParams)
        {
            try
            {
                if (File.Exists(antCCParams.ReportBuildFile))
                {
                    File.Delete(antCCParams.ReportBuildFile);
                }
            }
            catch (IOException e)
            {
                _executionContext.Error(e);
            }

            try
            {
                var reportXml = GetAntReport(antCCParams);
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };
                using (XmlReader reader = XmlReader.Create(new StringReader(reportXml), settings))
                {
                    XmlDocument xdoc = new XmlDocument();
                    xdoc.Load(reader);
                    using (FileStream stream = new FileStream(antCCParams.ReportBuildFile, FileMode.OpenOrCreate))
                    {
                        xdoc.Save(stream);
                    }
                }
            }
            catch (XmlException e)
            {
                _executionContext.Warning(StringUtil.Loc("InvalidBuildXml", antCCParams.ReportBuildFile, e.Message));
                throw;
            }
        }

        private void CollectCodeCoverageForNodes(XmlNodeList nodes, CodeCoverageEnablerInputs antCCParams, XmlDocument buildXml, bool enableForkMode = false)
        {
            for (var index = 0; index < nodes.Count; index++)
            {
                CollectAntCodeCoverageForNode(antCCParams.ReportDirectory, antCCParams.Include, antCCParams.Exclude, (XmlElement)nodes[index], buildXml);
                if (enableForkMode)
                {
                    ((XmlElement)nodes[index]).SetAttribute("forkmode", "once");
                    _executionContext.Debug(StringUtil.Format(CodeCoverageConstants.SettingAttributeTemplate, "forkmode", "once", "junit"));
                }
            }
        }

        private bool RemoveCoverageReports(XmlDocument buildXml)
        {
            bool isReportNodeExists = false;
            var nsmgr = new XmlNamespaceManager(buildXml.NameTable);
            nsmgr.AddNamespace("jacoco", "antlib:org.jacoco.ant");
            var reportNodes = buildXml.SelectNodes("project//jacoco:report", nsmgr);

            if (reportNodes.Count > 0)
            {
                isReportNodeExists = true;
            }

            for (var index = 0; index < reportNodes.Count; index++)
            {
                var parent = reportNodes[index].ParentNode;
                parent.RemoveChild(reportNodes[index]);
            }

            return isReportNodeExists;
        }

        private void CollectAntCodeCoverageForNode(string reportDirectory, string include, string exclude, XmlElement node, XmlDocument buildXml)
        {
            node.SetAttribute("fork", "true");
            _executionContext.Debug(StringUtil.Format(CodeCoverageConstants.SettingAttributeTemplate, "fork", "true", "test"));
            var execFileName = Path.Combine(reportDirectory, _jacocoExecPrefix + ".exec");

            var parentNode = node.ParentNode;
            if (parentNode.Name.Equals("jacoco:coverage"))
            {
                // jacoco coverage node is already present. Update the existing node.
                ((XmlElement)parentNode).SetAttribute("destfile", execFileName);
                ((XmlElement)parentNode).SetAttribute("append", "true");

                // remove existing includes and excludes attribute
                if (parentNode.Attributes["includes"] != null)
                {
                    parentNode.Attributes.Remove(parentNode.Attributes["includes"]);
                }

                if (parentNode.Attributes["excludes"] != null)
                {
                    parentNode.Attributes.Remove(parentNode.Attributes["excludes"]);
                }

                // add user given includes and excludes.
                if (!string.IsNullOrWhiteSpace(include))
                {
                    ((XmlElement)parentNode).SetAttribute("includes", include);
                }

                if (!string.IsNullOrWhiteSpace(exclude))
                {
                    ((XmlElement)parentNode).SetAttribute("excludes", exclude);
                }

                ((XmlElement)parentNode).SetAttribute("xmlns:jacoco", @"antlib:org.jacoco.ant");
            }
            else
            {
                var jacocoCoverageElement = buildXml.CreateElement("jacoco:coverage", "jacoco");
                jacocoCoverageElement.SetAttribute("destfile", execFileName);
                jacocoCoverageElement.SetAttribute("append", "true");

                if (!string.IsNullOrWhiteSpace(include))
                {
                    jacocoCoverageElement.SetAttribute("includes", include);
                }

                if (!string.IsNullOrWhiteSpace(exclude))
                {
                    jacocoCoverageElement.SetAttribute("excludes", exclude);
                }

                jacocoCoverageElement.SetAttribute("xmlns:jacoco", @"antlib:org.jacoco.ant");
                jacocoCoverageElement.AppendChild(node);
                parentNode.AppendChild(jacocoCoverageElement);
            }
        }

        private string GetAntReport(CodeCoverageEnablerInputs antCCParams)
        {
            var executionData = string.Empty;
            executionData += @"             <file file='" + Path.Combine(antCCParams.ReportDirectory, _jacocoExecPrefix + ".exec") + @"'/>" + Environment.NewLine;


            var srcData = CodeCoverageUtilities.GetSourceDataForJacoco(antCCParams.SourceDirectories);
            var classData = CodeCoverageUtilities.GetClassDataForAnt(antCCParams.Include, antCCParams.Exclude, antCCParams.ClassFilesDirectories);

            return string.Format(CodeCoverageConstants.JacocoAntReport, antCCParams.CCReportTask, executionData, classData, srcData, antCCParams.ReportDirectory, Path.Combine(antCCParams.ReportDirectory, "report.csv"), Path.Combine(antCCParams.ReportDirectory, antCCParams.SummaryFile));
        }
        #endregion
    }
}
