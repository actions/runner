using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.CodeCoverage
{
    public class CoberturaSummaryReaderTests
    {
        private Mock<IExecutionContext> _ec;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void VerifyCoberturaCoverageStatisticsForValidSummaryFile()
        {
            string coberturaXml = GetPathToValidCoberturaFile();
            try
            {
                SetupMocks();
                var summaryReader = new CoberturaSummaryReader();
                IEnumerable<CodeCoverageStatistics> coverageStats = summaryReader.GetCodeCoverageSummary(_ec.Object, coberturaXml);
                List<CodeCoverageStatistics> coverageStatsNew = coverageStats.ToList();
                coverageStatsNew.Sort(new Statscomparer());
                Assert.Equal(0, _errors.Count);
                Assert.Equal(0, _warnings.Count);
                VerifyLineCoverageStats(coverageStatsNew.ToList());
                VerifyBranchCoverageStats(coverageStatsNew.ToList());
            }
            finally
            {
                File.Delete(coberturaXml);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void VerifyFileDidnotExist()
        {
            SetupMocks();
            var coberturaXml = CoberturaFileDidnotExist();
            var summaryReader = new CoberturaSummaryReader();
            Assert.Throws<ArgumentException>(() => summaryReader.GetCodeCoverageSummary(_ec.Object, coberturaXml));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void VerifyInvalidXmlFile()
        {
            var invalidXml = CoberturaInvalidXmlFile();
            try
            {
                SetupMocks();
                var summaryReader = new CoberturaSummaryReader();
                summaryReader.GetCodeCoverageSummary(_ec.Object, invalidXml);
            }
            finally
            {
                File.Delete(invalidXml);
            }

            Assert.Equal(0, _errors.Count);
            Assert.Equal(1, _warnings.Count);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void VerifyEmptyXmlFile()
        {
            var emptyXml = GetEmptyCCFile();
            try
            {
                SetupMocks();
                var summaryReader = new CoberturaSummaryReader();
                Assert.Null(summaryReader.GetCodeCoverageSummary(_ec.Object, emptyXml));
                Assert.Equal(0, _errors.Count);
                Assert.Equal(0, _warnings.Count);
            }
            finally
            {
                File.Delete(emptyXml);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void VerifyWrongXmlFile()
        {
            var wrongXml = CoberturaWrongXmlFile();
            try
            {
                SetupMocks();
                var summaryReader = new CoberturaSummaryReader();
                var coverageStats = summaryReader.GetCodeCoverageSummary(_ec.Object, wrongXml);
                Assert.Equal(coverageStats.ToList().Count, 0);
                Assert.Equal(0, _errors.Count);
                Assert.Equal(0, _warnings.Count);
            }
            finally
            {
                File.Delete(wrongXml);
            }
        }

        private static string GetPathToValidCoberturaFile()
        {
            var file = Path.Combine(Path.GetTempPath(), "coberturaValid.xml");
            File.WriteAllText(file, CodeCoverageTestConstants.ValidCoberturaXml);
            return file;
        }

        private string CoberturaFileDidnotExist()
        {
            return Path.Combine(Path.GetTempPath(), "CoberturaDidNotExist.xml");
        }

        private string CoberturaInvalidXmlFile()
        {
            var file = Path.GetTempFileName();
            File.WriteAllText(file, "This is not XML File");
            return file;
        }

        private string GetEmptyCCFile()
        {
            return Path.GetTempFileName();
        }

        private string CoberturaWrongXmlFile()
        {
            var file = Path.GetTempFileName();
            File.WriteAllText(file, "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n<event>This is a Test</event>");
            return file;
        }

        private static void VerifyLineCoverageStats(List<CodeCoverageStatistics> coverageStats)
        {
            Assert.Equal(2, coverageStats.Count);

            Assert.Equal(4, (int)coverageStats[0].Position);
            Assert.Equal("lines", coverageStats[0].Label.ToLower());
            Assert.Equal(11, (int)coverageStats[0].Covered);
            Assert.Equal(22, (int)coverageStats[0].Total);
        }

        private static void VerifyBranchCoverageStats(List<CodeCoverageStatistics> coverageStats)
        {
            Assert.Equal(2, coverageStats.Count);
            Assert.Equal(6, (int)coverageStats[1].Position);
            Assert.Equal("branches", coverageStats[1].Label.ToLower());
            Assert.Equal(2, (int)coverageStats[1].Covered);
            Assert.Equal(8, (int)coverageStats[1].Total);
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            TestHostContext hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();
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
