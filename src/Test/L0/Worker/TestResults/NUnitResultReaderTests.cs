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
            "              <test-case name=\"TBScoutTest1.ProgramTest1.MultiplyTest\" executed=\"True\" result=\"Success\" success=\"True\" time=\"-0.027\" asserts=\"1\" />" +
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

        private const string _nUnit3ResultsXml = "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?><test-run id=\"2\" testcasecount=\"5\" result=\"Failed\" total=\"5\" passed=\"2\" failed=\"1\" inconclusive=\"1\" skipped=\"1\" asserts=\"0\" engine-version=\"3.4.1.0\" clr-version=\"4.0.30319.42000\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.178828\"><command-line><![CDATA[\"C:\\Program Files (x86)\\NUnit.org\\nunit-console\\nunit3-console.exe\"  \"C:\\Users\\prmutn\\Downloads\\nunit-csharp-samples-master\\ExpectedExceptionExample\\bin\\Release\\ExpectedExceptionExample.dll\"]]></command-line><test-suite type=\"Assembly\" id=\"0-1006\" name=\"ExpectedExceptionExample.dll\" fullname=\"C:\\Users\\prmutn\\Downloads\\nunit-csharp-samples-master\\ExpectedExceptionExample\\bin\\Release\\ExpectedExceptionExample.dll\" runstate=\"Runnable\" testcasecount=\"5\" result=\"Failed\" site=\"Child\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.102433\" total=\"5\" passed=\"2\" failed=\"1\" inconclusive=\"1\" skipped=\"1\" asserts=\"0\"><environment framework-version=\"3.4.1.0\" clr-version=\"2.0.50727.8689\" os-version=\"Microsoft Windows NT 10.0.10586.0\" platform=\"Win32NT\" cwd=\"C:\\Users\\prmutn\\Downloads\\nunit-csharp-samples-master\" machine-name=\"PRMUTN-PC\" user=\"prmutn\" user-domain=\"FAREAST\" culture=\"en-IN\" uiculture=\"en-US\" os-architecture=\"x64\" /><settings><setting name=\"WorkDirectory\" value=\"C:\\Users\\prmutn\\Downloads\\nunit-csharp-samples-master\" /><setting name=\"ImageRuntimeVersion\" value=\"2.0.50727\" /><setting name=\"ImageRequiresX86\" value=\"False\" /><setting name=\"ImageRequiresDefaultAppDomainAssemblyResolver\" value=\"False\" /><setting name=\"NumberOfTestWorkers\" value=\"12\" /></settings><properties><property name=\"_PID\" value=\"64680\" /><property name=\"_APPDOMAIN\" value=\"test-domain-\" /></properties><failure><message><![CDATA[One or more child tests had errors]]></message></failure><test-suite type=\"TestSuite\" id=\"0-1007\" name=\"ExpectedExceptionExample\" fullname=\"ExpectedExceptionExample\" runstate=\"Runnable\" testcasecount=\"5\" result=\"Failed\" site=\"Child\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.083422\" total=\"5\" passed=\"2\" failed=\"1\" inconclusive=\"1\" skipped=\"1\" asserts=\"0\"><failure><message><![CDATA[One or more child tests had errors]]></message></failure><test-suite type=\"TestFixture\" id=\"0-1000\" name=\"ExpectedExceptionTests\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Runnable\" testcasecount=\"5\" result=\"Failed\" site=\"Child\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.072832\" total=\"5\" passed=\"2\" failed=\"1\" inconclusive=\"1\" skipped=\"1\" asserts=\"0\"><failure><message><![CDATA[One or more child tests had errors]]></message></failure><test-case id=\"0-1003\" name=\"AnotherTest\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests.AnotherTest\" methodname=\"AnotherTest\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Runnable\" seed=\"1902677290\" result=\"Inconclusive\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"-0.026448\" asserts=\"0\"><reason><message><![CDATA[]]></message></reason></test-case><test-case id=\"0-1005\" name=\"HandlesArgumentExceptionAsType\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests.HandlesArgumentExceptionAsType\" methodname=\"HandlesArgumentExceptionAsType\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Runnable\" seed=\"1751424779\" result=\"Passed\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.001804\" asserts=\"0\" /><test-case id=\"0-1002\" name=\"SomeFailingTest\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests.SomeFailingTest\" methodname=\"SomeFailingTest\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Runnable\" seed=\"1236998109\" result=\"Failed\" label=\"Error\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.004298\" asserts=\"0\"><failure><message><![CDATA[System.ArgumentException : Value does not fall within the expected range.]]></message><stack-trace><![CDATA[   at ExpectedExceptionExample.ExpectedExceptionTests.SomeFailingTest()]]></stack-trace></failure><output><![CDATA[This is standard console output.]]></output><attachments><attachment><filePath>C:\\Users\\navb\\Pictures\\dummy1.png</filePath></attachment></attachments></test-case><test-case id=\"0-1004\" name=\"SomeIgnoredTest\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests.SomeIgnoredTest\" methodname=\"SomeIgnoredTest\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Ignored\" seed=\"584340542\" result=\"Skipped\" label=\"Ignored\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.001308\" asserts=\"0\"><properties><property name=\"_SKIPREASON\" value=\"Some reason\" /></properties><reason><message><![CDATA[Some reason]]></message></reason></test-case><test-case id=\"0-1001\" name=\"SomeRandomPassingTest\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests.SomeRandomPassingTest\" methodname=\"SomeRandomPassingTest\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Runnable\" seed=\"987550463\" result=\"Passed\" start-time=\"0001-01-01 00:00:00Z\" end-time=\"0001-01-01 00:00:00Z\" duration=\"0.000000\" asserts=\"0\" /></test-suite></test-suite></test-suite></test-run>";

        private const string _nUnit3ResultsXmlWithAttachments = "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?><test-run id=\"2\" testcasecount=\"5\" result=\"Failed\" total=\"5\" passed=\"2\" failed=\"1\" inconclusive=\"1\" skipped=\"1\" asserts=\"0\" engine-version=\"3.4.1.0\" clr-version=\"4.0.30319.42000\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.178828\"><command-line><![CDATA[\"C:\\Program Files (x86)\\NUnit.org\\nunit-console\\nunit3-console.exe\"  \"C:\\Users\\prmutn\\Downloads\\nunit-csharp-samples-master\\ExpectedExceptionExample\\bin\\Release\\ExpectedExceptionExample.dll\"]]></command-line><test-suite type=\"Assembly\" id=\"0-1006\" name=\"ExpectedExceptionExample.dll\" fullname=\"C:\\Users\\prmutn\\Downloads\\nunit-csharp-samples-master\\ExpectedExceptionExample\\bin\\Release\\ExpectedExceptionExample.dll\" runstate=\"Runnable\" testcasecount=\"5\" result=\"Failed\" site=\"Child\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.102433\" total=\"5\" passed=\"2\" failed=\"1\" inconclusive=\"1\" skipped=\"1\" asserts=\"0\"><environment framework-version=\"3.4.1.0\" clr-version=\"2.0.50727.8689\" os-version=\"Microsoft Windows NT 10.0.10586.0\" platform=\"Win32NT\" cwd=\"C:\\Users\\prmutn\\Downloads\\nunit-csharp-samples-master\" machine-name=\"PRMUTN-PC\" user=\"prmutn\" user-domain=\"FAREAST\" culture=\"en-IN\" uiculture=\"en-US\" os-architecture=\"x64\" /><settings><setting name=\"WorkDirectory\" value=\"C:\\Users\\prmutn\\Downloads\\nunit-csharp-samples-master\" /><setting name=\"ImageRuntimeVersion\" value=\"2.0.50727\" /><setting name=\"ImageRequiresX86\" value=\"False\" /><setting name=\"ImageRequiresDefaultAppDomainAssemblyResolver\" value=\"False\" /><setting name=\"NumberOfTestWorkers\" value=\"12\" /></settings><properties><property name=\"_PID\" value=\"64680\" /><property name=\"_APPDOMAIN\" value=\"test-domain-\" /></properties><failure><message><![CDATA[One or more child tests had errors]]></message></failure><test-suite type=\"TestSuite\" id=\"0-1007\" name=\"ExpectedExceptionExample\" fullname=\"ExpectedExceptionExample\" runstate=\"Runnable\" testcasecount=\"5\" result=\"Failed\" site=\"Child\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.083422\" total=\"5\" passed=\"2\" failed=\"1\" inconclusive=\"1\" skipped=\"1\" asserts=\"0\"><failure><message><![CDATA[One or more child tests had errors]]></message></failure><test-suite type=\"TestFixture\" id=\"0-1000\" name=\"ExpectedExceptionTests\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Runnable\" testcasecount=\"5\" result=\"Failed\" site=\"Child\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.072832\" total=\"5\" passed=\"2\" failed=\"1\" inconclusive=\"1\" skipped=\"1\" asserts=\"0\"><failure><message><![CDATA[One or more child tests had errors]]></message></failure><test-case id=\"0-1003\" name=\"AnotherTest\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests.AnotherTest\" methodname=\"AnotherTest\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Runnable\" seed=\"1902677290\" result=\"Inconclusive\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"-0.026448\" asserts=\"0\"><reason><message><![CDATA[]]></message></reason></test-case><test-case id=\"0-1005\" name=\"HandlesArgumentExceptionAsType\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests.HandlesArgumentExceptionAsType\" methodname=\"HandlesArgumentExceptionAsType\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Runnable\" seed=\"1751424779\" result=\"Passed\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.001804\" asserts=\"0\" /><test-case id=\"0-1002\" name=\"SomeFailingTest\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests.SomeFailingTest\" methodname=\"SomeFailingTest\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Runnable\" seed=\"1236998109\" result=\"Failed\" label=\"Error\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.004298\" asserts=\"0\"><failure><message><![CDATA[System.ArgumentException : Value does not fall within the expected range.]]></message><stack-trace><![CDATA[   at ExpectedExceptionExample.ExpectedExceptionTests.SomeFailingTest()]]></stack-trace></failure><output><![CDATA[This is standard console output.]]></output><attachments><attachment><filePath>C:\\Users\\navb\\Pictures\\dummy1.png</filePath></attachment><attachment><filePath>C:\\Users\\navb\\Pictures\\dummy4.png</filePath></attachment></attachments></test-case><test-case id=\"0-1004\" name=\"SomeIgnoredTest\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests.SomeIgnoredTest\" methodname=\"SomeIgnoredTest\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Ignored\" seed=\"584340542\" result=\"Skipped\" label=\"Ignored\" start-time=\"2016-10-26 05:40:02Z\" end-time=\"2016-10-26 05:40:02Z\" duration=\"0.001308\" asserts=\"0\"><properties><property name=\"_SKIPREASON\" value=\"Some reason\" /></properties><reason><message><![CDATA[Some reason]]></message></reason></test-case><test-case id=\"0-1001\" name=\"SomeRandomPassingTest\" fullname=\"ExpectedExceptionExample.ExpectedExceptionTests.SomeRandomPassingTest\" methodname=\"SomeRandomPassingTest\" classname=\"ExpectedExceptionExample.ExpectedExceptionTests\" runstate=\"Runnable\" seed=\"987550463\" result=\"Passed\" start-time=\"0001-01-01 00:00:00Z\" end-time=\"0001-01-01 00:00:00Z\" duration=\"0.000000\" asserts=\"0\" /></test-suite><attachments><attachment><filePath>C:\\Users\\navb\\Pictures\\dummy2.png</filePath></attachment></attachments></test-suite><attachments><attachment><filePath>C:\\Users\\navb\\Pictures\\dummy3.png</filePath></attachment></attachments></test-suite></test-run>";

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
        public void PublishNUnit3Results()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnit3ResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(5, _testRunData.Results.Length);
            Assert.Equal(2, _testRunData.Results.Count(r => r.Outcome.Equals("Passed")));
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("Failed")));
            var failedTestResult = _testRunData.Results.Where(r => r.Outcome.Equals("Failed")).First();
            Assert.Equal("System.ArgumentException : Value does not fall within the expected range.", failedTestResult.ErrorMessage);
            Assert.Equal("   at ExpectedExceptionExample.ExpectedExceptionTests.SomeFailingTest()", failedTestResult.StackTrace);
            Assert.Equal("This is standard console output.", failedTestResult.AttachmentData.ConsoleLog);
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("NotExecuted")));
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("Inconclusive")));
            Assert.Equal(1, _testRunData.Attachments.Length);
            Assert.Equal("NUnit Test Run", _testRunData.Name);
            Assert.Equal(default(DateTime), _testRunData.Results[4].StartedDate);
            Assert.Equal(default(DateTime), _testRunData.Results[4].CompletedDate);
            Assert.Equal(0f, _testRunData.Results[4].DurationInMs);
            // Platform is honored when BuildId is present.
            Assert.True(String.Equals(_testRunData.BuildPlatform, "Win32NT", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishNUnit3ResultsWhenThereIsNoBuild()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnit3ResultsXml;
            ReadResults(new TestRunContext("owner", String.Empty, string.Empty, 0, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(5, _testRunData.Results.Length);
            Assert.Equal(2, _testRunData.Results.Count(r => r.Outcome.Equals("Passed")));
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("Failed")));
            var failedTestResult = _testRunData.Results.Where(r => r.Outcome.Equals("Failed")).First();
            Assert.Equal("System.ArgumentException : Value does not fall within the expected range.", failedTestResult.ErrorMessage);
            Assert.Equal("   at ExpectedExceptionExample.ExpectedExceptionTests.SomeFailingTest()", failedTestResult.StackTrace);
            Assert.Equal("This is standard console output.", failedTestResult.AttachmentData.ConsoleLog);
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("NotExecuted")));
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("Inconclusive")));
            Assert.Equal(1, _testRunData.Attachments.Length);
            Assert.Equal("NUnit Test Run", _testRunData.Name);
            Assert.Equal(default(DateTime), _testRunData.Results[4].StartedDate);
            Assert.Equal(default(DateTime), _testRunData.Results[4].CompletedDate);
            Assert.Equal(0f, _testRunData.Results[4].DurationInMs);

            // When Build Id is 0, BuildPlatform and BuildFlavour shouldn't be set as the server makes validation and throws exception if these values are present without the build id specified.
            Assert.True(String.IsNullOrEmpty(_testRunData.BuildPlatform));
            Assert.True(String.IsNullOrEmpty(_testRunData.BuildFlavor));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void CustomRunTitleIsHonourednunit3()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnit3ResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri", "MyRunTitle"));

            Assert.NotNull(_testRunData);
            Assert.Equal("MyRunTitle", _testRunData.Name);
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
        public void CheckDurationIsSetToZeroIfNegative()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnitBasicResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
            // Verify that the negative value is appropriately converted to zero
            Assert.Equal(0, _testRunData.Results[0].DurationInMs);
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
        public void PublishBasicNUnitResultsSkipsAddingResultsFileWhenFlaggednunit3()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnit3ResultsXml;

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
        public void VerifyStartDateIsEmptyWhenStartTimeisUnavailablenunit3()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnit3ResultsXml.Replace("start-time", "start-timexyz");
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
        public void VerifyEndDateIsEmptyWhenEndTimeisUnavailablenunit3()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnit3ResultsXml.Replace("end-time", "end-timexyz");
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
            Assert.Equal(string.Empty, _testRunData.CompleteDate);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void CheckDurationIsZeroInNUnit3()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnit3ResultsXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));
            Assert.Equal(0, _testRunData.Results[0].DurationInMs);
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
            DateTime.TryParse(_testRunData.StartDate, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out testStartDate);
            DateTime.TryParse(_testRunData.CompleteDate, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out testCompleteDate);
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishNUnit3ResultsShouldConsiderTestCaseAttachments()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnit3ResultsXml;

            ReadResults(new TestRunContext("owner", String.Empty, string.Empty, 0, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            var failedTestWithAttachment = _testRunData.Results.Where(x => x.Outcome.Equals("Failed")).FirstOrDefault();
            Assert.Equal(failedTestWithAttachment.AttachmentData.AttachmentsFilePathList.Count(), 1);
            Assert.Collection(failedTestWithAttachment.AttachmentData.AttachmentsFilePathList.ToList(), x=>{x.Equals(@"C:\Users\navb\Pictures\dummy1.png");});
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishNUnit3ResultsShouldConsiderTestCaseAttachmentsMultiple()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnit3ResultsXmlWithAttachments;

            ReadResults(new TestRunContext("owner", String.Empty, string.Empty, 0, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            var failedTestWithAttachment = _testRunData.Results.Where(x => x.Outcome.Equals("Failed")).FirstOrDefault();
            Assert.Equal(failedTestWithAttachment.AttachmentData.AttachmentsFilePathList.Count(), 2);
            Assert.Collection(failedTestWithAttachment.AttachmentData.AttachmentsFilePathList.ToList(), x=>{x.Equals(@"C:\Users\navb\Pictures\dummy1.png");}, y=>{y.Equals( @"C:\Users\navb\Pictures\dummy4.png");});

            // checking other test cases should not have attachments.
            var passedTest = _testRunData.Results.Where(x => x.Outcome.Equals("Passed")).FirstOrDefault();
            Assert.Equal(passedTest.AttachmentData.AttachmentsFilePathList.Count(), 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishNUnit3ResultsShouldConsiderTestSuiteLevelAttachments()
        {
            SetupMocks();
            _nunitResultsToBeRead = _nUnit3ResultsXmlWithAttachments;

            ReadResults(new TestRunContext("owner", String.Empty, string.Empty, 0, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            // 2 files are test suites level attachments and one is the result file itself.
            Assert.Equal(_testRunData.Attachments.Count(), 3);
            Assert.Collection(_testRunData.Attachments.ToList(), x=>{x.Equals(@"C:\Users\navb\Pictures\dummy2.png");}, y=>{y.Equals( @"C:\Users\navb\Pictures\dummy3.png");}, z=>{z.Equals(_fileName);});
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
            var variables = new Variables(hc, new Dictionary<string, VariableValue>(), out warnings);
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