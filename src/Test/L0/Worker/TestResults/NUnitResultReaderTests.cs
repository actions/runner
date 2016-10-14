using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.TestResults;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Globalization;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.TestResults
{
    public class NunitResultReaderTests : IDisposable
    {
        private Mock<IExecutionContext> _ec;
        private static NUnitResultReader _nUnitReader;
        private static TestRunData _testRunData;
        private static string _fileName;
        private static string _nunitResultsToBeRead;

        private const string _nUnitBasicResultsXml =
            "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>" +
            "<!--This file represents the results of running a test suite-->" +
            "<test-results name=\"C:\\testws\\mghost\\projvc\\TBScoutTest1\\bin\\Debug\\TBScoutTest1.dll\" total=\"3\" errors=\"0\" failures=\"1\" not-run=\"0\" inconclusive=\"0\" ignored=\"0\" skipped=\"0\" invalid=\"0\" date=\"2015-04-15\" time=\"12:25:14\">" +
            "  <environment nunit-version=\"2.6.4.14350\" clr-version=\"2.0.50727.8009\" os-version=\"Microsoft Windows NT 6.2.9200.0\" platform=\"Win32NT\" cwd=\"D:\\Software\\NUnit-2.6.4\\bin\" machine-name=\"MGHOST\" user=\"madhurig\" user-domain=\"REDMOND\" />" +
            "  <culture-info current-culture=\"en-US\" current-uiculture=\"en-US\" />" +
            "  <test-suite type=\"Assembly\" name=\"C:\\testws\\mghost\\projvc\\TBScoutTest1\\bin\\Debug\\TBScoutTest1.dll\" executed=\"True\" result=\"Failure\" success=\"False\" time=\"0.059\" asserts=\"0\">" +
            "    <results>" +
            "      <test-suite type=\"Namespace\" name=\"TBScoutTest1\" executed=\"True\" result=\"Failure\" success=\"False\" time=\"0.051\" asserts=\"0\">" +
            "        <results>" +
            "          <test-suite type=\"TestFixture\" name=\"ProgramTest1\" executed=\"True\" result=\"Failure\" success=\"False\" time=\"0.050\" asserts=\"0\">" +
            "            <results>" +
            "              <test-case name=\"TBScoutTest1.ProgramTest1.MultiplyTest\" executed=\"True\" result=\"Success\" success=\"True\" time=\"0.027\" asserts=\"1\" />" +
            "              <test-case name=\"TBScoutTest1.ProgramTest1.SumTest\" executed=\"True\" result=\"Success\" success=\"True\" time=\"0.000\" asserts=\"1\" />" +
            "              <test-case name=\"TBScoutTest1.ProgramTest1.TestSumWithZeros\" executed=\"True\" result=\"Failure\" success=\"False\" time=\"0.009\" asserts=\"1\">" +
            "                <failure>" +
            "                  <message><![CDATA[  TBScout.Program.Sum did not return the expected value." +
            "  Expected: 0" +
            "  But was:  25" +
            "]]></message>" +
            "                  <stack-trace><![CDATA[at TBScoutTest1.ProgramTest1.TestSumWithZeros() in C:\\testws\\mghost\\projvc\\TBScoutTest1\\ProgramTest1.cs:line 63" +
            "]]></stack-trace>" +
            "                </failure>" +
            "              </test-case>" +
            "            </results>" +
            "          </test-suite>" +
            "        </results>" +
            "      </test-suite>" +
            "    </results>" +
            "  </test-suite>" +
            "</test-results>";

        private const string _nUnitSimpleResultsXml =
            "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" +
            "<test-results not-run=\"0\" failures=\"0\" total=\"17\" time=\"10:34:42\" date=\"2015-06-22\">" +
            "<test-suite time =\"1.943\" name=\"Tests.dll\" success=\"True\">" +
            "<results>" +
            "<test-case time=\"0.0003414\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailRESTTests.PaytrailCreatePaymentResponse_Deserializes\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0002827\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailRESTTests.PaytrailErrorMessage_Deserializes\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.022023\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailRESTTests.PaytrailCreatePaymentMessage_Serializes\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0081796\" name=\"VVO.RentalApartmentStore.Tests.Core.Database.RasContextTests.RasContext_EmptyByDefault\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0011515\" name=\"VVO.RentalApartmentStore.Tests.Core.Database.RasContextTests.RasContext_Insert_Ok\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0001928\" name=\"VVO.RentalApartmentStore.Tests.DummyTest.EnsureTestFrameworkWorksInBuildEnvironment\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.7327256\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateSuccessResponse_AuthCodeError_Fails\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.3111823\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.CreatePayment_Success\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.5112726\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateFailureResponse_NoExistingLogEntry_Throws\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0071163\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.CreatePayment_ErrorReturned_ThrowsPaytrailException\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0349477\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateFailureResponse_UpdatesLogEntry\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0017853\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateFailureResponse_AuthCodeError_Fails\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0013893\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.CreatePayment_HttpErrorReturned_ThrowsException\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0117449\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateSuccessResponse_NoExistingLogEntry_Throws\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.002887\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.CreatePayment_Success_CreatesLogEntry\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0026852\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateSuccessResponse_UpdatedLogEntry\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0020365\" name=\"VVO.RentalApartmentStore.Tests.Core.Entities.ISupportChangeTrackingTests.Insert_UpdatedCreatedByAndModifiedBy\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0020365\" name=\"VVO.RentalApartmentStore.Tests.Core.Entities.ISupportChangeTrackingTests.Insert_UpdatedCreatedByAndDeletedBy\" success=\"True\" result=\"Ignored\" executed=\"True\"> </test-case>" +
            "</results>" +
            "</test-suite>" +
            "</test-results>";

        private const string _nUnitSimpleResultsXmlWithoutMandatoryFields =
            "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" +
            "<test-results not-run=\"0\" failures=\"0\" total=\"17\" time=\"10:34:42\" date=\"2015-06-22\">" +
            "<test-suite time =\"1.943\" name=\"Tests.dll\" success=\"True\">" +
            "<results>" +
            "<test-case time=\"0.0003414\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0002827\" name=\"\" success=\"True\" executed=\"True\"> </test-case>" +
            "</results>" +
            "</test-suite>" +
            "</test-results>";

        private const string _nUnitSimpleResultsXmlWithDtd =
            "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" +
            "<!DOCTYPE report PUBLIC '-//JACOCO//DTD Report 1.0//EN' 'report.dtd'>" +
            "<test-results not-run=\"0\" failures=\"0\" total=\"17\" time=\"10:34:42\" date=\"2015-06-22\">" +
            "<test-suite time =\"1.943\" name=\"Tests.dll\" success=\"True\">" +
            "<results>" +
            "<test-case time=\"0.0003414\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailRESTTests.PaytrailCreatePaymentResponse_Deserializes\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0002827\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailRESTTests.PaytrailErrorMessage_Deserializes\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.022023\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailRESTTests.PaytrailCreatePaymentMessage_Serializes\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0081796\" name=\"VVO.RentalApartmentStore.Tests.Core.Database.RasContextTests.RasContext_EmptyByDefault\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0011515\" name=\"VVO.RentalApartmentStore.Tests.Core.Database.RasContextTests.RasContext_Insert_Ok\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0001928\" name=\"VVO.RentalApartmentStore.Tests.DummyTest.EnsureTestFrameworkWorksInBuildEnvironment\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.7327256\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateSuccessResponse_AuthCodeError_Fails\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.3111823\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.CreatePayment_Success\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.5112726\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateFailureResponse_NoExistingLogEntry_Throws\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0071163\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.CreatePayment_ErrorReturned_ThrowsPaytrailException\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0349477\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateFailureResponse_UpdatesLogEntry\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0017853\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateFailureResponse_AuthCodeError_Fails\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0013893\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.CreatePayment_HttpErrorReturned_ThrowsException\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0117449\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateSuccessResponse_NoExistingLogEntry_Throws\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.002887\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.CreatePayment_Success_CreatesLogEntry\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0026852\" name=\"VVO.RentalApartmentStore.Tests.Core.Integrations.Paytrail.PaytrailIntegrationTests.ValidateSuccessResponse_UpdatedLogEntry\" success=\"True\" executed=\"True\"> </test-case>" +
            "<test-case time=\"0.0020365\" name=\"VVO.RentalApartmentStore.Tests.Core.Entities.ISupportChangeTrackingTests.Insert_UpdatedCreatedByAndModifiedBy\" success=\"True\" executed=\"True\"> </test-case>" +
            "</results>" +
            "</test-suite>" +
            "</test-results>";

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ResultsWithoutMandatoryFieldsAreSkipped()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXmlWithoutMandatoryFields;

            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(0, _testRunData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishNUnitSimpleTransformResults()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(18, _testRunData.Results.Length);
            Assert.Equal(17, _testRunData.Results.Count(r => r.Outcome.Equals("Passed")));
            Assert.Equal(0, _testRunData.Results.Count(r => r.Outcome.Equals("Failed")));
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("NotExecuted")));
            Assert.Equal(1, _testRunData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishNUnitResultFile()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitBasicResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal("C:\\testws\\mghost\\projvc\\TBScoutTest1\\bin\\Debug\\TBScoutTest1.dll", _testRunData.Name);
            Assert.Equal(3, _testRunData.Results.Length);
            Assert.Equal(2, _testRunData.Results.Count(r => r.Outcome.Equals("Passed")));
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("Failed")));
            Assert.Equal(1, _testRunData.Attachments.Length);

            Assert.Equal("releaseUri", _testRunData.ReleaseUri);
            Assert.Equal("releaseEnvironmentUri", _testRunData.ReleaseEnvironmentUri);

            Assert.Equal(null, _testRunData.Results[0].AutomatedTestId);
            Assert.Equal(null, _testRunData.Results[0].AutomatedTestTypeId);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void CustomRunTitleIsHonoured()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri", "MyRunTitle"));

            Assert.NotNull(_testRunData);
            Assert.Equal("MyRunTitle", _testRunData.Name);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void CustomRunTitleIsHonouredEvenIfXmlHasTitle()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitBasicResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri", "MyRunTitle"));

            Assert.NotNull(_testRunData);
            Assert.Equal("MyRunTitle", _testRunData.Name);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void DefaultRunTitleIsHonoured()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXml;
            ReadResults();

            Assert.NotNull(_testRunData);
            Assert.Equal("NUnit Test Run", _testRunData.Name);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void RunTitleFromXmlIsHonoured()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitBasicResultsXml;
            ReadResults();

            Assert.NotNull(_testRunData);
            Assert.Equal("C:\\testws\\mghost\\projvc\\TBScoutTest1\\bin\\Debug\\TBScoutTest1.dll", _testRunData.Name);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyRunTypeIsSet()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXml;
            ReadResults();
            Assert.Equal(_nUnitReader.Name, _testRunData.Results[0].AutomatedTestType);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishBasicNUnitResultsAddsResultsFileByDefault()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.True(_testRunData.Attachments.Length == 1, "the run level attachment is not present");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishBasicNUnitResultsSkipsAddingResultsFileWhenFlagged()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXml;

            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"), false);
            Assert.True(_testRunData.Attachments.Length == 0, "the run level attachment is present even though the flag was set to false");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestCaseStartDate()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
            Assert.Equal(_testRunData.StartDate, _testRunData.Results[0].StartedDate.ToString("o"));

        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestCaseCompletedDate()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
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
            _nunitResultsToBeRead = _nUnitSimpleResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
            var testCaseCompletedDate = _testRunData.Results[0].CompletedDate;
            var testRunCompletedDate = _testRunData.Results[0].StartedDate.AddTicks(DateTime.Parse(_testRunData.CompleteDate).Ticks);
            Assert.True(testCaseCompletedDate <= testRunCompletedDate, "first test case end should be within test run completed time");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyStartDateIsEmptyWhenNoRunTimeIsAvailable()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitBasicResultsXml.Replace("time", "timer").Replace("date", "dater");
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
            Assert.Equal(string.Empty, _testRunData.StartDate);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyEndDateIsEmptyWhenNoRunTimeIsAvailable()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitBasicResultsXml.Replace("time", "timer").Replace("date", "dater");
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
            Assert.Equal(string.Empty, _testRunData.CompleteDate);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestRunDuration()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironment"));
            DateTime testStartDate, testCompleteDate;
            DateTime.TryParse(_testRunData.StartDate, out testStartDate);
            DateTime.TryParse(_testRunData.CompleteDate, out testCompleteDate);
            TimeSpan duration = testCompleteDate - testStartDate;
            Assert.Equal(1.653, duration.TotalSeconds);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyDateTimeAndDurationCalculationsInDifferentCulture()
        {
            SetupMocks();
            CultureInfo current = CultureInfo.CurrentCulture;
            try
            {
                //German is used, as in this culture decimal seperator is comma & thousand seperator is dot
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                //Verify test case start date
                _nunitResultsToBeRead = _nUnitSimpleResultsXml;
                ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
                Assert.Equal(_testRunData.StartDate, _testRunData.Results[0].StartedDate.ToString("o"));

                //Verify test case completed date
                _nunitResultsToBeRead = _nUnitSimpleResultsXml;
                ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
                var testCase1CompletedDate = _testRunData.Results[0].CompletedDate;
                var testCase2StartDate = _testRunData.Results[1].StartedDate;
                Assert.True(testCase1CompletedDate <= testCase2StartDate, "first test case end should be before second test case start time");

                //Verify test case run duration 
                _nunitResultsToBeRead = _nUnitSimpleResultsXml;
                ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironment"));
                DateTime testStartDate, testCompleteDate;
                DateTime.TryParse(_testRunData.StartDate, out testStartDate);
                DateTime.TryParse(_testRunData.CompleteDate, out testCompleteDate);
                TimeSpan duration = testCompleteDate - testStartDate;
                Assert.Equal(1.653, duration.TotalSeconds);

            }
            finally
            {
                CultureInfo.CurrentCulture = current;
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Nunit_DtdProhibitedXmlShouldReturnNull()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitSimpleResultsXmlWithDtd;
            ReadResults();
            Assert.NotNull(_testRunData);
        }

        public void Dispose()
        {
            _nUnitReader.AddResultsFileToRunLevelAttachments = true;
            try
            {
                File.Delete(_fileName);
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

        private void ReadResults(TestRunContext runContext = null, bool attachRunLevelAttachments = true)
        {
            _fileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            File.WriteAllText(_fileName, _nunitResultsToBeRead);

            _nUnitReader = new NUnitResultReader();
            _nUnitReader.AddResultsFileToRunLevelAttachments = attachRunLevelAttachments;
            _testRunData = _nUnitReader.ReadResults(_ec.Object, _fileName, runContext);
        }
    }
}