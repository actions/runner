using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public sealed class CodeCoverageEnablerForJacocoMaven : AgentService, ICodeCoverageEnabler
    {
        public Type ExtensionType => typeof(ICodeCoverageSummaryReader);
        public string Name => "JaCoCo_Maven";

        private IExecutionContext _executionContext;
        private bool _isMultiModule;
        private string _jacocoVersion = CodeCoverageConstants.JacocoVersion;
        private const string _jacocoGroupId = "org.jacoco";
        private const string _jacocoArtifactId = "jacoco-maven-plugin";
        private const string _pluginsTag = "plugins";
        private readonly string _jacocoExecPrefix = "jacoco";
        private readonly string _mavenAntRunPluginVersion = CodeCoverageConstants.MavenAntRunPluginVersion;

        public void EnableCodeCoverage(IExecutionContext context, CodeCoverageEnablerInputs ccInputs)
        {
            Trace.Entering();
            _executionContext = context;

            ccInputs.VerifyInputsForJacocoMaven();

            context.Debug(StringUtil.Format(CodeCoverageConstants.EnablingEditingTemplate, "jacoco", "maven", ccInputs.BuildFile));

            // see jacoco maven documentation for more details. http://www.eclemma.org/jacoco/trunk/doc/maven.html
            XElement pomXml;
            try
            {
                pomXml = XElement.Load(ccInputs.BuildFile);
            }
            catch (XmlException e)
            {
                _executionContext.Warning(StringUtil.Loc("InvalidBuildXml", ccInputs.BuildFile, e.Message));
                throw;
            }

            XNamespace xNameSpace = pomXml.Attribute("xmlns").Value;

            if (pomXml.Element(xNameSpace + "modules") != null)
            {
                // aa multi module project 
                _isMultiModule = true;
                context.Debug(CodeCoverageConstants.MavenMultiModule);
            }

            // Add a build element if doesnot exist.
            var build = pomXml.Element(xNameSpace + "build");
            if (build == null)
            {
                pomXml.Add(new XElement("build"));
                build = pomXml.Element("build");
            }

            // Set jacoco plugins to enable code coverage.
            SetPlugins(build, ccInputs, xNameSpace);

            foreach (var e in build.DescendantsAndSelf().Where(e => string.IsNullOrEmpty(e.Name.Namespace.NamespaceName)))
            {
                e.Name = xNameSpace + e.Name.LocalName;
            }

            using (FileStream stream = new FileStream(ccInputs.BuildFile, FileMode.Create))
            {
                pomXml.Save(stream);
            }

            if (_isMultiModule)
            {
                CreateReportPomForMultiModule(ccInputs.ReportBuildFile, xNameSpace, pomXml, ccInputs);
            }

            context.Output(StringUtil.Loc("CodeCoverageEnabled", "jacoco", "maven"));
        }

        #region private methods

        private void SetPlugins(XElement build, CodeCoverageEnablerInputs mavenCcParams, XNamespace xNameSpace)
        {
            var defaultConfigurationElement = new XElement("configuration");
            AddConfigurationDefaults(defaultConfigurationElement, xNameSpace, mavenCcParams);

            var defaultExecutions = GetDefaultExecutions();

            var pluginManagement = build.Element(xNameSpace + "pluginManagement");
            if (pluginManagement != null)
            {
                var pluginManagementplugins = pluginManagement.Element(xNameSpace + _pluginsTag);

                if (pluginManagementplugins != null)
                {
                    SetPluginsForJacoco(mavenCcParams, pluginManagementplugins, xNameSpace, defaultConfigurationElement, defaultExecutions, false);
                }
            }

            var plugins = build.Element(xNameSpace + _pluginsTag);
            if (plugins == null)
            {
                // no pluggins add plugins default jacoco plugin. <plugins>  default jacoco plugin </plugins>
                build.Add(new XElement(_pluginsTag));
                plugins = build.Element(_pluginsTag);
            }
            SetPluginsForJacoco(mavenCcParams, plugins, xNameSpace, defaultConfigurationElement, defaultExecutions, true);
        }

        private void SetPluginsForJacoco(CodeCoverageEnablerInputs mavenCcParams, XElement plugins, XNamespace xNameSpace, XElement defaultConfigurationElement, XElement defaultExecutions, bool addIfNotExists)
        {
            IList<XElement> pluginList = plugins.Elements(xNameSpace + "plugin").ToList();
            var hasJacocoPlugin = false;

            foreach (var plugin in pluginList.Where(plugin =>
            {
                var groupId = plugin.Element(xNameSpace + "groupId");
                var artifactId = plugin.Element(xNameSpace + "artifactId");
                return (artifactId != null && artifactId.Value == _jacocoArtifactId)
                    || (groupId != null && groupId.Value == _jacocoGroupId);
            }))
            {
                _executionContext.Debug("A jacoco plugin already exists. Adding/Editing the configuration values to match inputs provided in the task.");

                // jacoco plugin
                hasJacocoPlugin = true;

                // set the values to ensure artifactid and groupid are both not null
                plugin.SetElementValue(xNameSpace + "groupId", _jacocoGroupId);
                plugin.SetElementValue(xNameSpace + "artifactId", _jacocoArtifactId);

                // dont change the existing jacoco version as it needs particular version of maven
                var versionElements = plugin.Elements(xNameSpace + "version").ToList();
                if (versionElements.Count == 0)
                {
                    // add version element if it is not there
                    plugin.SetElementValue(xNameSpace + "version", _jacocoVersion);
                }
                else
                {
                    _jacocoVersion = versionElements[0].Value;
                }

                // Edit the root configurations to default values. ie., user inputs.
                IList<XElement> rootConfigurations = plugin.Elements(xNameSpace + "configuration").ToList();

                // If there is no root configuration add one
                if (rootConfigurations.Count == 0)
                {
                    plugin.Add(defaultConfigurationElement);
                }
                else
                {
                    foreach (var rootConfiguration in rootConfigurations)
                    {
                        AddConfigurationDefaults(rootConfiguration, xNameSpace, mavenCcParams);
                    }
                }

                // <executions> <execution> <configuration/> </execution> </executions>
                IList<XElement> executionsElements = plugin.Elements(xNameSpace + "executions").ToList();

                // if executions element doesnt exist add it.
                if (executionsElements.Count == 0)
                {
                    plugin.Add(defaultExecutions);
                }
                else
                {
                    foreach (var executionsElement in executionsElements)
                    {
                        IList<XElement> executionElements = executionsElement.Elements(xNameSpace + "execution").ToList();

                        foreach (var executionElement in executionElements)
                        {
                            // isPrepartAgentExecution is a hack, without this +: filter shows 0% coverage. 
                            // Here we are collecting code coverage for all class files but reports are generated according to include/exclude filters
                            var isPrepartAgentExecution = IsPrepareAgentExecution(executionElement, xNameSpace);

                            //  Edit the configurations of executionsElements to default values. ie., user inputs.
                            IList<XElement> configurations = executionElement.Elements(xNameSpace + "configuration").ToList();
                            foreach (var configuration in configurations)
                            {
                                AddConfigurationDefaults(configuration, xNameSpace, mavenCcParams, isPrepartAgentExecution);
                            }
                            if (_isMultiModule)
                            {
                                // remove the report goal.
                                IList<XElement> goalsElements = executionElement.Elements(xNameSpace + "goals").ToList();
                                foreach (var goalsElement in goalsElements)
                                {
                                    IList<XElement> goalElements = goalsElement.Elements(xNameSpace + "goal").ToList();
                                    for (int index = 0; index < goalElements.Count; index++)
                                    {
                                        if (goalElements[index].Value.Equals("report", StringComparison.OrdinalIgnoreCase))
                                        {
                                            goalsElements[index].Remove();
                                        }
                                    }
                                }
                            }
                        }

                        // add default execution nodes to make sure we are not missing default coverage
                        executionsElement.Add(defaultExecutions.Elements().ToList());
                    }
                }
            }

            if (addIfNotExists && !hasJacocoPlugin)
            {
                _executionContext.Debug("Adding a jacoco plugin.");
                // if there is no jacoco plugin add one
                plugins.Add(GetDefaultJacocoPlugin(defaultConfigurationElement, defaultExecutions));
            }
        }

        private void CreateReportPomForMultiModule(string multiModulePomFilePath, XNamespace xNameSpace, XElement pomXml, CodeCoverageEnablerInputs mavenCcParams)
        {
            try
            {
                if (File.Exists(multiModulePomFilePath))
                {
                    File.Delete(multiModulePomFilePath);
                }
            }
            catch (IOException e)
            {
                _executionContext.Error(e);
            }

            try
            {
                var reportXml = GetReportPomXml(xNameSpace, pomXml, mavenCcParams);
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };
                using (XmlReader reader = XmlReader.Create(new StringReader(reportXml), settings))
                {
                    XmlDocument xdoc = new XmlDocument();
                    xdoc.Load(reader);
                    using (FileStream stream = new FileStream(multiModulePomFilePath, FileMode.Create))
                    {
                        xdoc.Save(stream);
                    }
                }
            }
            catch (XmlException e)
            {
                _executionContext.Warning(StringUtil.Loc("InvalidBuildXml", multiModulePomFilePath, e.Message));
                throw;
            }
        }

        private bool IsPrepareAgentExecution(XElement executionElement, XNamespace xNameSpace)
        {
            IList<XElement> goalsElements = executionElement.Elements(xNameSpace + "goals").ToList();
            foreach (var goalsElement in goalsElements)
            {
                IList<XElement> goalElements = goalsElement.Elements(xNameSpace + "goal").ToList();
                for (int index = 0; index < goalElements.Count; index++)
                {
                    if (goalElements[index].Value.Equals("prepare-agent", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private XElement GetDefaultJacocoPlugin(XElement defaultConfigurationElement, XElement defaultExecutions)
        {
            return new XElement("plugin",
                    new XElement("groupId", _jacocoGroupId),
                    new XElement("artifactId", _jacocoArtifactId),
                    new XElement("version", _jacocoVersion),
                    defaultConfigurationElement,
                    defaultExecutions
                    );
        }

        private void AddConfigurationDefaults(XElement configuration, XNamespace xNameSpace, CodeCoverageEnablerInputs mavenCcParams, bool isPrepartAgentExecution = false)
        {
            var excludesElement = CodeCoverageUtilities.GetClassDataForMaven(mavenCcParams.Exclude, "excludes", "exclude", true);
            XElement includesElement;
            if (!isPrepartAgentExecution)
            {
                includesElement = CodeCoverageUtilities.GetClassDataForMaven(mavenCcParams.Include, "includes", "include", true);
            }
            else
            {
                includesElement = new XElement("includes",
                                new XElement("include", "**/*"));
            }

            configuration.SetElementValue(xNameSpace + "destFile", Path.Combine(mavenCcParams.ReportDirectory, _jacocoExecPrefix + ".exec"));
            configuration.SetElementValue(xNameSpace + "outputDirectory", mavenCcParams.ReportDirectory);
            configuration.SetElementValue(xNameSpace + "dataFile", Path.Combine(mavenCcParams.ReportDirectory, _jacocoExecPrefix + ".exec"));
            configuration.SetElementValue(xNameSpace + "append", "true");

            // remove any existing excludes and includes. We cannot directly set the values here as the value is not one single object but multiple xelements
            configuration.SetElementValue(xNameSpace + excludesElement.Name.ToString(), null);
            configuration.SetElementValue(xNameSpace + includesElement.Name.ToString(), null);

            // add includes and excludes from user inputs
            configuration.Add(includesElement);
            configuration.Add(excludesElement);
        }

        private XElement GetDefaultExecutions()
        {
            // prepareAgentConfiguration is a hack, without this +: filter shows 0% coverage. 
            // Here we are collecting code coverage for all class files but reports are generated according to include/exclude filters
            var prepareAgentConfiguration = new XElement("configuration",
                                new XElement("includes",
                                    new XElement("include", "**/*")
                                ));

            // guid is used to make sure the element name is always unique
            if (_isMultiModule)
            {
                return new XElement("executions",
                    GetDefaultExecution("default-prepare-agent" + Guid.NewGuid(), "prepare-agent", null, prepareAgentConfiguration)
                    );
            }

            return new XElement("executions",
                  GetDefaultExecution("default-prepare-agent" + Guid.NewGuid(), "prepare-agent", null, prepareAgentConfiguration),
                  GetDefaultExecution("default-report" + Guid.NewGuid(), "report", "test")
                  );
        }

        private XElement GetDefaultExecution(string id, string goal, string phase = null, XElement configuration = null)
        {
            var executionElement = new XElement("execution",
                        configuration,
                        new XElement("id", id),
                        new XElement("goals",
                            new XElement("goal", goal)
                        )
                    );

            if (phase != null)
            {
                executionElement.SetElementValue("phase", phase);
            }
            return executionElement;
        }

        private string GetSettingsForPom(XNamespace xNameSpace, XElement pomXml)
        {
            string settings = string.Empty;
            string[] settingsNodes = { "modelVersion", "groupId", "artifactId", "version" };
            string[] settingsNodesDefaults = { "4.0.0", "reports", "report", "1.0" };
            for (int index = 0; index < settingsNodes.Count(); index++)
            {
                var element = pomXml.Element(xNameSpace + settingsNodes[index]);
                if (element != null)
                {
                    settings += @"   <" + settingsNodes[index] + @">" + element.Value + @"</" + settingsNodes[index] + @">" + Environment.NewLine;
                }
                else
                {
                    settings += @"   <" + settingsNodes[index] + @">" + settingsNodesDefaults[index] + @"</" + settingsNodes[index] + @">" + Environment.NewLine;
                }
            }

            settings += @"   <packaging>pom</packaging>" + Environment.NewLine;

            return settings;
        }

        private string GetReportPomXml(XNamespace xNameSpace, XElement pomXml, CodeCoverageEnablerInputs mavenCcParams)
        {
            string settings = string.Empty;
            settings += GetSettingsForPom(xNameSpace, pomXml);


            var srcDirectories = CodeCoverageUtilities.SetCurrentDirectoryIfDirectoriesParameterIsEmpty(_executionContext, mavenCcParams.SourceDirectories, StringUtil.Loc("SourceDirectoriesNotSpecifiedForMultiModule"));
            var classFilesDirectories = CodeCoverageUtilities.SetCurrentDirectoryIfDirectoriesParameterIsEmpty(_executionContext, mavenCcParams.ClassFilesDirectories, StringUtil.Loc("ClassDirectoriesNotSpecifiedForMultiModule"));

            var srcData = CodeCoverageUtilities.GetSourceDataForJacoco(srcDirectories);
            var classData = CodeCoverageUtilities.GetClassDataForAnt(mavenCcParams.Include, mavenCcParams.Exclude, classFilesDirectories);

            var execFile = Path.Combine(mavenCcParams.ReportDirectory, _jacocoExecPrefix + ".exec");
            var csvFile = Path.Combine(mavenCcParams.ReportDirectory, "report.csv");
            var summaryFile = Path.Combine(mavenCcParams.ReportDirectory, mavenCcParams.SummaryFile);

            // ref https://dzone.com/articles/jacoco-maven-multi-module

            return
                @"<?xml version='1.0' encoding='UTF-8'?>" + Environment.NewLine +
                @" <project xmlns='http://maven.apache.org/POM/4.0.0' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd'>" + Environment.NewLine +
                settings +
                @"   <build>" + Environment.NewLine +
                @"      <plugins>" + Environment.NewLine +
                @"         <plugin>" + Environment.NewLine +
                @"            <groupId>org.apache.maven.plugins</groupId>" + Environment.NewLine +
                @"            <artifactId>maven-antrun-plugin</artifactId>" + Environment.NewLine +
                @"            <version>" + _mavenAntRunPluginVersion + @"</version>" + Environment.NewLine +
                @"            <executions>" + Environment.NewLine +
                @"               <execution>" + Environment.NewLine +
                @"                  <phase>post-integration-test</phase>" + Environment.NewLine +
                @"                  <goals>" + Environment.NewLine +
                @"                     <goal>run</goal>" + Environment.NewLine +
                @"                  </goals>" + Environment.NewLine +
                @"                  <configuration>" + Environment.NewLine +
                @"                     <target>" + Environment.NewLine +
                @"                        <echo message='Generating JaCoCo Reports' />" + Environment.NewLine +
                @"                        <taskdef name='report' classname='org.jacoco.ant.ReportTask'>" + Environment.NewLine +
                @"                           <classpath path='{basedir}/target/jacoco-jars/org.jacoco.ant.jar' />" + Environment.NewLine +
                @"                       </taskdef>" + Environment.NewLine +
                @"                        <report>" + Environment.NewLine +
                @"                           <executiondata>" + Environment.NewLine +
                @"                              <file file='" + execFile + @"' />" + Environment.NewLine +
                @"                           </executiondata>" + Environment.NewLine +
                @"                           <structure name='Jacoco report'>" + Environment.NewLine +
                @"                              <classfiles>" + Environment.NewLine +
                classData +
                @"                              </classfiles>" + Environment.NewLine +
                @"                              <sourcefiles encoding = 'UTF-8'>" + Environment.NewLine +
                srcData +
                @"                              </sourcefiles>" + Environment.NewLine +
                @"                           </structure>" + Environment.NewLine +
                @"                           <html destdir='" + mavenCcParams.ReportDirectory + @"' />" + Environment.NewLine +
                @"                           <xml destfile='" + summaryFile + @"' />" + Environment.NewLine +
                @"                           <csv destfile='" + csvFile + @"' />" + Environment.NewLine +
                @"                        </report>" + Environment.NewLine +
                @"                     </target>" + Environment.NewLine +
                @"                  </configuration>" + Environment.NewLine +
                @"               </execution>" + Environment.NewLine +
                @"            </executions>" + Environment.NewLine +
                @"            <dependencies>" + Environment.NewLine +
                @"               <dependency>" + Environment.NewLine +
                @"                  <groupId>org.jacoco</groupId>" + Environment.NewLine +
                @"                  <artifactId>org.jacoco.ant</artifactId>" + Environment.NewLine +
                @"                  <version>" + _jacocoVersion + @"</version>" + Environment.NewLine +
                @"               </dependency>" + Environment.NewLine +
                @"            </dependencies>" + Environment.NewLine +
                @"         </plugin>" + Environment.NewLine +
                @"     </plugins>" + Environment.NewLine +
                @"   </build>" + Environment.NewLine +
                @" </project>";
        }
        #endregion
    }
}
