using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public sealed class CoberturaSummaryReader : AgentService, ICodeCoverageSummaryReader
    {
        public Type ExtensionType => typeof(ICodeCoverageSummaryReader);
        public string Name => "Cobertura";

        private const string _linesCovered = "Lines";
        private const string _linesTotal = "Total Lines";
        private const string _branchesCovered = "Branches";
        private const string _branchesTotal = "Total Branches";
        private const string _linesCoveredTag = "lines-covered";
        private const string _branchesCoveredTag = "branches-covered";
        private const string _linesValidTag = "lines-valid";
        private const string _branchesValidTag = "branches-valid";

        public IEnumerable<CodeCoverageStatistics> GetCodeCoverageSummary(IExecutionContext context, string summaryXmlLocation)
        {
            var doc = CodeCoverageUtilities.ReadSummaryFile(context, summaryXmlLocation);

            return ReadDataFromNodes(doc, summaryXmlLocation);
        }

        private IEnumerable<CodeCoverageStatistics> ReadDataFromNodes(XmlDocument doc, string summaryXmlLocation)
        {
            var listCoverageStats = new List<CodeCoverageStatistics>();

            if (doc == null)
            {
                return null;
            }

            XmlNode reportNode = doc.SelectSingleNode("coverage");

            if (reportNode != null)
            {
                if (reportNode.Attributes != null)
                {
                    CodeCoverageStatistics coverageStatisticsForLines = GetCCStats(labelTag: _linesCovered, coveredTag: _linesCoveredTag, validTag: _linesValidTag,
                                                                                    priorityTag: "line", summaryXmlLocation: summaryXmlLocation, reportNode: reportNode);

                    if (coverageStatisticsForLines != null)
                    {
                        listCoverageStats.Add(coverageStatisticsForLines);
                    }

                    CodeCoverageStatistics coverageStatisticsForBranches = GetCCStats(labelTag: _branchesCovered, coveredTag: _branchesCoveredTag, validTag: _branchesValidTag,
                                                                                        priorityTag: "branch", summaryXmlLocation: summaryXmlLocation, reportNode: reportNode);

                    if (coverageStatisticsForBranches != null)
                    {
                        listCoverageStats.Add(coverageStatisticsForBranches);
                    }
                }
            }

            return listCoverageStats.AsEnumerable();
        }

        private CodeCoverageStatistics GetCCStats(string labelTag, string coveredTag, string validTag, string priorityTag, string summaryXmlLocation, XmlNode reportNode)
        {
            CodeCoverageStatistics coverageStatistics = null;

            if (reportNode.Attributes[coveredTag] != null && reportNode.Attributes[validTag] != null)
            {
                coverageStatistics = new CodeCoverageStatistics();
                coverageStatistics.Label = labelTag;
                coverageStatistics.Position = CodeCoverageUtilities.GetPriorityOrder(priorityTag);

                coverageStatistics.Covered = (int)ParseFromXML(coveredTag, summaryXmlLocation, reportNode);

                coverageStatistics.Total = (int)ParseFromXML(validTag, summaryXmlLocation, reportNode);
            }

            return coverageStatistics;
        }

        private float ParseFromXML(string parseTag, string summaryXmlLocation, XmlNode reportNode)
        {
            float value;
            if (!float.TryParse(reportNode.Attributes[parseTag].Value, out value))
            {
                throw new InvalidDataException(StringUtil.Loc("InvalidValueInXml", parseTag, summaryXmlLocation));
            }

            return value;
        }
    }
}