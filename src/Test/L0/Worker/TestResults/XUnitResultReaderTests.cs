using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.TestResults;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.TestResults
{
    public class XUnitResultReaderTests : IDisposable
    {
        private string _xUnitResultFile;
        private XUnitResultReader _xUnitReader;
        private Mock<IExecutionContext> _ec;

        private const string _xunitResultsFull = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<assemblies>" +
        "<assembly name=\"C:\\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\bin\\Debug\\ClassLibrary2.DLL\" environment=\"64-bit .NET 4.0.30319.42000 [collection-per-class, parallel]\" test-framework=\"xUnit.net 2.0.0.2929\" run-date=\"2015-08-18\" run-time=\"06:17:15\" config-file=\"C:\\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\packages\\xunit.runner.console.2.0.0\\tools\\xunit.console.exe.Config\" total=\"2\" passed=\"1\" failed=\"1\" skipped=\"0\" time=\"0.233\" errors=\"0\">" +
        "<errors />" +
        "<collection total=\"2\" passed=\"1\" failed=\"1\" skipped=\"0\" name=\"Test collection for MyFirstUnitTests.Class1\" time=\"0.044\">" +
        "<test name=\"MyFirstUnitTests.Class1.FailingTest\" type=\"MyFirstUnitTests.Class1\" method=\"FailingTest\" time=\"0.0422319\" result=\"Fail\">" +
        "<failure exception-type=\"Xunit.Sdk.EqualException\" >" +
        "<message><![CDATA[Assert.Equal() Failure" +
        "Expected: 5" +
        "Actual: 4]]></message >" +
        "<stack-trace><![CDATA[at MyFirstUnitTests.Class1.FailingTest() in C: \\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\Class1.cs:line 17]]></stack-trace>" +
        "</failure >" +
        "</test>" +
        "<test name=\"MyFirstUnitTests.Class1.PassingTest\" type=\"MyFirstUnitTests.Class1\" method=\"PassingTest\" time=\"0.0014079\" result=\"Pass\">" +
        "<traits>" +
        "<trait name=\"priority\" value=\"0\" />" +
        "<trait name=\"owner\" value=\"asdf\" />" +
        "</traits>" +
        "</test>" +
        "</collection>" +
        "</assembly>" +
        "<assembly name=\"C:\\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary1\\bin\\Debug\\ClassLibrary1.DLL\" environment=\"64-bit .NET 4.0.30319.42000 [collection-per-class, parallel]\" test-framework=\"xUnit.net 2.0.0.2929\" run-date=\"2015-08-18\" run-time=\"06:17:16\" config-file=\"C:\\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\packages\\xunit.runner.console.2.0.0\\tools\\xunit.console.exe.Config\" total=\"2\" passed=\"2\" failed=\"0\" skipped=\"0\" time=\"0.152\" errors=\"0\">" +
        "<errors />" +
        "<collection total=\"2\" passed=\"2\" failed=\"0\" skipped=\"0\" name=\"Test collection for MyFirstUnitTests.Class2\" time=\"0.006\">" +
        "<test name=\"MyFirstUnitTests.Class2.tset2\" type=\"MyFirstUnitTests.Class2\" method=\"tset2\" time=\"0.0056931\" result=\"Pass\" />" +
        "<test name=\"MyFirstUnitTests.Class2.test1\" type=\"MyFirstUnitTests.Class2\" method=\"test1\" time=\"0.0001093\" result=\"Pass\">" +
        "<traits>" +
        "<trait name=\"priority\" value=\"0\" />" +
        "</traits>" +
        "</test>" +
        "</collection>" +
        "</assembly>" +
        "</assemblies>";

        private const string _xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<assemblies>" +
        "<assembly name = \"C:\\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\bin\\Debug\\ClassLibrary2.DLL\">" +
        "<class name=\"MyFirstUnitTests.Class1\">" +
        "<test name=\"MyFirstUnitTests.Class1.FailingTest\">" +
        "</test>" +
        "</class>" +
        "</assembly>" +
        "</assemblies>";

        [Fact]
        [Trait("Level", "L0")]
        public void ResultsWithoutMandatoryFieldsAreSkipped()
        {
            SetupMocks();
            string xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                  "<assemblies>" +
                                  "<assembly>" +
                                  "<collection>" +
                                  "<test name=\"\">" +
                                  "</test>" +
                                  "<test>" +
                                  "</test>" +
                                  "</collection>" +
                                  "</assembly>" +
                                  "</assemblies>";
            WriteXUnitFile(xunitResults);
            XUnitResultReader reader = new XUnitResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(runData);
            Assert.Equal(0, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void ReadResultsReturnsCorrectValues()
        {
            SetupMocks();
            string xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<assemblies>" +
            "<assembly name=\"C:\\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\bin\\Debug\\ClassLibrary2.DLL\" environment=\"64-bit .NET 4.0.30319.42000 [collection-per-class, parallel]\" test-framework=\"xUnit.net 2.0.0.2929\" run-date=\"2015-08-18\" run-time=\"06:17:15\" config-file=\"C:\\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\packages\\xunit.runner.console.2.0.0\\tools\\xunit.console.exe.Config\" total=\"2\" passed=\"1\" failed=\"1\" skipped=\"0\" time=\"0.233\" errors=\"0\">" +
            "<errors />" +
            "<collection total=\"2\" passed=\"1\" failed=\"1\" skipped=\"0\" name=\"Test collection for MyFirstUnitTests.Class1\" time=\"0.044\">" +
            "<test name=\"MyFirstUnitTests.Class1.FailingTest\" type=\"MyFirstUnitTests.Class1\" method=\"FailingTest\" time=\"1.0422319\" result=\"Fail\">" +
            "<failure exception-type=\"Xunit.Sdk.EqualException\" >" +
            "<message><![CDATA[Assert.Equal() Failure" +
            "Expected: 5" +
            "Actual: 4]]></message >" +
            "<stack-trace><![CDATA[at MyFirstUnitTests.Class1.FailingTest() in C: \\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\Class1.cs:line 17]]></stack-trace>" +
            "</failure >" +
            "</test>" +
            "<test name=\"MyFirstUnitTests.Class1.PassingTest\" type=\"MyFirstUnitTests.Class1\" method=\"PassingTest\" time=\"0.0014079\" result=\"Pass\">" +
            "<traits>" +
            "<trait name=\"priority\" value=\"0\" />" +
            "<trait name=\"owner\" value=\"asdf\" />" +
            "</traits>" +
            "</test>" +
            "</collection>" +
            "</assembly>" +
            "<assembly name=\"C:\\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary1\\bin\\Debug\\ClassLibrary1.DLL\" environment=\"64-bit .NET 4.0.30319.42000 [collection-per-class, parallel]\" test-framework=\"xUnit.net 2.0.0.2929\" run-date=\"2015-08-18\" run-time=\"06:17:16\" config-file=\"C:\\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\packages\\xunit.runner.console.2.0.0\\tools\\xunit.console.exe.Config\" total=\"2\" passed=\"2\" failed=\"0\" skipped=\"0\" time=\"0.152\" errors=\"0\">" +
            "<errors />" +
            "<collection total=\"2\" passed=\"2\" failed=\"0\" skipped=\"0\" name=\"Test collection for MyFirstUnitTests.Class2\" time=\"0.006\">" +
            "<test name=\"MyFirstUnitTests.Class2.tset2\" type=\"MyFirstUnitTests.Class2\" method=\"tset2\" time=\"0.0056931\" result=\"Pass\" />" +
            "<test name=\"MyFirstUnitTests.Class2.test1\" type=\"MyFirstUnitTests.Class2\" method=\"test1\" time=\"0.0001093\" result=\"Pass\">" +
            "<traits>" +
            "<trait name=\"priority\" value=\"0\" />" +
            "</traits>" +
            "</test>" +
            "</collection>" +
            "</assembly>" +
            "</assemblies>";

            _xUnitResultFile = "XUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);

            XUnitResultReader reader = new XUnitResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));

            Assert.Equal("XUnit Test Run debug any cpu", runData.Name);
            Assert.Equal(4, runData.Results.Length);
            Assert.Equal("debug", runData.BuildFlavor);
            Assert.Equal("any cpu", runData.BuildPlatform);
            Assert.Equal("1", runData.Build.Id);
            Assert.Equal(1, runData.Attachments.Length);

            Assert.Equal("Failed", runData.Results[0].Outcome);
            Assert.Equal("FailingTest", runData.Results[0].TestCaseTitle);
            Assert.Equal("MyFirstUnitTests.Class1.FailingTest", runData.Results[0].AutomatedTestName);
            Assert.Equal("Assert.Equal() FailureExpected: 5Actual: 4", runData.Results[0].ErrorMessage);
            Assert.Equal("at MyFirstUnitTests.Class1.FailingTest() in C: \\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\Class1.cs:line 17", runData.Results[0].StackTrace);
            Assert.Equal("Owner", runData.Results[0].RunBy.DisplayName);
            Assert.Equal("Completed", runData.Results[0].State);
            Assert.Equal("1042", runData.Results[0].DurationInMs);
            Assert.Equal("ClassLibrary2.DLL", runData.Results[0].AutomatedTestStorage);


            Assert.Equal("Passed", runData.Results[1].Outcome);
            Assert.Equal("0", runData.Results[1].TestCasePriority);
            Assert.Equal("asdf", runData.Results[1].Owner.DisplayName);

            Assert.Equal(null, runData.Results[0].AutomatedTestId);
            Assert.Equal(null, runData.Results[0].AutomatedTestTypeId);

            double runDuration = 0;
            foreach (TestCaseResultData result in runData.Results)
            {
                double resultDuration;
                double.TryParse(result.DurationInMs, out resultDuration);
                runDuration += resultDuration;
            }

            DateTime startDate;
            DateTime.TryParse(runData.StartDate, out startDate);
            DateTime completeDate;
            DateTime.TryParse(runData.CompleteDate, out completeDate);
            TimeSpan duration = completeDate - startDate;
            Assert.Equal((completeDate - startDate).TotalMilliseconds, runDuration);

            Assert.Equal("releaseUri", runData.ReleaseUri);
            Assert.Equal("releaseEnvironmentUri", runData.ReleaseEnvironmentUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void CustomRunTitleIsHonoured()
        {
            SetupMocks();
            var runData = ReadResults();

            Assert.Equal("My Run Title", runData.Name);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void ReadResultsDoesNotFailForV1()
        {
            SetupMocks();
            string xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<assemblies>" +
            "<assembly name = \"C:\\Users\\kaadhina\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\bin\\Debug\\ClassLibrary2.DLL\">" +
            "<class name=\"MyFirstUnitTests.Class1\">" +
            "<test name=\"MyFirstUnitTests.Class1.FailingTest\">" +
            "</test>" +
            "</class>" +
            "</assembly>" +
            "</assemblies>";

            _xUnitResultFile = "BadXUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);

            XUnitResultReader reader = new XUnitResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext(null, null, null, 0, null, null, null));

            Assert.Equal(0, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void ReadResultsDoesNotFailForBadXml()
        {
            SetupMocks();
            string xunitResults = "<random>" +
            "</random>";

            _xUnitResultFile = "BadXUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);

            XUnitResultReader reader = new XUnitResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext(null, null, null, 0, null, null, null));

            Assert.Equal(0, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void ReadResultsDoesNotFailForBareMinimumXml()
        {
            SetupMocks();
            var runData = GetTestRunData();
            Assert.Equal(1, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void VerifyRunTypeIsSet()
        {
            SetupMocks();
            var runData = GetTestRunData();
            Assert.Equal(_xUnitReader.Name, runData.Results[0].AutomatedTestType);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void PublishBasicXUnitResultsAddsResultsFileByDefault()
        {
            SetupMocks();
            var runData = ReadResults();

            Assert.Equal(1, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void PublishBasicXUnitResultsSkipsAddingResultsFileWhenFlagged()
        {
            SetupMocks();
            var runData = ReadResults(false);

            Assert.Equal(0, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void VerifyTestCaseStartDate()
        {
            SetupMocks();
            var runData = ReadResults(true, true);
            Assert.Equal(runData.StartDate, runData.Results[0].StartedDate);

        }

        [Fact]
        [Trait("Level", "L0")]
        public void VerifyTestCaseCompletedDate()
        {
            SetupMocks();
            var runData = ReadResults(true, true);
            var testCase1CompletedDate = DateTime.Parse(runData.Results[0].CompletedDate);
            var testCase2StartDate = DateTime.Parse(runData.Results[1].StartedDate);
            Assert.True(testCase1CompletedDate <= testCase2StartDate, "first test case end should be before second test case start time");

        }

        [Fact]
        [Trait("Level", "L0")]
        public void VerifyLastTestCaseEndDateNotGreaterThanTestRunTotalTime()
        {
            SetupMocks();
            var runData = ReadResults(true, true);
            var testCaseCompletedDate = DateTime.Parse(runData.Results[0].CompletedDate);
            var testRunCompletedDate = DateTime.Parse(runData.Results[0].StartedDate).AddTicks(DateTime.Parse(runData.CompleteDate).Ticks);
            Assert.True(testCaseCompletedDate <= testRunCompletedDate, "first test case end should be within test run completed time");

        }

        [Fact]
        [Trait("Level", "L0")]
        public void VerifyStartDateIsEmptyWhenNoRunTimeIsAvailable()
        {
            SetupMocks();
            var resultsWithNoTime = _xunitResultsFull.Replace("run-time", "timer").Replace("run-date", "dater");
            var testRunData = ReadResults(resultsWithNoTime);
            Assert.Equal(string.Empty, testRunData.StartDate);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void VerifyEndDateIsEmptyWhenNoRunTimeIsAvailable()
        {
            SetupMocks();
            var resultsWithNoTime = _xunitResultsFull.Replace("run-time", "timer").Replace("run-date", "dater");
            var testRunData = ReadResults(resultsWithNoTime);
            Assert.Equal(string.Empty, testRunData.CompleteDate);
        }

        [Fact]
        [Trait("Level", "L0")]
        public void Xunit_DtdProhibitedXmlShouldReturnNull()
        {
            SetupMocks();
            var testRunData = ReadResults(Utilities.dtdInvalidXml);

            var exceptionMessage = Utilities.GetDtdExceptionMessage("BadXUnitResults.xml");

            Assert.Null(testRunData);
        }

        public void Dispose()
        {
            try
            {
                File.Delete(_xUnitResultFile);
            }
            catch
            {
            }
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            TestHostContext hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();
            List<string> warnings;
            var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
            _ec.Setup(x => x.Variables).Returns(variables);
        }

        private TestRunData GetTestRunData()
        {
            string xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                  "<assemblies>" +
                                  "<assembly>" +
                                  "<collection>" +
                                  "<test name=\"MyFirstUnitTests.Class1.Test1\" method=\"asdasdf\">" +
                                  "</test>" +
                                  "</collection>" +
                                  "</assembly>" +
                                  "</assemblies>";

            _xUnitResultFile = "BareMinimumXUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);

            _xUnitReader = new XUnitResultReader();
            TestRunData runData = _xUnitReader.ReadResults(_ec.Object, _xUnitResultFile,
                new TestRunContext(null, null, null, 0, null, null, null));
            return runData;
        }

        private TestRunData ReadResults(string content)
        {
            WriteXUnitFile(content);
            return ReadResultsInternal(false);
        }

        private TestRunData ReadResults(bool attachRunLevelAttachments = true, bool fullResults = false)
        {
            WriteXUnitFile(fullResults ? _xunitResultsFull : _xunitResults);

            return ReadResultsInternal(attachRunLevelAttachments);
        }

        private void WriteXUnitFile(string content)
        {
            _xUnitResultFile = "BadXUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, content);
        }

        private TestRunData ReadResultsInternal(bool attachRunLevelAttachments = true)
        {
            XUnitResultReader reader = new XUnitResultReader();
            reader.AddResultsFileToRunLevelAttachments = attachRunLevelAttachments;
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile,
                new TestRunContext(null, "any cpu", "debug", 0, null, null, null, "My Run Title"));
            return runData;
        }
    }
}
