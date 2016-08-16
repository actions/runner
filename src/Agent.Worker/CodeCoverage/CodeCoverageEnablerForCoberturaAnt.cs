using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Xml;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public sealed class CodeCoverageEnablerForCoberturaAnt : AgentService, ICodeCoverageEnabler
    {
        public Type ExtensionType => typeof(ICodeCoverageSummaryReader);
        public string Name => "Cobertura_Ant";

        private IExecutionContext _executionContext;
        private readonly string _coberturaSerPrefix = "cobertura";
        private readonly string _instrumentClassesDir = "InstrumentedClasses";

        public void EnableCodeCoverage(IExecutionContext context, CodeCoverageEnablerInputs ccInputs)
        {
            Trace.Entering();
            _executionContext = context;

            ccInputs.VerifyInputsForCoberturaAnt(context);

            var sourcesDirectory = Path.GetDirectoryName(ccInputs.BuildFile);
            if (string.IsNullOrWhiteSpace(sourcesDirectory))
            {
                throw new InvalidOperationException(StringUtil.Loc("InvalidSourceDirectory"));
            }

            var instrumentedClassesDirectory = Path.Combine(Path.GetDirectoryName(ccInputs.BuildFile), _instrumentClassesDir);
            var dataFile = Path.Combine(ccInputs.ReportDirectory, _coberturaSerPrefix + @".ser");

            var buildFiles = Directory.GetFiles(sourcesDirectory, "*.xml", SearchOption.AllDirectories);
            foreach (var buildFile in buildFiles)
            {
                var buildXml = new XmlDocument();

                try
                {
                    var settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit
                    };

                    using (XmlReader reader = XmlReader.Create(buildFile, settings))
                    {
                        buildXml.Load(reader);
                    }
                }
                catch (XmlException e)
                {
                    if (buildFile.Equals(ccInputs.BuildFile))
                    {
                        _executionContext.Warning(StringUtil.Loc("InvalidBuildXml", buildFile, e.Message));
                        throw;
                    }
                    else
                    {
                        _executionContext.Debug(StringUtil.Format(CodeCoverageConstants.InvalidXMLTemplate, buildFile, e.Message));
                    }
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(buildXml.OuterXml))
                {
                    //Remove cobertura instrument nodes
                    var isInstrumentNodeExists = RemoveNodesWithName(buildXml, "project//cobertura-instrument");

                    // get instrument node
                    var instrumentNode = GetInstrumentNode(ccInputs, instrumentedClassesDirectory, dataFile);

                    var junitNodes = buildXml.SelectNodes("project//junit");
                    var javaNodes = buildXml.SelectNodes("project//java");
                    var testngNodes = buildXml.SelectNodes("project//testng");

                    //collect code coverage
                    CollectCodeCoverageForNodes(junitNodes, ccInputs, buildXml, instrumentNode, instrumentedClassesDirectory, dataFile, true);
                    CollectCodeCoverageForNodes(javaNodes, ccInputs, buildXml, instrumentNode, instrumentedClassesDirectory, dataFile);
                    CollectCodeCoverageForNodes(testngNodes, ccInputs, buildXml, instrumentNode, instrumentedClassesDirectory, dataFile);

                    var batchTestNodes = buildXml.SelectNodes("project//batchtest");
                    for (var index = 0; index < batchTestNodes.Count; index++)
                    {
                        ((XmlElement)batchTestNodes[index]).SetAttribute("fork", "true");
                        _executionContext.Debug(StringUtil.Format(CodeCoverageConstants.SettingAttributeTemplate, "fork", "true", "batchtest"));
                    }

                    // make debug=true for javac nodes
                    var javacNodes = buildXml.SelectNodes("project//javac");
                    for (int index = 0; index < javacNodes.Count; index++)
                    {
                        ((XmlElement)javacNodes[index]).SetAttribute("debug", "true");
                    }

                    // Removing any cobertura report nodes
                    var isReportNodeExists = RemoveNodesWithName(buildXml, "project//cobertura-report");

                    if (!(junitNodes.Count == 0 && javaNodes.Count == 0 && testngNodes.Count == 0) || isInstrumentNodeExists || isReportNodeExists)
                    {
                        // buildFile is edited
                        context.Debug(StringUtil.Format(CodeCoverageConstants.EnablingEditingTemplate, "cobertura", "ant", buildFile));

                        //Adding Path and TaskDef for Cobertura
                        var envProperty = AddEnvProperty();
                        var pathNode = AddPathForCobertura();
                        var taskDefNode = AddTaskDefForCobertura();
                        var envTag = buildXml.ImportNode(envProperty, true);
                        var pathTag = buildXml.ImportNode(pathNode, true);
                        var taskDefTag = buildXml.ImportNode(taskDefNode, true);

                        buildXml.DocumentElement.InsertBefore(envTag, buildXml.DocumentElement.FirstChild);
                        buildXml.DocumentElement.InsertAfter(pathTag, envTag);
                        buildXml.DocumentElement.InsertAfter(taskDefTag, pathTag);
                    }

                    using (FileStream stream = new FileStream(buildFile, FileMode.Create))
                    {
                        buildXml.Save(stream);
                    }
                }
            }

            // add cobertura report
            CreateCoberturaReport(ccInputs, dataFile);
            context.Output(StringUtil.Loc("CodeCoverageEnabled", "cobertura", "ant"));
        }

        private void CreateCoberturaReport(CodeCoverageEnablerInputs ccInputs, string dataFile)
        {
            try
            {
                if (File.Exists(ccInputs.ReportBuildFile))
                {
                    File.Delete(ccInputs.ReportBuildFile);
                }
            }
            catch (IOException e)
            {
                _executionContext.Error(e);
            }

            try
            {
                var reportXml = GetAntReport(ccInputs, dataFile);
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };
                using (XmlReader reader = XmlReader.Create(new StringReader(reportXml), settings))
                {
                    XmlDocument xdoc = new XmlDocument();
                    xdoc.Load(reader);
                    using (FileStream stream = new FileStream(ccInputs.ReportBuildFile, FileMode.Create))
                    {
                        xdoc.Save(stream);
                    }
                }
            }
            catch (XmlException e)
            {
                _executionContext.Warning(StringUtil.Loc("InvalidBuildXml", ccInputs.ReportBuildFile, e.Message));
                throw;
            }
        }

        private bool RemoveNodesWithName(XmlDocument buildXml, string name)
        {
            var nodes = buildXml.SelectNodes(name);
            var isNodeWithNameExists = false;

            for (var index = 0; index < nodes.Count; index++)
            {
                isNodeWithNameExists = true;
                nodes[index].ParentNode.RemoveChild(nodes[index]);
            }

            return isNodeWithNameExists;
        }

        private string GetAntReport(CodeCoverageEnablerInputs ccInputs, string dataFile)
        {
            var targetXml = string.Format(CodeCoverageConstants.CoberturaAntReport, AddEnvProperty().OuterXml, AddPathForCobertura().OuterXml, AddTaskDefForCobertura().OuterXml,
                                        ccInputs.CCReportTask, ccInputs.ReportDirectory, dataFile, ccInputs.SourceDirectories);
            return targetXml;
        }

        private XmlNode GetInstrumentNode(CodeCoverageEnablerInputs antCCParams, string instrumentedClassesDirectory, string dataFile)
        {
            var inclusionExclusionSet = CodeCoverageUtilities.GetClassDataForAnt(antCCParams.Include, antCCParams.Exclude, antCCParams.ClassFilesDirectories);

            var targetXml = string.Format(CodeCoverageConstants.CoberturaInstrumentNode, instrumentedClassesDirectory, dataFile, inclusionExclusionSet);

            return GetXmlNode(targetXml);
        }

        private XmlNode AddEnvProperty()
        {
            var envXML = CodeCoverageConstants.CoberturaEnvProperty;
            return GetXmlNode(envXML);
        }

        private XmlNode AddPathForCobertura()
        {
            var pathXML = CodeCoverageConstants.CoberturaClassPath;

            return GetXmlNode(pathXML);
        }

        private XmlNode AddTaskDefForCobertura()
        {
            var taskDefXML = CodeCoverageConstants.CoberturaTaskDef;

            return GetXmlNode(taskDefXML);
        }

        private XmlNode GetXmlNode(string xmlString)
        {
            var xmlDoc = new XmlDocument();
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit
            };

            using (XmlReader reader = XmlReader.Create(new StringReader(xmlString), settings))
            {
                xmlDoc.Load(reader);
            }
            return xmlDoc.DocumentElement;
        }

        private void CollectCodeCoverageForNodes(XmlNodeList nodes, CodeCoverageEnablerInputs ccInputs, XmlDocument buildXml, XmlNode instrumentNode, string instrumentedClassesDirectory, string dataFile, bool enableForkMode = false)
        {
            for (var index = 0; index < nodes.Count; index++)
            {
                var instrumentTag = buildXml.ImportNode(instrumentNode, true);
                CollectAntCodeCoverageForNode(ccInputs.ReportDirectory, ccInputs.Include, ccInputs.Exclude, (XmlElement)nodes[index], buildXml, instrumentTag, instrumentedClassesDirectory, dataFile);
                if (enableForkMode)
                {
                    ((XmlElement)nodes[index]).SetAttribute("forkmode", "once");
                    _executionContext.Debug(StringUtil.Format(CodeCoverageConstants.SettingAttributeTemplate, "forkmode", "once", "junit"));
                }
            }
        }

        private void RemoveSysNodes(XmlElement node)
        {
            var sysnodes = node.SelectNodes("sysproperty");

            for (int index = 0; index < sysnodes.Count; index++)
            {
                if (sysnodes[index].Attributes != null && sysnodes[index].Attributes["key"] != null)
                {
                    if (sysnodes[index].Attributes["key"].Value.Equals("net.sourceforge.cobertura.datafile", StringComparison.OrdinalIgnoreCase))
                    {
                        sysnodes[index].ParentNode.RemoveChild(sysnodes[index]);
                    }
                }
            }
        }

        private void CollectAntCodeCoverageForNode(string reportDirectory, string include, string exclude, XmlElement node, XmlDocument buildXml, XmlNode instrumentNode, string instrumentedClassesDirectory, string dataFile)
        {
            // add instrument node befor the test node.            
            var parentNode = node.ParentNode;
            parentNode.InsertBefore(instrumentNode, node);

            node.SetAttribute("fork", "true");
            _executionContext.Debug(StringUtil.Format(CodeCoverageConstants.SettingAttributeTemplate, "fork", "true", "test"));

            RemoveSysNodes(node);

            var coberturaCoverageElement = buildXml.CreateElement("sysproperty");
            coberturaCoverageElement.SetAttribute("key", "net.sourceforge.cobertura.datafile");
            coberturaCoverageElement.SetAttribute("file", dataFile);
            node.InsertBefore(coberturaCoverageElement, node.FirstChild);

            var instrumentElement = buildXml.CreateElement("classpath");
            instrumentElement.SetAttribute("location", instrumentedClassesDirectory);
            node.InsertAfter(instrumentElement, node.FirstChild);

            var classPathElement = buildXml.CreateElement("classpath");
            classPathElement.SetAttribute("refid", CodeCoverageConstants.CoberturaClassPathString);
            node.InsertAfter(classPathElement, node.LastChild);
        }
    }
}
