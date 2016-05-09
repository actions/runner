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
    public class JacocoSummaryReaderTests
    {
        private Mock<IExecutionContext> _ec;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        [Trait("DeploymentItem", "Jacoco.xml")]
        public void VerifyJacocoCoverageStatisticsForValidSummaryFile()
        {
            SetupMocks();
            var jacocoXml = GetPathToValidJaCoCoFile();
            try
            {
                JaCoCoSummaryReader summaryReader = new JaCoCoSummaryReader();
                var coverageStats = summaryReader.GetCodeCoverageSummary(_ec.Object, jacocoXml);
                var coverageStatsNew = coverageStats.ToList();
                coverageStatsNew.Sort(new Statscomparer());
                Assert.Equal(0, _errors.Count);
                Assert.Equal(0, _warnings.Count);
                VerifyCoverageStats(coverageStatsNew.ToList());
            }
            finally
            {
                File.Delete(jacocoXml);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void VerifyFileDidnotExist()
        {
            SetupMocks();
            var jacocoXml = JacocoFileDidnotExist();
            JaCoCoSummaryReader summaryReader = new JaCoCoSummaryReader();
            Assert.Throws<ArgumentException>(() => summaryReader.GetCodeCoverageSummary(_ec.Object, jacocoXml));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void VerifyInvalidXmlFile()
        {
            var invalidXml = JacocoInvalidXmlFile();
            var summaryReader = new JaCoCoSummaryReader();
            try
            {
                SetupMocks();
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
        public void VerifyWrongXmlFile()
        {
            var wrongXml = JacocoWrongXmlFile();
            var summaryReader = new JaCoCoSummaryReader();
            try
            {
                SetupMocks();
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishCodeCoverage")]
        public void VerifyEmptyXmlFile()
        {
            var emptyXml = GetEmptyCCFile();
            try
            {
                SetupMocks();
                var summaryReader = new JaCoCoSummaryReader();
                Assert.Null(summaryReader.GetCodeCoverageSummary(_ec.Object, emptyXml));
                Assert.Equal(0, _errors.Count);
                Assert.Equal(0, _warnings.Count);
            }
            finally
            {
                File.Delete(emptyXml);
            }
        }

        private string GetPathToValidJaCoCoFile()
        {
            var file = Path.Combine(Path.GetTempPath(), "jacocoValid.xml");
            File.WriteAllText(file, CodeCoverageConstants.ValidJacocoXml);
            return file;
        }

        private string JacocoFileDidnotExist()
        {
            return Path.Combine(Path.GetTempPath(), "CoberturaDidNotExist.xml");
        }

        private string JacocoInvalidXmlFile()
        {
            var file = Path.GetTempFileName();
            File.WriteAllText(file, "This is not XML File");
            return file;
        }

        private string JacocoWrongXmlFile()
        {
            var file = Path.GetTempFileName();
            File.WriteAllText(file, "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n<event>This is a Test</event>");
            return file;
        }

        private string GetEmptyCCFile()
        {
            return Path.GetTempFileName();
        }

        private static void VerifyCoverageStats(List<CodeCoverageStatistics> coverageStats)
        {
            Assert.Equal(5, coverageStats.Count);

            Assert.Equal(1, (int)coverageStats[0].Position);
            Assert.Equal("class", coverageStats[0].Label.ToLower());
            Assert.Equal(2, (int)coverageStats[0].Covered);
            Assert.Equal(2, (int)coverageStats[0].Total);

            Assert.Equal(2, (int)coverageStats[1].Position);
            Assert.Equal("complexity", coverageStats[1].Label.ToLower());
            Assert.Equal(2, (int)coverageStats[1].Covered);
            Assert.Equal(6, (int)coverageStats[1].Total);

            Assert.Equal(3, (int)coverageStats[2].Position);
            Assert.Equal("method", coverageStats[2].Label.ToLower());
            Assert.Equal(2, (int)coverageStats[2].Covered);
            Assert.Equal(6, (int)coverageStats[2].Total);

            Assert.Equal(4, (int)coverageStats[3].Position);
            Assert.Equal("line", coverageStats[3].Label.ToLower());
            Assert.Equal(2, (int)coverageStats[3].Covered);
            Assert.Equal(7, (int)coverageStats[3].Total);

            Assert.Equal(5, (int)coverageStats[4].Position);
            Assert.Equal("instruction", coverageStats[4].Label.ToLower());
            Assert.Equal(8, (int)coverageStats[4].Covered);
            Assert.Equal(22, (int)coverageStats[4].Total);
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

    public class Statscomparer : IComparer<CodeCoverageStatistics>
    {
        public int Compare(CodeCoverageStatistics x, CodeCoverageStatistics y)
        {
            return ((int)x.Position > (int)y.Position ? 1 : -1);
        }
    }
}
