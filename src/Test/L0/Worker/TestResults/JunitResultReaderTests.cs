using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.TestResults;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.TestResults
{
    public class JunitResultReaderTests : IDisposable
    {
        private Mock<IExecutionContext> _ec;
        private static JUnitResultReader _jUnitReader;
        private static TestRunData _testRunData;
        private static string _fileName;
        private static string _junitResultsToBeRead;

        private const string _onlyTestSuiteNode = "<?xml version =\'1.0\' encoding=\'UTF-8\'?>" +
"<testsuite errors =\"0\" failures=\"0\" hostname=\"IE11Win8_1\" name=\"Microsoft Comp\" tests=\"1\" time=\"5529.0\" timestamp=\"2015-11-17T16:35:38.756-08:00\" url=\"https://try.soasta.com/concerto/\">" +
  "<testcase classname =\"root\" name=\"Microsoft Comp\" resultID=\"27327\" time=\"5.529\" />" +
"</testsuite>";

        private const string _sampleJunitResultXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>"
            + "<testsuite errors = \"0\" failures=\"0\" hostname=\"achalla-dev\" name=\"test.AllTests\" skipped=\"0\" tests=\"1\" time=\"0.03\" timestamp=\"2015-09-01T10:19:04\">"
            + "<properties>"
            + "<property name = \"java.vendor\" value=\"Oracle Corporation\" />"
            + "<property name = \"lib.dir\" value=\"lib\" />"
            + "<property name = \"sun.java.launcher\" value=\"SUN_STANDARD\" />"
            + "</properties>"
            + "<testcase classname = \"test.ExampleTest\" name=\"Fact\" time=\"0.001\" />"
            + "<system-out><![CDATA[Set Up Complete."
            + "Sample test Successful"
            + "]]></system-out>"
            + "<system-err><![CDATA[]]></system-err>"
            + "</testsuite>";

        private const string _jUnitBasicResultsWithLogsXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<testsuite errors=\"0\" failures=\"1\" hostname=\"mghost\" name=\"com.contoso.billingservice.ConsoleMessageRendererTest\" skipped=\"0\" tests=\"2\" time=\"0.006\" timestamp=\"2015-04-06T21:56:24\">" +
              "<testcase classname=\"com.contoso.billingservice.ConsoleMessageRendererTest\" name=\"testRenderMessage\" time=\"0.003\">" +
                "<failure type=\"junit.framework.AssertionFailedError\">junit.framework.AssertionFailedError at com.contoso.billingservice.ConsoleMessageRendererTest.testRenderMessage(ConsoleMessageRendererTest.java:11)" +
                "</failure>" +
                "<system-out><![CDATA[system out...]]></system-out>" +
                "<system-err><![CDATA[system err...]]></system-err>" +
              "</testcase >" +
            "</testsuite>";

        private const string _jUnitBasicResultsXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<testsuite errors=\"0\" failures=\"1\" hostname=\"mghost\" name=\"com.contoso.billingservice.ConsoleMessageRendererTest\" skipped=\"0\" tests=\"2\" time=\"0.006\" timestamp=\"2015-04-06T21:56:24\">" +
              "<testcase classname=\"com.contoso.billingservice.ConsoleMessageRendererTest\" name=\"testRenderNullMessage\" testType=\"asdasdas\" time=\"0.001\" />" +
              "<testcase classname=\"com.contoso.billingservice.ConsoleMessageRendererTest\" name=\"testRenderMessage\" time=\"0.003\">" +
                "<failure type=\"junit.framework.AssertionFailedError\">junit.framework.AssertionFailedError at com.contoso.billingservice.ConsoleMessageRendererTest.testRenderMessage(ConsoleMessageRendererTest.java:11)" +
                "</failure>" +
              "</testcase >" +
              "<system-out><![CDATA[Hello World!]]>" +
              "</system-out>" +
              "<system-err><![CDATA[]]></system-err>" +
            "</testsuite>";

        private const string _jUnitMultiSuiteXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<testsuites>" +
            "<testsuite errors=\"0\" failures=\"1\" hostname=\"mghost\" name=\"com.contoso.billingservice.ConsoleMessageRendererTest\" skipped=\"0\" tests=\"2\" time=\"1.006\" timestamp=\"2015-04-06T21:56:24\">" +
              "<testcase classname=\"com.contoso.billingservice.ConsoleMessageRendererTest\" name=\"testRenderNullMessage\" time=\"1.001\" />" +
            "</testsuite>" +
            "<testsuite errors=\"0\" failures=\"1\" hostname=\"mghost\" name=\"com.contoso.billingservice.ConsoleMessageRendererTest1\" skipped=\"0\" tests=\"2\" time=\"1.006\" timestamp=\"2015-04-06T21:56:24\">" +
              "<testcase classname=\"com.contoso.billingservice.ConsoleMessageRendererTest\" name=\"testRenderNullMessage\" time=\"1.001\" />" +
            "</testsuite>" +
            "</testsuites>";

        private const string c_jUnitMultiSuiteParallelXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<testsuites>" +
            "<testsuite errors=\"0\" failures=\"1\" hostname=\"mghost\" name=\"com.contoso.billingservice.ConsoleMessageRendererTest\" skipped=\"0\" tests=\"2\" time=\"1.006\" timestamp=\"2015-04-06T21:56:24\">" +
              "<testcase classname=\"com.contoso.billingservice.ConsoleMessageRendererTest\" name=\"testRenderNullMessage\" time=\"1.001\" />" +
            "</testsuite>" +
            "<testsuite errors=\"0\" failures=\"1\" hostname=\"mghost\" name=\"com.contoso.billingservice.ConsoleMessageRendererTest1\" skipped=\"0\" tests=\"2\" time=\"3.058\" timestamp=\"2015-04-06T21:56:25\">" +
              "<testcase classname=\"com.contoso.billingservice.ConsoleMessageRendererTest\" name=\"testRenderNullMessage\" time=\"3.003\" />" +
            "</testsuite>" +
            "<testsuite errors=\"0\" failures=\"1\" hostname=\"mghost\" name=\"com.contoso.billingservice.ConsoleMessageRendererTest1\" skipped=\"0\" tests=\"2\" time=\"2.016\" timestamp=\"2015-04-06T21:56:25\">" +
              "<testcase classname=\"com.contoso.billingservice.ConsoleMessageRendererTest\" name=\"testRenderNullMessage\" time=\"2.002\" />" +
            "</testsuite>" +
            "</testsuites>";

        private const string _jUnitKarmaResultsXml =
            "<?xml version =\"1.0\"?>" +
            "<testsuites>" +
              "<testsuite name =\"PhantomJS 2.0.0 (Windows 8)\" package=\"\" timestamp=\"2015-05-22T12:56:58\" id=\"0\" hostname=\"nirvana\" tests=\"56\" errors=\"0\" failures=\"0\" time=\"0.394\">" +
              "<properties>" +
                "<property name =\"browser.fullName\" value=\"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/538.1 (KHTML, like Gecko) PhantomJS/2.0.0 (development) Safari/538.1\"/>" +
              "</properties>" +
                "<testcase name =\"can be instantiated\" time=\"0.01\" classname=\"PhantomJS 2.0.0 (Windows 8).the Admin module\"/>" +
                "<testcase name =\"configures the router\" time=\"0.01\" classname=\"PhantomJS 2.0.0 (Windows 8).the Admin module\"/>" +
                "<testcase name =\"should activate correctly\" time=\"0.03\" classname=\"PhantomJS 2.0.0 (Windows 8).the Admin module\"/>" +
                "<testcase name =\"can be instantiated\" time=\"0.007\" classname=\"PhantomJS 2.0.0 (Windows 8).the App module\"/>" +
                "<testcase name =\"registers dependencies on session router and reportRouter\" time=\"0.001\" classname=\"PhantomJS 2.0.0 (Windows 8).the App module\"/>" +
                "<testcase name =\"configures the router\" time=\"0.009\" classname=\"PhantomJS 2.0.0 (Windows 8).the App module\"/>" +
                "<system-out><![CDATA[]]></system-out>" +
                "<system-err/>" +
              "</testsuite>" +
            "</testsuites>";

        private const string _jUnitBasicResultsXmlWithoutMandatoryFields =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<testsuite errors=\"0\" failures=\"1\" hostname=\"mghost\" name=\"com.contoso.billingservice.ConsoleMessageRendererTest\" skipped=\"0\" tests=\"2\" time=\"0.006\" timestamp=\"2015-04-06T21:56:24\">" +
              "<testcase classname=\"com.contoso.billingservice.ConsoleMessageRendererTest\" time=\"0.001\" />" +
              "<testcase classname=\"com.contoso.billingservice.ConsoleMessageRendererTest\" name=\"\" time=\"0.001\" />" +
            "</testsuite>";

        private const string _sampleJunitResultXmlWithDtd = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>"
            + "<!DOCTYPE report PUBLIC '-//JACOCO//DTD Report 1.0//EN' 'report.dtd'>"
            + "<testsuite errors = \"0\" failures=\"0\" hostname=\"achalla-dev\" name=\"test.AllTests\" skipped=\"0\" tests=\"1\" time=\"0.03\" timestamp=\"2015-09-01T10:19:04\">"
            + "<properties>"
            + "<property name = \"java.vendor\" value=\"Oracle Corporation\" />"
            + "<property name = \"lib.dir\" value=\"lib\" />"
            + "<property name = \"sun.java.launcher\" value=\"SUN_STANDARD\" />"
            + "</properties>"
            + "<testcase classname = \"test.ExampleTest\" name=\"Fact\" time=\"0.001\" />"
            + "<system-out><![CDATA[Set Up Complete."
            + "Sample test Successful"
            + "]]></system-out>"
            + "<system-err><![CDATA[]]></system-err>"
            + "</testsuite>";

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ResultsWithoutMandatoryFieldsAreSkipped()
        {
            SetupMocks();
            _junitResultsToBeRead = _jUnitBasicResultsXmlWithoutMandatoryFields;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(0, _testRunData.Results.Length);
        }


        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestRunDurationWhenTotalTimeIsInMilliseconds()
        {
            SetupMocks();
            _junitResultsToBeRead = _sampleJunitResultXml;
            ReadResults();

            var timeSpan = DateTime.Parse(_testRunData.CompleteDate) - DateTime.Parse(_testRunData.StartDate);
            Assert.Equal(0.03, timeSpan.TotalSeconds);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestCaseDurationWhenTestCaseTimeIsInMilliseconds()
        {
            SetupMocks();
            _junitResultsToBeRead = _sampleJunitResultXml;
            ReadResults();

            var time = TimeSpan.FromMilliseconds(_testRunData.Results[0].DurationInMs);
            Assert.Equal(0.001, time.TotalSeconds);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyDurationAsDiffBetweenMinStartAndMaxCompletedDate()
        {
            SetupMocks();
            // case for duration = maxcompletedtime- minstarttime
            _junitResultsToBeRead = c_jUnitMultiSuiteParallelXml;
            ReadResults();
            var timeSpan = DateTime.Parse(_testRunData.CompleteDate) - DateTime.Parse(_testRunData.StartDate);
            Assert.Equal(4.058, timeSpan.TotalSeconds);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyDurationAsSumOfAssemblyTimeWhenTimestampNotParseable()
        {
            SetupMocks();
            //  time stamp parsing failure: case for duration = summation of assembly run time
            _junitResultsToBeRead = c_jUnitMultiSuiteParallelXml.Replace("timestamp=\"2015-04-06T21:56:24\"", "timestamp =\"5-04ggg-06T21:56:24\"");
            ReadResults();
            var timeSpan = DateTime.Parse(_testRunData.CompleteDate) - DateTime.Parse(_testRunData.StartDate);
            Assert.Equal(6.08, timeSpan.TotalSeconds);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyDurationAsSumOfAssemblyTimeWhenTimestampTagNotAvailable()
        {
            SetupMocks();
            //  timestamp attribute not present: case for duration = summation of assembly run time
            _junitResultsToBeRead = c_jUnitMultiSuiteParallelXml.Replace("timestamp", "ts");
            ReadResults();
            var timeSpan = DateTime.Parse(_testRunData.CompleteDate) - DateTime.Parse(_testRunData.StartDate);
            Assert.Equal(6.08, timeSpan.TotalSeconds);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyDurationAsSumOfTestCaseTimeWhenTestSuiteTimeTagNotAvailable()
        {
            SetupMocks();
            //  timestamp and time attribute not present in test suite tag : case for duration = summation of test cases run time
            _junitResultsToBeRead = c_jUnitMultiSuiteParallelXml.Replace("timestamp", "ts").Replace("time=\"2.016\"", "tm =\"2.016\"");
            ReadResults();
            var timeSpan = DateTime.Parse(_testRunData.CompleteDate) - DateTime.Parse(_testRunData.StartDate);
            Assert.Equal(6.006, Math.Round(timeSpan.TotalSeconds, 3));
        }


        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyDurationsCalculationsInDifferentCulture()
        {
            SetupMocks();
            CultureInfo current = CultureInfo.CurrentCulture;
            try
            {
                //German is used, as in this culture decimal separator is comma & thousand separator is dot
                CultureInfo.CurrentCulture = new CultureInfo("de-De");
                //verify test case start date
                _junitResultsToBeRead = _sampleJunitResultXml;
                ReadResults();
                Assert.Equal(_testRunData.StartDate, _testRunData.Results[0].StartedDate.ToString("o"));

                //verify test case completed date
                _junitResultsToBeRead = _sampleJunitResultXml;
                ReadResults();
                Assert.Equal(_testRunData.StartDate, _testRunData.Results[0].StartedDate.ToString("o"));

                //verify duration as difference between min start time and max completed time
                _junitResultsToBeRead = _jUnitKarmaResultsXml;
                ReadResults();
                var testCase1CompletedDate = _testRunData.Results[0].CompletedDate;
                var testCase2StartDate = _testRunData.Results[1].StartedDate;
                Assert.True(testCase1CompletedDate <= testCase2StartDate, "first test case end should be before second test case start time");

                //verify duration as sum of assembly time when timestamp tag is not available
                _junitResultsToBeRead = c_jUnitMultiSuiteParallelXml.Replace("timestamp", "ts");
                ReadResults();
                var timeSpan = DateTime.Parse(_testRunData.CompleteDate) - DateTime.Parse(_testRunData.StartDate);
                Assert.Equal(6.08, timeSpan.TotalSeconds);

                //verify duration as sum of test case time when test suite time tag is not available
                _junitResultsToBeRead = c_jUnitMultiSuiteParallelXml.Replace("timestamp", "ts").Replace("time=\"2.016\"", "tm =\"2.016\"");
                ReadResults();
                timeSpan = DateTime.Parse(_testRunData.CompleteDate) - DateTime.Parse(_testRunData.StartDate);
                Assert.Equal(6.006, Math.Round(timeSpan.TotalSeconds, 3));
            }
            finally
            {
                CultureInfo.CurrentCulture = current;
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishBasicJUnitResults()
        {
            SetupMocks();
            _junitResultsToBeRead = _jUnitBasicResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(2, _testRunData.Results.Length);
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("Passed")));
            Assert.Equal(null, _testRunData.Results[0].AutomatedTestId);
            Assert.Equal(null, _testRunData.Results[0].AutomatedTestTypeId);
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("Failed")));
            Assert.Equal("com.contoso.billingservice.ConsoleMessageRendererTest", _testRunData.Name);

            Assert.Equal("releaseUri", _testRunData.ReleaseUri);
            Assert.Equal("releaseEnvironmentUri", _testRunData.ReleaseEnvironmentUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyPublishingStandardLogs()
        {
            SetupMocks();
            _junitResultsToBeRead = _jUnitBasicResultsWithLogsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(1, _testRunData.Results.Length);
            Assert.Equal("system out...", _testRunData.Results[0].ConsoleLog);
            Assert.Equal("system err...", _testRunData.Results[0].StandardError);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishBasicJUnitResultsAddsResultsFileByDefault()
        {
            SetupMocks();
            _junitResultsToBeRead = _jUnitBasicResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.True(_testRunData.Attachments.Length == 1, "the run level attachment is not present");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishBasicJUnitResultsSkipsAddingResultsFileWhenFlagged()
        {
            SetupMocks();
            _junitResultsToBeRead = _jUnitBasicResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"), false);
            Assert.True(_testRunData.Attachments.Length == 0, "the run level attachment is present even though the flag was set to false");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void CustomRunTitleIsHonoured()
        {
            SetupMocks();
            _junitResultsToBeRead = _jUnitBasicResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri", "MyRunTitle"));

            Assert.NotNull(_testRunData);
            Assert.Equal("MyRunTitle", _testRunData.Name);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void DefaultRunTitleIsHonouredForMultiSuiteRuns()
        {
            SetupMocks();
            _junitResultsToBeRead = _jUnitMultiSuiteXml;
            ReadResults();

            Assert.NotNull(_testRunData);
            Assert.Equal("JUnit_" + Path.GetFileName(_fileName), _testRunData.Name);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishJUnitKarmaResultFile()
        {
            SetupMocks();
            _junitResultsToBeRead = _jUnitKarmaResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(6, _testRunData.Results.Length);
            Assert.Equal(6, _testRunData.Results.Count(r => r.Outcome.Equals("Passed")));
            Assert.Equal(0, _testRunData.Results.Count(r => r.Outcome.Equals("Failed")));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyRunTypeIsSet()
        {
            SetupMocks();
            _junitResultsToBeRead = _jUnitKarmaResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
            Assert.Equal(_jUnitReader.Name, _testRunData.Results[0].AutomatedTestType);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestCaseStartDate()
        {
            SetupMocks();
            _junitResultsToBeRead = _sampleJunitResultXml;
            ReadResults();
            Assert.Equal(_testRunData.StartDate, _testRunData.Results[0].StartedDate.ToString("o"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestCaseCompletedDate()
        {
            SetupMocks();
            _junitResultsToBeRead = _jUnitKarmaResultsXml;
            ReadResults();
            var testCase1CompletedDate = _testRunData.Results[0].CompletedDate;
            var testCase2StartDate = _testRunData.Results[1].StartedDate;
            Assert.True(testCase1CompletedDate <= testCase2StartDate, "first test case end should be before second test case start time");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyLastTestCaseEndDateNotGreaterThanTestRunTotalTime()
        {
            SetupMocks();
            _junitResultsToBeRead = _sampleJunitResultXml;
            ReadResults();
            var testCaseCompletedDate = _testRunData.Results[0].CompletedDate;
            var testRunCompletedDate = _testRunData.Results[0].StartedDate.AddTicks(DateTime.Parse(_testRunData.CompleteDate).Ticks);
            Assert.True(testCaseCompletedDate <= testRunCompletedDate, "first test case end should be within test run completed time");

        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyWithJustTestSuite()
        {
            SetupMocks();
            _junitResultsToBeRead = _onlyTestSuiteNode;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri",
                "releaseEnvironmentUri"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Junit_DtdProhibitedXmlShouldReturnNull()
        {
            SetupMocks();
            _junitResultsToBeRead = _sampleJunitResultXmlWithDtd;
            ReadResults();
            Assert.NotNull(_testRunData);
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            TestHostContext hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();
            List<string> warnings;
            var variables = new Variables(hc, new Dictionary<string, VariableValue>(), out warnings);
            _ec.Setup(x => x.Variables).Returns(variables);
        }

        private void ReadResults(TestRunContext runContext = null, bool attachRunLevelAttachments = true)
        {
            _fileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            File.WriteAllText(_fileName, _junitResultsToBeRead);

            _jUnitReader = new JUnitResultReader();
            _jUnitReader.AddResultsFileToRunLevelAttachments = attachRunLevelAttachments;
            _testRunData = _jUnitReader.ReadResults(_ec.Object, _fileName, runContext);
        }

        public void Dispose()
        {
            _jUnitReader.AddResultsFileToRunLevelAttachments = true;
            try
            {
                File.Delete(_fileName);
            }
            catch
            {

            }
        }
    }
}