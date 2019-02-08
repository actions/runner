using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.TestResults;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Results
{
    public class CTestResultReaderTests : IDisposable
    {
        private Mock<IExecutionContext> _ec;
        private static CTestResultReader _cTestReader;
        private static TestRunData _testRunData;
        private static string _fileName;
        private static string _cResultsToBeRead;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void ResultsWithoutMandatoryFieldsAreSkippedCTest()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultsWithoutMandatoryFields;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(0, _testRunData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void PublishBasicCResultsXml()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(6, _testRunData.Results.Length);
            Assert.Equal(3, _testRunData.Results.Count(r => r.Outcome.Equals("Passed")));
            Assert.Equal(2, _testRunData.Results.Count(r => r.Outcome.Equals("Failed")));
            Assert.Equal(1, _testRunData.Results.Count(r => r.Outcome.Equals("NotExecuted")));
            Assert.Equal("CTest Test Run configuration platform", _testRunData.Name);
            Assert.Equal("releaseUri", _testRunData.ReleaseUri);
            Assert.Equal("releaseEnvironmentUri", _testRunData.ReleaseEnvironmentUri);
            Assert.Equal("owner", _testRunData.Results[0].RunBy.DisplayName);
            Assert.Equal("owner", _testRunData.Results[0].Owner.DisplayName);
            Assert.Equal("Completed", _testRunData.Results[0].State);
            Assert.Equal("./libs/MgmtVisualization/tests/LoggingSinkRandomTests.loggingSinkRandomTest_CallLoggingManagerCallback", _testRunData.Results[0].AutomatedTestName);
            Assert.Equal("./libs/MgmtVisualization/tests", _testRunData.Results[0].AutomatedTestStorage);
            Assert.Equal("LoggingSinkRandomTests.loggingSinkRandomTest_CallLoggingManagerCallback", _testRunData.Results[0].TestCaseTitle);
            Assert.Equal(null, _testRunData.Results[0].AutomatedTestId);
            Assert.Equal(null, _testRunData.Results[0].AutomatedTestTypeId);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyTestRunDurationCTest()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            var timeSpan = DateTime.Parse(_testRunData.CompleteDate) - DateTime.Parse(_testRunData.StartDate);

            Assert.Equal(382, timeSpan.TotalSeconds);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyTestCaseDurationWhenTestCaseTimeIsInSeconds()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            var time = TimeSpan.FromMilliseconds(_testRunData.Results[0].DurationInMs);

            Assert.Equal(0.074, time.TotalSeconds);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyRunTypeIsSet()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXml;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.Equal(CTestResultReader.ParserName, _testRunData.Results[0].AutomatedTestType);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyPublishingStandardLogs()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXmlWithOneTest;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.NotNull(_testRunData);
            Assert.Equal(1, _testRunData.Results.Length);
            Assert.Equal("output : [----------] Global test environment set-up.", _testRunData.Results[0].AttachmentData.ConsoleLog);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyTestCaseStartDate()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXmlWithOneTest;
            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.Equal(_testRunData.StartDate, _testRunData.Results[0].StartedDate.ToString("o"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyLastTestCaseEndDateNotGreaterThanTestRunTotalTime()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXmlWithOneTest;
            ReadResults();
            var testCaseCompletedDate = _testRunData.Results[0].CompletedDate;
            var testRunCompletedDate = _testRunData.Results[0].StartedDate
                .AddTicks(DateTime.Parse(_testRunData.CompleteDate).Ticks);

            Assert.True(testCaseCompletedDate <= testRunCompletedDate, "first test case end should be within test run completed time");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyCTestReadResultsDoesNotFailWithoutStartTime()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXmlWithoutStartTime;

            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.Equal(_testRunData.Results.Length, 1);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyCTestReadResultsDoesNotFailWithoutFinishTime()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXmlWithoutEndTime;

            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.Equal(_testRunData.Results.Length, 1);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyStackTraceForFailedTest()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXmlWithFailedTest;
            string expectedStackTrace = "-- Download of file://\\abc-mang.md5.txtfailed with message: [37]\"couldn't read a file:// file\"CMake Error at modules/Logging.cmake:121 (message):test BAR_wget_file succeed: result is \"OFF\" instead of \"ON\"Call Stack (most recent call first):modules/Test.cmake:74 (BAR_msg_fatal)modules/testU/WGET-testU-noMD5.cmake:14 (BAR_check_equal)";

            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.Equal(expectedStackTrace, _testRunData.Results[0].StackTrace);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyTestRunTimeIsEmptyIfTimestampIsNotParseable()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestResultXmlWithNegativeTimeStamp;

            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.Equal(string.Empty, _testRunData.StartDate);
            Assert.Equal(string.Empty, _testRunData.CompleteDate);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyTestRunTimeIsSetToZeroIfDurationIsNegative()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestResultXmlWithNegativeDuration;

            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.Equal(0, _testRunData.Results[0].DurationInMs);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishResults")]
        public void VerifyTestRunTimeIsSetToUTCTime()
        {
            SetupMocks();
            _cResultsToBeRead = _cTestSimpleResultXmlWithLocalTime;

            ReadResults(new TestRunContext("owner", "platform", "configuration", 1, "buildUri", "releaseUri", "releaseEnvironmentUri"));

            Assert.Equal("2019-01-03T12:21:19.0000000Z", _testRunData.StartDate);
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            TestHostContext hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();
            List<string> warnings;
            var variables = new Variables(hc, new Dictionary<string, VariableValue>(), out warnings);
            _ec.Setup(x => x.Variables).Returns(variables);
        }

        private void ReadResults(TestRunContext runContext = null)
        {
            _fileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            File.WriteAllText(_fileName, _cResultsToBeRead);

            _cTestReader = new CTestResultReader();
            _testRunData = _cTestReader.ReadResults(_ec.Object, _fileName, runContext);
        }

        public void Dispose()
        {
            try
            {
                File.Delete(_fileName);
            }
            catch
            {

            }
        }

        #region Constants

        private const string _cTestSimpleResultsWithoutMandatoryFields = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Site BuildName=\"(empty)\" BuildStamp=\"20180515-1731-Experimental\" Name=\"(empty)\" Generator=\"ctest-3.11.0\" "
            + "CompilerName=\"\" CompilerVersion=\"\" OSName=\"Linux\" Hostname=\"3tnavBuild\" OSRelease=\"4.4.0-116-generic\" "
            + "OSVersion=\"#140-Ubuntu SMP Mon Feb 12 21:23:04 UTC 2018\" OSPlatform=\"x86_64\" Is64Bits=\"1\">"
            + "<Testing>"
            + "<StartDateTime>May 15 10:31 PDT</StartDateTime>"
            + "<StartTestTime>1526405497</StartTestTime>"
            + "<TestList>"
            + "<Test>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Test>"
            + "<Test>./tools/simulator/test/simulator.SimulatorTest.readEventFile_mediaDetectedEvent_oneSignalEmitted</Test>"
            + "</TestList>"
            + "<Test Status =\"passed\">"
            + "<Path>./libs/MgmtVisualization/tests</Path>"
            + "<FullName>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</FullName>"
            + "<FullCommandLine>/home/ctc/jenkins/workspace/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</FullCommandLine>"
            + "</Test>"
            + "<Test Status =\"notrun\">"
            + "<Name>simulator.SimulatorTest.readEventFile_mediaDetectedEvent_oneSignalEmitted</Name>"
            + "<Path>./tools/simulator/test</Path>"
            + "<FullCommandLine></FullCommandLine>"
            + "</Test>"
            + "<EndDateTime>May 15 10:37 PDT</EndDateTime>"
            + "<EndTestTime>1526405879</EndTestTime>"
            + "<ElapsedMinutes>6</ElapsedMinutes>"
            + "</Testing>"
            + "</Site>";

        private const string _cTestSimpleResultXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Site BuildName=\"(empty)\" BuildStamp=\"20180515-1731-Experimental\" Name=\"(empty)\" Generator=\"ctest-3.11.0\" "
            + "CompilerName=\"\" CompilerVersion=\"\" OSName=\"Linux\" Hostname=\"3tnavBuild\" OSRelease=\"4.4.0-116-generic\" "
            + "OSVersion=\"#140-Ubuntu SMP Mon Feb 12 21:23:04 UTC 2018\" OSPlatform=\"x86_64\" Is64Bits=\"1\">"
            + "<Testing>"
            + "<StartDateTime>May 15 10:31 PDT</StartDateTime>"
            + "<StartTestTime>1526405497</StartTestTime>"
            + "<TestList>"
            + "<Test>./libs/MgmtVisualization/tests/LoggingSinkRandomTests.loggingSinkRandomTest_CallLoggingManagerCallback</Test>"
            + "<Test>./tools/simulator/test/simulator.SimulatorTest.readEventFile_mediaDetectedEvent_oneSignalEmitted</Test>"
            + "</TestList>"
            + "<Test Status =\"passed\">"
            + "<Name>LoggingSinkRandomTests.loggingSinkRandomTest_CallLoggingManagerCallback</Name>"
            + "<Path>./libs/MgmtVisualization/tests</Path>"
            + "<FullName>./libs/MgmtVisualization/tests/LoggingSinkRandomTests.loggingSinkRandomTest_CallLoggingManagerCallback</FullName>"
            + "<FullCommandLine>D:/a/r1/a/libs/MgmtVisualization/tests/MgmtVisualizationResultsAPI \"--gtest_filter=LoggingSinkRandomTests.loggingSinkRandomTest_CallLoggingManagerCallback\"</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Execution Time\">"
            + "<Value>0.074303</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Processors\">"
            + "<Value>1</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Completion Status\">"
            + "<Value>Completed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Command Line\">"
            + "<Value>/home/ctc/jenkins/workspace/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>output : [----------] Global test environment set-up.</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<Test Status =\"notrun\">"
            + "<Name>simulator.SimulatorTest.readEventFile_mediaDetectedEvent_oneSignalEmitted</Name>"
            + "<Path>./tools/simulator/test</Path>"
            + "<FullName>./tools/simulator/test/simulator.SimulatorTest.readEventFile_mediaDetectedEvent_oneSignalEmitted</FullName>"
            + "<FullCommandLine></FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Processors\">"
            + "<Value>1</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Completion Status\">"
            + "<Value>Disabled</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Command Line\">"
            + "<Value></Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>Disabled</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<Test Status=\"passed\">"
            + "<Name>test_cgreen_run_named_test</Name>"
            + "<Path>./tests</Path>"
            + "<FullName>./tests/test_cgreen_run_named_test</FullName>"
            + "<FullCommandLine>/var/lib/jenkins/workspace/Cgreen-thoni56/build/build-c/tests/test_cgreen_c &quot;integer_one_should_assert_true&quot;</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type=\"numeric/double\" name=\"Execution Time\"><Value>0.00615707</Value></NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Completion Status\"><Value>Completed</Value></NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Command Line\"><Value>/var/lib/jenkins/workspace/Cgreen-thoni56/build/build-c/tests/test_cgreen_c &quot;integer_one_should_assert_true&quot;</Value></NamedMeasurement>"
            + "<Measurement>"
            + "<Value>Running &quot;all_c_tests&quot; (136 tests)..."
            + "Completed &quot;assertion_tests&quot;: 1 pass, 0 failures, 0 exceptions in 0ms."
            + "Completed &quot;all_c_tests&quot;: 1 pass, 0 failures, 0 exceptions in 0ms."
            + "</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<Test Status=\"passed\">"
            + "<Name>runner_test_cgreen_c</Name>"
            + "<Path>./tests</Path>"
            + "<FullName>./tests/runner_test_cgreen_c</FullName>"
            + "<FullCommandLine>D:/a/r1/a/Cgreen-thoni56/build/build-c/tools/cgreen-runner &quot;-x&quot; &quot;TEST&quot; &quot;libcgreen_c_tests.so&quot;</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type=\"numeric/double\" name=\"Execution Time\"><Value>0.499399</Value></NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Completion Status\"><Value>Completed</Value></NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Command Line\"><Value>/var/lib/jenkins/workspace/Cgreen-thoni56/build/build-c/tools/cgreen-runner &quot;-x&quot; &quot;TEST&quot; &quot;libcgreen_c_tests.so&quot;</Value></NamedMeasurement>"
            + "<Measurement>"
            + "<Value>	CGREEN EXCEPTION: Too many assertions within a single test."
            + "</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<Test Status=\"failed\">"
            + "<Name>WGET-testU-MD5-fail</Name>"
            + "<Path>E_/foo/sources</Path>"
            + "<FullName>E_/foo/sources/WGET-testU-MD5-fail</FullName>"
            + "<FullCommandLine>E:\\Tools\\cmake\\cmake-2.8.11-rc4-win32-x86\\bin\\cmake.exe &quot;-DTEST_OUTPUT_DIR:PATH=E:/foo/build-vs2008-visual/_cmake/modules/testU_WGET&quot;"
            + "&quot;-P&quot; &quot;E:/foo/sources/modules/testU/WGET-testU-MD5-fail.cmake&quot;</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type=\"text/string\" name=\"Exit Code\">"
            + "<Value>Failed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Exit Value\">"
            + "<Value>0</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"numeric/double\" name=\"Execution Time\">"
            + "<Value>0.0760078</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Completion Status\">"
            + "<Value>Completed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Command Line\">"
            + "<Value>E:\\Tools\\cmake\\cmake-2.8.11-rc4-win32-x86\\bin\\cmake.exe &quot;-DTEST_OUTPUT_DIR:PATH=E:/foo/build-vs2008-visual/_cmake/modules/testU_WGET&quot;"
            + "&quot;-P&quot; &quot;E:/foo/sources/modules/testU/WGET-testU-MD5-fail.cmake&quot;</Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>-- Download of file://\\abc-mang.md5.txt"
            + "failed with message: [37]&quot;couldn&apos;t read a file:// file&quot;"
            + "</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<Test Status=\"failed\">"
            + "<Name>WGET-testU-noMD5</Name>"
            + "<Path>E_/foo/sources</Path>"
            + "<FullName>E_/foo/sources/WGET-testU-noMD5</FullName>"
            + "<FullCommandLine>E:\\Tools\\cmake\\cmake-2.8.11-rc4-win32-x86\\bin\\cmake.exe &quot;-DTEST_OUTPUT_DIR:PATH=E:/foo/build-vs2008-visual/_cmake/modules/testU_WGET&quot;"
            + "&quot;-P&quot; &quot;E:/foo/sources/modules/testU/WGET-testU-noMD5.cmake&quot;</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type=\"text/string\" name=\"Exit Code\">"
            + "<Value>Failed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Exit Value\">"
            + "<Value>1</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"numeric/double\" name=\"Execution Time\">"
            + "<Value>0.0820084</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Completion Status\">"
            + "<Value>Completed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Command Line\">"
            + "<Value>E:\\Tools\\cmake\\cmake-2.8.11-rc4-win32-x86\\bin\\cmake.exe &quot;-DTEST_OUTPUT_DIR:PATH=E:/foo/build-vs2008-visual/_cmake/modules/testU_WGET&quot;"
            + "&quot;-P&quot; &quot;E:/foo/sources/modules/testU/WGET-testU-noMD5.cmake&quot;</Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>-- Download of file://\\abc-mang.md5.txt"
            + "failed with message: [37]&quot;couldn&apos;t read a file:// file&quot;"
            + "CMake Error at modules/Logging.cmake:121 (message):"
            + ""
            + ""
            + "test BAR_wget_file succeed: result is &quot;OFF&quot; instead of &quot;ON&quot;"
            + ""
            + "Call Stack (most recent call first):"
            + "modules/Test.cmake:74 (BAR_msg_fatal)"
            + "modules/testU/WGET-testU-noMD5.cmake:14 (BAR_check_equal)"
            + ""
            + ""
            + "</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<EndDateTime>May 15 10:37 PDT</EndDateTime>"
            + "<EndTestTime>1526405879</EndTestTime>"
            + "<ElapsedMinutes>6</ElapsedMinutes>"
            + "</Testing>"
            + "</Site>";

        private const string _cTestSimpleResultXmlWithOneTest = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Site BuildName=\"(empty)\" BuildStamp=\"20180515-1731-Experimental\" Name=\"(empty)\" Generator=\"ctest-3.11.0\" "
            + "CompilerName=\"\" CompilerVersion=\"\" OSName=\"Linux\" Hostname=\"3tnavBuild\" OSRelease=\"4.4.0-116-generic\" "
            + "OSVersion=\"#140-Ubuntu SMP Mon Feb 12 21:23:04 UTC 2018\" OSPlatform=\"x86_64\" Is64Bits=\"1\">"
            + "<Testing>"
            + "<StartDateTime>May 15 10:31 PDT</StartDateTime>"
            + "<StartTestTime>1526405497</StartTestTime>"
            + "<TestList>"
            + "<Test>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Test>"
            + "</TestList>"
            + "<Test Status =\"passed\">"
            + "<Name>loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Name>"
            + "<Path>./libs/MgmtVisualization/tests</Path>"
            + "<FullName>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</FullName>"
            + "<FullCommandLine>D:/a/r1/a/libs/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Execution Time\">"
            + "<Value>0.0074303</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Processors\">"
            + "<Value>1</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Completion Status\">"
            + "<Value>Completed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Command Line\">"
            + "<Value>D:/a/r1/a/libs/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>output : [----------] Global test environment set-up.</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<EndDateTime>May 15 10:37 PDT</EndDateTime>"
            + "<EndTestTime>1526405879</EndTestTime>"
            + "<ElapsedMinutes>6</ElapsedMinutes>"
            + "</Testing>"
            + "</Site>";

        private const string _cTestSimpleResultXmlWithFailedTest = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Site BuildName=\"(empty)\" BuildStamp=\"20180515-1731-Experimental\" Name=\"(empty)\" Generator=\"ctest-3.11.0\" "
            + "CompilerName=\"\" CompilerVersion=\"\" OSName=\"Linux\" Hostname=\"3tnavBuild\" OSRelease=\"4.4.0-116-generic\" "
            + "OSVersion=\"#140-Ubuntu SMP Mon Feb 12 21:23:04 UTC 2018\" OSPlatform=\"x86_64\" Is64Bits=\"1\">"
            + "<Testing>"
            + "<StartDateTime>May 15 10:31 PDT</StartDateTime>"
            + "<StartTestTime>1526405497</StartTestTime>"
            + "<TestList>"
            + "<Test>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Test>"
            + "</TestList>"
            + "<Test Status=\"failed\">"
            + "<Name>WGET-testU-noMD5</Name>"
            + "<Path>E_/foo/sources</Path>"
            + "<FullName>E_/foo/sources/WGET-testU-noMD5</FullName>"
            + "<FullCommandLine>E:\\Tools\\cmake\\cmake-2.8.11-rc4-win32-x86\\bin\\cmake.exe &quot;-DTEST_OUTPUT_DIR:PATH=E:/foo/build-vs2008-visual/_cmake/modules/testU_WGET&quot;"
            + "&quot;-P&quot; &quot;E:/foo/sources/modules/testU/WGET-testU-noMD5.cmake&quot;</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type=\"text/string\" name=\"Exit Code\">"
            + "<Value>Failed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Exit Value\">"
            + "<Value>1</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"numeric/double\" name=\"Execution Time\">"
            + "<Value>0.0820084</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Completion Status\">"
            + "<Value>Completed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Command Line\">"
            + "<Value>E:\\Tools\\cmake\\cmake-2.8.11-rc4-win32-x86\\bin\\cmake.exe &quot;-DTEST_OUTPUT_DIR:PATH=E:/foo/build-vs2008-visual/_cmake/modules/testU_WGET&quot;"
            + "&quot;-P&quot; &quot;E:/foo/sources/modules/testU/WGET-testU-noMD5.cmake&quot;</Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>-- Download of file://\\abc-mang.md5.txt"
            + "failed with message: [37]&quot;couldn&apos;t read a file:// file&quot;"
            + "CMake Error at modules/Logging.cmake:121 (message):"
            + ""
            + ""
            + "test BAR_wget_file succeed: result is &quot;OFF&quot; instead of &quot;ON&quot;"
            + ""
            + "Call Stack (most recent call first):"
            + "modules/Test.cmake:74 (BAR_msg_fatal)"
            + "modules/testU/WGET-testU-noMD5.cmake:14 (BAR_check_equal)"
            + ""
            + ""
            + "</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<EndDateTime>May 15 10:37 PDT</EndDateTime>"
            + "<EndTestTime>1526405879</EndTestTime>"
            + "<ElapsedMinutes>6</ElapsedMinutes>"
            + "</Testing>"
            + "</Site>";

        private const string _cTestSimpleResultXmlWithoutEndTime = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Site BuildName=\"(empty)\" BuildStamp=\"20180515-1731-Experimental\" Name=\"(empty)\" Generator=\"ctest-3.11.0\" "
            + "CompilerName=\"\" CompilerVersion=\"\" OSName=\"Linux\" Hostname=\"3tnavBuild\" OSRelease=\"4.4.0-116-generic\" "
            + "OSVersion=\"#140-Ubuntu SMP Mon Feb 12 21:23:04 UTC 2018\" OSPlatform=\"x86_64\" Is64Bits=\"1\">"
            + "<Testing>"
            + "<StartDateTime>May 15 10:31 PDT</StartDateTime>"
            + "<StartTestTime>1526405497</StartTestTime>"
            + "<TestList>"
            + "<Test>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Test>"
            + "</TestList>"
            + "<Test Status =\"passed\">"
            + "<Name>loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Name>"
            + "<Path>./libs/MgmtVisualization/tests</Path>"
            + "<FullName>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</FullName>"
            + "<FullCommandLine>/home/ctc/jenkins/workspace/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Execution Time\">"
            + "<Value>0.0074303</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Processors\">"
            + "<Value>1</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Completion Status\">"
            + "<Value>Completed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Command Line\">"
            + "<Value>/home/ctc/jenkins/workspace/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>output : [----------] Global test environment set-up.</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "</Testing>"
            + "</Site>";

        private const string _cTestSimpleResultXmlWithoutStartTime = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Site BuildName=\"(empty)\" BuildStamp=\"20180515-1731-Experimental\" Name=\"(empty)\" Generator=\"ctest-3.11.0\" "
            + "CompilerName=\"\" CompilerVersion=\"\" OSName=\"Linux\" Hostname=\"3tnavBuild\" OSRelease=\"4.4.0-116-generic\" "
            + "OSVersion=\"#140-Ubuntu SMP Mon Feb 12 21:23:04 UTC 2018\" OSPlatform=\"x86_64\" Is64Bits=\"1\">"
            + "<Testing>"
            + "<TestList>"
            + "<Test>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Test>"
            + "</TestList>"
            + "<Test Status =\"passed\">"
            + "<Name>loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Name>"
            + "<Path>./libs/MgmtVisualization/tests</Path>"
            + "<FullName>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</FullName>"
            + "<FullCommandLine>/home/ctc/jenkins/workspace/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Execution Time\">"
            + "<Value>0.0074303</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Processors\">"
            + "<Value>1</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Completion Status\">"
            + "<Value>Completed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Command Line\">"
            + "<Value>/home/ctc/jenkins/workspace/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>output : [----------] Global test environment set-up.</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<EndDateTime>May 15 10:37 PDT</EndDateTime>"
            + "<EndTestTime>1526405879</EndTestTime>"
            + "<ElapsedMinutes>6</ElapsedMinutes>"
            + "</Testing>"
            + "</Site>";

        private const string _cTestResultXmlWithNegativeTimeStamp = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Site BuildName=\"(empty)\" BuildStamp=\"20180515-1731-Experimental\" Name=\"(empty)\" Generator=\"ctest-3.11.0\" "
            + "CompilerName=\"\" CompilerVersion=\"\" OSName=\"Linux\" Hostname=\"3tnavBuild\" OSRelease=\"4.4.0-116-generic\" "
            + "OSVersion=\"#140-Ubuntu SMP Mon Feb 12 21:23:04 UTC 2018\" OSPlatform=\"x86_64\" Is64Bits=\"1\">"
            + "<Testing>"
            + "<StartDateTime>May 15 10:31 PDT</StartDateTime>"
            + "<StartTestTime>-1526405497</StartTestTime>"
            + "<TestList>"
            + "<Test>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Test>"
            + "</TestList>"
            + "<Test Status =\"passed\">"
            + "<Name>loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Name>"
            + "<Path>./libs/MgmtVisualization/tests</Path>"
            + "<FullName>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</FullName>"
            + "<FullCommandLine>D:/a/r1/a/libs/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Execution Time\">"
            + "<Value>0.0074303</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Processors\">"
            + "<Value>1</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Completion Status\">"
            + "<Value>Completed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Command Line\">"
            + "<Value>D:/a/r1/a/libs/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>output : [----------] Global test environment set-up.</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<EndDateTime>May 15 10:37 PDT</EndDateTime>"
            + "<EndTestTime>1526405879</EndTestTime>"
            + "<ElapsedMinutes>6</ElapsedMinutes>"
            + "</Testing>"
            + "</Site>";

        private const string _cTestResultXmlWithNegativeDuration = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Site BuildName=\"(empty)\" BuildStamp=\"20180515-1731-Experimental\" Name=\"(empty)\" Generator=\"ctest-3.11.0\" "
            + "CompilerName=\"\" CompilerVersion=\"\" OSName=\"Linux\" Hostname=\"3tnavBuild\" OSRelease=\"4.4.0-116-generic\" "
            + "OSVersion=\"#140-Ubuntu SMP Mon Feb 12 21:23:04 UTC 2018\" OSPlatform=\"x86_64\" Is64Bits=\"1\">"
            + "<Testing>"
            + "<StartDateTime>May 15 10:31 PDT</StartDateTime>"
            + "<StartTestTime>1526405497</StartTestTime>"
            + "<TestList>"
            + "<Test>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Test>"
            + "</TestList>"
            + "<Test Status =\"passed\">"
            + "<Name>loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Name>"
            + "<Path>./libs/MgmtVisualization/tests</Path>"
            + "<FullName>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</FullName>"
            + "<FullCommandLine>D:/a/r1/a/libs/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Execution Time\">"
            + "<Value>-0.0074303</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"numeric/double\" name=\"Processors\">"
            + "<Value>1</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Completion Status\">"
            + "<Value>Completed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type =\"text/string\" name=\"Command Line\">"
            + "<Value>D:/a/r1/a/libs/build_TNAV-dev_Pull-Request/build/libs/MgmtVisualization/tests/MgmtVisualizationTestPublicAPI \"--gtest_filter=loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback\"</Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>output : [----------] Global test environment set-up.</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<EndDateTime>May 15 10:37 PDT</EndDateTime>"
            + "<EndTestTime>1526405879</EndTestTime>"
            + "<ElapsedMinutes>6</ElapsedMinutes>"
            + "</Testing>"
            + "</Site>";

        private const string _cTestSimpleResultXmlWithLocalTime = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Site BuildName=\"(empty)\" BuildStamp=\"20180515-1731-Experimental\" Name=\"(empty)\" Generator=\"ctest-3.11.0\" "
            + "CompilerName=\"\" CompilerVersion=\"\" OSName=\"Linux\" Hostname=\"3tnavBuild\" OSRelease=\"4.4.0-116-generic\" "
            + "OSVersion=\"#140-Ubuntu SMP Mon Feb 12 21:23:04 UTC 2018\" OSPlatform=\"x86_64\" Is64Bits=\"1\">"
            + "<Testing>"
            + "<StartDateTime>Jan 03 17:51 India Standard Time</StartDateTime>"
            + "<StartTestTime>1546518079</StartTestTime>"
            + "<TestList>"
            + "<Test>./libs/MgmtVisualization/tests/loggingSinkRandomTests.loggingSinkRandomTest_CallLoggingCallback</Test>"
            + "</TestList>"
            + "<Test Status=\"failed\">"
            + "<Name>WGET-testU-noMD5</Name>"
            + "<Path>E_/foo/sources</Path>"
            + "<FullName>E_/foo/sources/WGET-testU-noMD5</FullName>"
            + "<FullCommandLine>E:\\Tools\\cmake\\cmake-2.8.11-rc4-win32-x86\\bin\\cmake.exe &quot;-DTEST_OUTPUT_DIR:PATH=E:/foo/build-vs2008-visual/_cmake/modules/testU_WGET&quot;"
            + "&quot;-P&quot; &quot;E:/foo/sources/modules/testU/WGET-testU-noMD5.cmake&quot;</FullCommandLine>"
            + "<Results>"
            + "<NamedMeasurement type=\"text/string\" name=\"Exit Code\">"
            + "<Value>Failed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Exit Value\">"
            + "<Value>1</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"numeric/double\" name=\"Execution Time\">"
            + "<Value>0.0820084</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Completion Status\">"
            + "<Value>Completed</Value>"
            + "</NamedMeasurement>"
            + "<NamedMeasurement type=\"text/string\" name=\"Command Line\">"
            + "<Value>E:\\Tools\\cmake\\cmake-2.8.11-rc4-win32-x86\\bin\\cmake.exe &quot;-DTEST_OUTPUT_DIR:PATH=E:/foo/build-vs2008-visual/_cmake/modules/testU_WGET&quot;"
            + "&quot;-P&quot; &quot;E:/foo/sources/modules/testU/WGET-testU-noMD5.cmake&quot;</Value>"
            + "</NamedMeasurement>"
            + "<Measurement>"
            + "<Value>-- Download of file://\\abc-mang.md5.txt"
            + "failed with message: [37]&quot;couldn&apos;t read a file:// file&quot;"
            + "CMake Error at modules/Logging.cmake:121 (message):"
            + ""
            + ""
            + "test BAR_wget_file succeed: result is &quot;OFF&quot; instead of &quot;ON&quot;"
            + ""
            + "Call Stack (most recent call first):"
            + "modules/Test.cmake:74 (BAR_msg_fatal)"
            + "modules/testU/WGET-testU-noMD5.cmake:14 (BAR_check_equal)"
            + ""
            + ""
            + "</Value>"
            + "</Measurement>"
            + "</Results>"
            + "</Test>"
            + "<EndDateTime>Jan 03 17:51 India Standard Time</EndDateTime>"
            + "<EndTestTime>1546518079</EndTestTime>"
            + "<ElapsedMinutes>6</ElapsedMinutes>"
            + "</Testing>"
            + "</Site>";

        #endregion
    }
}