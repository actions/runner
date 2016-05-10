using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public sealed class CodeCoverageEnablerForCoberturaMaven : AgentService, ICodeCoverageEnabler
    {
        public Type ExtensionType => typeof(ICodeCoverageSummaryReader);
        public string Name => "Cobertura_Maven";

        private IExecutionContext _executionContext;
        private bool _isMultiModule;
        private const string _coberturaGroupId = "org.codehaus.mojo";
        private const string _coberturaArtifactId = "cobertura-maven-plugin";
        private const string _pluginsTag = "plugins";
        private const string _pluginTag = "plugin";
        private string _coberturaVersion = CodeCoverageConstants.CoberturaVersion;

        public void EnableCodeCoverage(IExecutionContext context, CodeCoverageEnablerInputs ccInputs)
        {
            Trace.Entering();
            _executionContext = context;

            context.Debug(StringUtil.Format(CodeCoverageConstants.EnablingEditingTemplate, "cobertura", "maven", ccInputs.BuildFile));

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
                context.Debug("This is a multi module project. Generating code coverage reports using ant task.");
            }

            // Add a build element if doesnot exist.
            var build = pomXml.Element(xNameSpace + "build");
            if (build == null)
            {
                pomXml.Add(new XElement(xNameSpace + "build"));
                build = pomXml.Element(xNameSpace + "build");
            }

            // Set cobertura plugins to enable code coverage.
            SetPlugins(build, ccInputs, xNameSpace);

            // Adding reporting enabler for cobertura
            var report = pomXml.Element(xNameSpace + "reporting");
            if (report == null)
            {
                pomXml.Add(new XElement(xNameSpace + "reporting"));
                report = pomXml.Element(xNameSpace + "reporting");
            }

            var pluginsInReport = report.Element(xNameSpace + _pluginsTag);
            if (pluginsInReport == null)
            {
                report.Add(new XElement(xNameSpace + _pluginsTag));
                pluginsInReport = report.Element(xNameSpace + _pluginsTag);
            }

            IList<XElement> pluginListInReport = pluginsInReport.Elements(xNameSpace + _pluginTag).ToList();

            foreach (var plugin in pluginListInReport.Where(plugin =>
            {
                var groupId = plugin.Element(xNameSpace + "groupId");
                var artifactId = plugin.Element(xNameSpace + "artifactId");
                return (artifactId != null && artifactId.Value == _coberturaArtifactId)
                    && (groupId != null && groupId.Value == _coberturaGroupId);
            }))
            {
                plugin.Parent.RemoveAll();
            }

            pluginsInReport.Add(GetDefaultReporting());

            foreach (var e in build.DescendantsAndSelf().Where(e => string.IsNullOrEmpty(e.Name.Namespace.NamespaceName)))
            {
                e.Name = xNameSpace + e.Name.LocalName;
            }

            foreach (var e in report.DescendantsAndSelf().Where(e => string.IsNullOrEmpty(e.Name.Namespace.NamespaceName)))
            {
                e.Name = xNameSpace + e.Name.LocalName;
            }

            using (FileStream stream = new FileStream(ccInputs.BuildFile, FileMode.OpenOrCreate))
            {
                pomXml.Save(stream);
            }
            context.Output(StringUtil.Loc("CodeCoverageEnabled", "cobertura", "maven"));
        }

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
                    SetPluginsForCobertura(mavenCcParams, pluginManagementplugins, xNameSpace, defaultConfigurationElement, defaultExecutions, false);
                }
            }

            var plugins = build.Element(xNameSpace + _pluginsTag);
            if (plugins == null)
            {
                build.Add(new XElement(xNameSpace + _pluginsTag));
                plugins = build.Element(xNameSpace + _pluginsTag);
            }
            SetPluginsForCobertura(mavenCcParams, plugins, xNameSpace, defaultConfigurationElement, defaultExecutions, true);
        }

        private void SetPluginsForCobertura(CodeCoverageEnablerInputs mavenCcParams, XElement plugins, XNamespace xNameSpace, XElement defaultConfigurationElement, XElement defaultExecutions, bool addIfNotExists)
        {
            IList<XElement> pluginList = plugins.Elements(xNameSpace + _pluginTag).ToList();
            var hasCoberturaPlugin = false;

            foreach (var plugin in pluginList.Where(plugin =>
            {
                var groupId = plugin.Element(xNameSpace + "groupId");
                var artifactId = plugin.Element(xNameSpace + "artifactId");
                return (artifactId != null && artifactId.Value == _coberturaArtifactId)
                    && (groupId != null && groupId.Value == _coberturaGroupId);
            }))
            {
                _executionContext.Debug("A cobertura plugin already exists. Adding/Editing the configuration values to match inputs provided in the task.");

                hasCoberturaPlugin = true;

                var versionElements = plugin.Elements(xNameSpace + "version").ToList();
                if (versionElements.Count == 0)
                {
                    // add version element if it is not there
                    plugin.SetElementValue(xNameSpace + "version", _coberturaVersion);
                }
                else
                {
                    _coberturaVersion = versionElements[0].Value;
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
            }

            if (addIfNotExists && !hasCoberturaPlugin)
            {
                _executionContext.Debug("Adding a cobertura plugin.");
                // if there is no cobertura plugin add one
                plugins.Add(GetDefaultCoberturaPlugin(defaultConfigurationElement, defaultExecutions));
            }
        }

        private object GetDefaultReporting()
        {
            return new XElement(_pluginTag,
                                 new XElement("groupId", _coberturaGroupId),
                                 new XElement("artifactId", _coberturaArtifactId),
                                 new XElement("version", _coberturaVersion),
                                 new XElement("configuration", GetFormatsElement())
                                 );
        }

        private XElement GetDefaultCoberturaPlugin(XElement defaultConfigurationElement, object defaultExecutions)
        {
            return new XElement(_pluginTag,
                    new XElement("groupId", _coberturaGroupId),
                    new XElement("artifactId", _coberturaArtifactId),
                    new XElement("version", _coberturaVersion),
                    defaultConfigurationElement,
                    defaultExecutions
                    );
        }

        private XElement GetDefaultExecutions()
        {
            // guid is used to make sure the element name is always unique
            return new XElement("executions",
                    GetDefaultExecution("package-" + Guid.NewGuid(), "cobertura", "package")
                );
        }

        private XElement GetDefaultExecution(string id, string goal, string phase = null)
        {
            var executionElement = new XElement("execution",
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

        private void AddConfigurationDefaults(XElement configuration, XNamespace xNameSpace, CodeCoverageEnablerInputs mavenCcParams)
        {
            var excludesElement = CodeCoverageUtilities.GetClassDataForMaven(mavenCcParams.Exclude, "excludes", "exclude");
            var includesElement = CodeCoverageUtilities.GetClassDataForMaven(mavenCcParams.Include, "includes", "include");

            var formatElement = GetFormatsElement();

            var instrumentationNode = configuration.Element(xNameSpace + "instrumentation");

            if (instrumentationNode != null)
            {
                // remove any existing excludes and includes. We cannot directly set the values here as the value is not one single object but multiple xelements
                instrumentationNode.Remove();
            }

            var formatsNode = configuration.Element(xNameSpace + "formats");

            if (formatsNode != null)
            {
                formatsNode.Remove();
            }

            //Adding formats  
            configuration.Add(formatElement);

            // add includes and excludes from user inputs
            configuration.Add(new XElement(xNameSpace + "instrumentation", includesElement, excludesElement));

            var aggregateNode = configuration.Element(xNameSpace + "aggregate");

            if (aggregateNode != null && _isMultiModule)
            {
                aggregateNode.Value = "true";
            }
            else if (_isMultiModule)
            {
                configuration.Add(new XElement(xNameSpace + "aggregate", "true"));
            }
        }

        private XElement GetFormatsElement()
        {
            var formatElement = new XElement("formats");

            formatElement.Add(new XElement("format", "xml"), new XElement("format", "html"));

            return formatElement;
        }
    }
}
