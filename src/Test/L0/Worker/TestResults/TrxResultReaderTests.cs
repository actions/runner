using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.TestResults;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.TestResults
{
    public class TrxReaderTests : IDisposable
    {
        private string _trxResultFile;
        private Mock<IExecutionContext> _ec;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ResultsWithoutTestNamesAreSkipped()
        {
            SetupMocks();
            string trxContents = "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" +
               "<TestRun>" +
               "<Results>" +
               "<UnitTestResult testId=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\" testName=\"\" outcome=\"Passed\" />" +
               "<UnitTestResult testId=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\" outcome=\"Passed\" />" +
               "</Results>" +
               "<TestDefinitions>" +
               "<UnitTest id=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\">" +
               "<TestMethod  name=\"\" />" +
               "<TestMethod />" +
               "</UnitTest>" +
               "</TestDefinitions>" +
               "</TestRun>";

            _trxResultFile = "resultsWithoutTestNames.trx";
            File.WriteAllText(_trxResultFile, trxContents);
            TrxResultReader reader = new TrxResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _trxResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri", "My Run Title"));

            Assert.NotNull(runData);
            Assert.Equal(0, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PendingOutcomeTreatedAsNotExecuted()
        {
            SetupMocks();
            String trxContents = "<?xml version = \"1.0\" encoding = \"UTF-8\"?>" +
               "<TestRun id = \"ee3d8b3b-1ac9-4a7e-abfa-3d3ed2008613\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" start = \"2015-03-20T16:53:32.3349628+05:30\" finish = \"2015-03-20T16:53:32.9232329+05:30\" />" +
                 "<TestDefinitions>" +
                   "<UnitTest name = \"TestMethod2\" storage = \"c:/users/somerandomusername/source/repos/projectx/unittestproject4/unittestproject4/bin/debug/unittestproject4.dll\" priority = \"1\" id = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\"><Owners><Owner name = \"asdf2\" /></Owners><Execution id = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" /><TestMethod codeBase = \"c:/users/somerandomusername/source/repos/projectx/unittestproject4/unittestproject4/bin/debug/unittestproject4.dll\" adapterTypeName = \"Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAdapter\" className = \"UnitTestProject4.UnitTest1\" name = \"TestMethod2\" /></UnitTest>" +
                   "<WebTest name=\"PSD_Startseite\" storage=\"c:\\vsoagent\\a284d2cc\\vseqa1\\psd_startseite.webtest\" id=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" persistedWebTest=\"7\"><TestCategory><TestCategoryItem TestCategory=\"PSD\" /></TestCategory><Execution id=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" /></WebTest>" +
                   "<OrderedTest name=\"OrderedTest1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\orderedtest1.orderedtest\" id=\"4eb63268-af79-48f1-b625-05ef09b0301a\"><Execution id=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestLinks><TestLink id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /><TestLink id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /></TestLinks></OrderedTest>" +
                   "<UnitTest name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\"><Execution id=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod1\" /></UnitTest>" +
                   "<UnitTest name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" priority=\"1\" id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\"><Execution id=\"5918f7d4-4619-4869-b777-71628227c62a\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod2\" /></UnitTest>" +
                 "</TestDefinitions>" +

                 "<Results>" +
                   "<UnitTestResult executionId = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" testId = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\" testName = \"TestMethod2\" computerName = \"SOMERANDOMCOMPUTERNAME\" duration = \"00:00:00.0834563\" startTime = \"2015-03-20T16:53:32.3099353+05:30\" endTime = \"2015-03-20T16:53:32.3939623+05:30\" testType = \"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome = \"Pending\" testListId = \"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" ><Output><StdOut>Show console log output.</StdOut><ErrorInfo><Message>Assert.Fail failed.</Message><StackTrace>at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21</StackTrace></ErrorInfo></Output>" +
                     "<ResultFiles><ResultFile path=\"DIGANR-DEV4\\x.txt\" /></ResultFiles>" +
                   "</UnitTestResult>" +
                   "<WebTestResult executionId=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" testId=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" testName=\"PSD_Startseite\" computerName=\"LAB-BUILDVNEXT\" duration=\"00:00:01.6887389\" startTime=\"2015-05-20T18:53:51.1063165+00:00\" endTime=\"2015-05-20T18:54:03.9160742+00:00\" testType=\"4e7599fa-5ecb-43e9-a887-cd63cf72d207\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"eb421c16-4546-435a-9c24-0d2878ea76d4\"><Output><StdOut>Do not show console log output.</StdOut></Output>" +
                     "<ResultFiles>" +
                       "<ResultFile path=\"PSD_Startseite.webtestResult\" />" +
                     "</ResultFiles>" +
                     "<WebTestResultFilePath>LOCAL SERVICE_LAB-BUILDVNEXT 2015-05-20 18_53_41\\In\\eb421c16-4546-435a-9c24-0d2878ea76d4\\PSD_Startseite.webtestResult</WebTestResultFilePath>" +
                   "</WebTestResult>" +
                   "<TestResultAggregation executionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"4eb63268-af79-48f1-b625-05ef09b0301a\" testName=\"OrderedTest1\" computerName=\"random-DT\" duration=\"00:00:01.4031295\" startTime=\"2017-12-14T16:27:24.2216619+05:30\" endTime=\"2017-12-14T16:27:25.6423256+05:30\" testType=\"ec4800e8-40e5-4ab3-8510-b8bf29b1904d\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\">" +
                     "<InnerResults>" +
                       "<UnitTestResult executionId=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" testName=\"01- CodedUITestMethod1 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.3658086\" startTime=\"2017-12-14T10:57:24.2386920+05:30\" endTime=\"2017-12-14T10:57:25.3440342+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" />" +
                       "<UnitTestResult executionId=\"5918f7d4-4619-4869-b777-71628227c62a\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" testName=\"02- CodedUITestMethod2 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.0448870\" startTime=\"2017-12-14T10:57:25.3480349+05:30\" endTime=\"2017-12-14T10:57:25.3950371+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"5918f7d4-4619-4869-b777-71628227c62a\" />" +
                     "</InnerResults>" +
                   "</TestResultAggregation>" +
                 "</Results>" +

                 "<ResultSummary outcome=\"Failed\"><Counters total = \"3\" executed = \"3\" passed=\"2\" failed=\"1\" error=\"0\" timeout=\"0\" aborted=\"0\" inconclusive=\"0\" passedButRunAborted=\"0\" notRunnable=\"0\" notExecuted=\"0\" disconnected=\"0\" warning=\"0\" completed=\"0\" inProgress=\"0\" pending=\"0\" />" +
                 "</ResultSummary>" +
               "</TestRun>";

            var runData = GetTestRunData(trxContents, null, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));

            Assert.Equal(runData.Results.Length, 3);

            Assert.Equal(runData.Results[0].Outcome, "NotExecuted");
            Assert.Equal(runData.Results[0].TestCaseTitle, "TestMethod2");
            Assert.Equal(runData.Results[1].Outcome, "Passed");
            Assert.Equal(runData.Results[1].TestCaseTitle, "PSD_Startseite");
            Assert.Equal(runData.Results[2].Outcome, "Passed");
            Assert.Equal(runData.Results[2].TestCaseTitle, "OrderedTest1");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ThereAreNoResultsWithInvalidGuid()
        {
            SetupMocks();
            String trxContents = "<?xml version = \"1.0\" encoding = \"UTF-8\"?>" +
               "<TestRun id = \"asdf\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" start = \"2015-03-20T16:53:32.3349628+05:30\" finish = \"2015-03-20T16:53:32.9232329+05:30\" />" +

                 "<TestDefinitions>" +
                   "<UnitTest name = \"TestMethod2\" storage = \"c:\\users\\somerandomusername\\source\\repos\\projectx\\unittestproject4\\unittestproject4\\bin\\debug\\unittestproject4.dll\" priority = \"1\" id = \"asdf\"><Owners><Owner name = \"asdf2\" /></Owners><Execution id = \"asdf\" /><TestMethod codeBase = \"C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\bin\\Debug\\UnitTestProject4.dll\" adapterTypeName = \"Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAdapter\" className = \"UnitTestProject4.UnitTest1\" name = \"TestMethod2\" /></UnitTest>" +
                 "</TestDefinitions>" +
                  "<Results>" +
                   "<UnitTestResult executionId = \"asdf\" testId = \"asdf\" testName = \"TestMethod2\" computerName = \"SOMERANDOMCOMPUTERNAME\" duration = \"00:00:00.0834563\" startTime = \"2015-03-20T16:53:32.3099353+05:30\" endTime = \"2015-03-20T16:53:32.3939623+05:30\" testType = \"asfd\" outcome = \"Failed\" testListId = \"asdf\" relativeResultsDirectory = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" ><Output><StdOut>Show console log output.</StdOut><ErrorInfo><Message>Assert.Fail failed.</Message><StackTrace>at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21</StackTrace></ErrorInfo></Output>" +
                     "<ResultFiles><ResultFile path=\"DIGANR-DEV4\\x.txt\" /></ResultFiles>" +
                   "</UnitTestResult>" +
                   "</Results></TestRun>";

            _trxResultFile = "ResultsWithInvalidGuid.trx";
            File.WriteAllText(_trxResultFile, trxContents);
            TrxResultReader reader = new TrxResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _trxResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri", "My Run Title"));

            Assert.NotNull(runData);
            Assert.Equal(1, runData.Results.Length);
            Assert.Equal(null, runData.Results[0].AutomatedTestId);
            Assert.Equal(null, runData.Results[0].AutomatedTestTypeId);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ResultsWithInvalidStartDate()
        {
            SetupMocks();
            String trxContents = "<?xml version = \"1.0\" encoding = \"UTF-8\"?>" +
               "<TestRun id = \"asdf\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" start = \"2015-03-20T16:53:32.3349628+05:30\" finish = \"2015-03-20T16:53:32.9232329+05:30\" />" +

                 "<TestDefinitions>" +
                   "<UnitTest name = \"TestMethod2\" storage = \"c:\\users\\somerandomusername\\source\\repos\\projectx\\unittestproject4\\unittestproject4\\bin\\debug\\unittestproject4.dll\" priority = \"1\" id = \"asdf\"><Owners><Owner name = \"asdf2\" /></Owners><Execution id = \"asdf\" /><TestMethod codeBase = \"C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\bin\\Debug\\UnitTestProject4.dll\" adapterTypeName = \"Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAdapter\" className = \"UnitTestProject4.UnitTest1\" name = \"TestMethod2\" /></UnitTest>" +
                 "</TestDefinitions>" +
                  "<Results>" +
                   "<UnitTestResult executionId = \"asdf\" testId = \"asdf\" testName = \"TestMethod2\" computerName = \"SOMERANDOMCOMPUTERNAME\" duration = \"00:00:00.0834563\" startTime = \"0001-01-01T00:00:00.0000000-08:00\" endTime = \"0001-01-01T00:00:00.0000000-08:00\" testType = \"asfd\" outcome = \"Failed\" testListId = \"asdf\" relativeResultsDirectory = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" ><Output><StdOut>Show console log output.</StdOut><ErrorInfo><Message>Assert.Fail failed.</Message><StackTrace>at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21</StackTrace></ErrorInfo></Output>" +
                     "<ResultFiles><ResultFile path=\"DIGANR-DEV4\\x.txt\" /></ResultFiles>" +
                   "</UnitTestResult>" +
                   "</Results></TestRun>";

            _trxResultFile = "ResultsWithInvalidGuid.trx";
            File.WriteAllText(_trxResultFile, trxContents);
            TrxResultReader reader = new TrxResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _trxResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri", "My Run Title"));

            Assert.NotNull(runData);
            Assert.Equal(1, runData.Results.Length);
            Assert.Equal(true, DateTime.Compare((DateTime) SqlDateTime.MinValue, runData.Results[0].StartedDate) < 0);
            Assert.Equal(true, DateTime.Compare((DateTime) SqlDateTime.MinValue, runData.Results[0].CompletedDate) < 0);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ResultsWithoutMandatoryFieldsAreSkipped()
        {
            SetupMocks();
            string trxContents = "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" +
               "<TestRun>" +
               "<Results>" +
               "<UnitTestResult testId=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\" testName=\"\" outcome=\"Passed\" />" +
               "<UnitTestResult testId=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\" outcome=\"Passed\" />" +
               "</Results>" +
               "<TestDefinitions>" +
               "<UnitTest id=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\">" +
               "<TestMethod  name=\"\" />" +
               "<TestMethod />" +
               "</UnitTest>" +
               "</TestDefinitions>" +
               "</TestRun>";

            _trxResultFile = "resultsWithoutMandatoryFields.trx";
            File.WriteAllText(_trxResultFile, trxContents);
            TrxResultReader reader = new TrxResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _trxResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri", "My Run Title"));

            Assert.NotNull(runData);
            Assert.Equal(0, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadsResultsReturnsCorrectValues()
        {
            SetupMocks();
            String trxContents = "<?xml version = \"1.0\" encoding = \"UTF-8\"?>" +
               "<TestRun id = \"ee3d8b3b-1ac9-4a7e-abfa-3d3ed2008613\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" start = \"2015-03-20T16:53:32.3349628+05:30\" finish = \"2015-03-20T16:53:32.9232329+05:30\" />" +

                 "<TestDefinitions>" +
                   "<UnitTest name = \"TestMethod2\" storage = \"c:/users/somerandomusername/source/repos/projectx/unittestproject4/unittestproject4/bin/debug/unittestproject4.dll\" priority = \"1\" id = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\"><Owners><Owner name = \"asdf2\" /></Owners><Execution id = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" /><TestMethod codeBase = \"c:/users/somerandomusername/source/repos/projectx/unittestproject4/unittestproject4/bin/debug/unittestproject4.dll\" adapterTypeName = \"Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAdapter\" className = \"UnitTestProject4.UnitTest1\" name = \"TestMethod2\" /></UnitTest>" +
                   "<WebTest name=\"PSD_Startseite\" storage=\"c:\\vsoagent\\a284d2cc\\vseqa1\\psd_startseite.webtest\" id=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" persistedWebTest=\"7\"><TestCategory><TestCategoryItem TestCategory=\"PSD\" /></TestCategory><Execution id=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" /></WebTest>" +
                   "<OrderedTest name=\"OrderedTest1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\orderedtest1.orderedtest\" id=\"4eb63268-af79-48f1-b625-05ef09b0301a\"><Execution id=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestLinks><TestLink id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /><TestLink id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /></TestLinks></OrderedTest>" +
                   "<UnitTest name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\"><Execution id=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod1\" /></UnitTest>" +
                   "<UnitTest name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" priority=\"1\" id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\"><Execution id=\"5918f7d4-4619-4869-b777-71628227c62a\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod2\" /></UnitTest>" +
                 "</TestDefinitions>" +

                 "<TestSettings name=\"TestSettings1\" id=\"e9d264e9-30da-48df-aa95-c6b53f699464\"><Description>These are default test settings for a local test run.</Description>" +
                   "<Execution>" +
                     "<AgentRule name=\"LocalMachineDefaultRole\">" +
                       "<DataCollectors>" +
                         "<DataCollector uri=\"datacollector://microsoft/CodeCoverage/1.0\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestTools.CodeCoverage.CoveragePlugIn, Microsoft.VisualStudio.QualityTools.Plugins.CodeCoverage, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" friendlyName=\"Code Coverage (Visual Studio 2010)\">" +
                           "<Configuration><CodeCoverage xmlns=\"\"><Regular>" +
                             "<CodeCoverageItem binaryFile=\"C:\\mstest.static.UnitTestProject3.dll\" pdbFile=\"C:\\mstest.static.UnitTestProject3.instr.pdb\" instrumentInPlace=\"true\" />" +
                           "</Regular></CodeCoverage></Configuration>" +
                         "</DataCollector>" +
                       "</DataCollectors>" +
                     "</AgentRule>" +
                   "</Execution>" +
                 "</TestSettings>" +

                 "<Results>" +
                   "<UnitTestResult executionId = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" testId = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\" testName = \"TestMethod2\" computerName = \"SOMERANDOMCOMPUTERNAME\" duration = \"00:00:00.0834563\" startTime = \"2015-03-20T16:53:32.3099353+05:30\" endTime = \"2015-03-20T16:53:32.3939623+05:30\" testType = \"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome = \"Failed\" testListId = \"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" ><Output><StdOut>Show console log output.</StdOut><StdErr>This is standard error message.</StdErr><ErrorInfo><Message>Assert.Fail failed.</Message><StackTrace>at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21</StackTrace></ErrorInfo></Output>" +
                     "<ResultFiles><ResultFile path=\"DIGANR-DEV4\\x.txt\" /></ResultFiles>" +
                   "</UnitTestResult>" +

                   "<WebTestResult executionId=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" testId=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" testName=\"PSD_Startseite\" computerName=\"LAB-BUILDVNEXT\" duration=\"00:00:01.6887389\" startTime=\"2015-05-20T18:53:51.1063165+00:00\" endTime=\"2015-05-20T18:54:03.9160742+00:00\" testType=\"4e7599fa-5ecb-43e9-a887-cd63cf72d207\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"eb421c16-4546-435a-9c24-0d2878ea76d4\"><Output><StdOut>Do not show console log output.</StdOut></Output>" +
                     "<ResultFiles>" +
                       "<ResultFile path=\"PSD_Startseite.webtestResult\" />" +
                     "</ResultFiles>" +
                     "<WebTestResultFilePath>LOCAL SERVICE_LAB-BUILDVNEXT 2015-05-20 18_53_41\\In\\eb421c16-4546-435a-9c24-0d2878ea76d4\\PSD_Startseite.webtestResult</WebTestResultFilePath>" +
                   "</WebTestResult>" +
                   "<TestResultAggregation executionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"4eb63268-af79-48f1-b625-05ef09b0301a\" testName=\"OrderedTest1\" computerName=\"random-DT\" duration=\"00:00:01.4031295\" startTime=\"2017-12-14T16:27:24.2216619+05:30\" endTime=\"2017-12-14T16:27:25.6423256+05:30\" testType=\"ec4800e8-40e5-4ab3-8510-b8bf29b1904d\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\">" +
                     "<InnerResults>" +
                       "<UnitTestResult executionId=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" testName=\"01- CodedUITestMethod1 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.3658086\" startTime=\"2017-12-14T10:57:24.2386920+05:30\" endTime=\"2017-12-14T10:57:25.3440342+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" />" +
                       "<UnitTestResult executionId=\"5918f7d4-4619-4869-b777-71628227c62a\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" testName=\"02- CodedUITestMethod2 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.0448870\" startTime=\"2017-12-14T10:57:25.3480349+05:30\" endTime=\"2017-12-14T10:57:25.3950371+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"5918f7d4-4619-4869-b777-71628227c62a\" />" +
                     "</InnerResults>" +
                   "</TestResultAggregation>" +
                 "</Results>" +

                 "<ResultSummary outcome=\"Failed\"><Counters total = \"3\" executed = \"3\" passed=\"2\" failed=\"1\" error=\"0\" timeout=\"0\" aborted=\"0\" inconclusive=\"0\" passedButRunAborted=\"0\" notRunnable=\"0\" notExecuted=\"0\" disconnected=\"0\" warning=\"0\" completed=\"0\" inProgress=\"0\" pending=\"0\" />" +

                   "<CollectorDataEntries>" +
                     "<Collector agentName=\"DIGANR-DEV4\" uri=\"datacollector://microsoft/CodeCoverage/2.0\" collectorDisplayName=\"Code Coverage\"><UriAttachments><UriAttachment>" +
                       "<A href=\"DIGANR-DEV4\\vstest_console.dynamic.data.coverage\"></A></UriAttachment></UriAttachments>" +
                     "</Collector>" +
                     "<Collector agentName=\"DIGANR-DEV4\" uri=\"datacollector://microsoft/CodeCoverage/1.0\" collectorDisplayName=\"MSTestAdapter\"><UriAttachments>" +
                       "<UriAttachment><A href=\"DIGANR-DEV4\\unittestproject3.dll\">c:\\vstest.static.unittestproject3.dll</A></UriAttachment>" +
                       "<UriAttachment><A href=\"DIGANR-DEV4\\UnitTestProject3.instr.pdb\">C:\\vstest.static.UnitTestProject3.instr.pdb</A></UriAttachment>" +
                     "</UriAttachments></Collector>" +
                   "</CollectorDataEntries>" +

                   "<ResultFiles>" +
                     "<ResultFile path=\"vstest_console.static.data.coverage\" /></ResultFiles>" +
                     "<ResultFile path=\"DIGANR-DEV4\\mstest.static.data.coverage\" />" +
                   "</ResultSummary>" +

               "</TestRun>";

            var runData = GetTestRunData(trxContents, null, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));

            DateTime StartedDate;
            DateTime.TryParse("2015-03-20T16:53:32.3099353+05:30", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out StartedDate);
            Assert.Equal(runData.Results[0].StartedDate, StartedDate);

            TimeSpan Duration;
            TimeSpan.TryParse("00:00:00.0834563", out Duration);
            Assert.Equal(runData.Results[0].DurationInMs, Duration.TotalMilliseconds);

            DateTime CompletedDate = StartedDate.AddTicks(Duration.Ticks);
            Assert.Equal(runData.Results[0].CompletedDate, CompletedDate);

            Assert.Equal(runData.Name, "VSTest Test Run debug any cpu");
            Assert.Equal(runData.State, "InProgress");
            Assert.Equal(runData.Results.Length, 3);

            Assert.Equal(runData.Results[0].Outcome, "Failed");
            Assert.Equal(runData.Results[0].TestCaseTitle, "TestMethod2");
            Assert.Equal(runData.Results[0].ComputerName, "SOMERANDOMCOMPUTERNAME");
            Assert.Equal(runData.Results[0].AutomatedTestType, "UnitTest");
            Assert.Equal(runData.Results[0].AutomatedTestName, "UnitTestProject4.UnitTest1.TestMethod2");
            Assert.Equal(runData.Results[0].AutomatedTestId, "f0d6b58f-dc08-9c0b-aab7-0a1411d4a346");
            Assert.Equal(runData.Results[0].AutomatedTestStorage, "unittestproject4.dll");
            Assert.Equal(runData.Results[0].AutomatedTestTypeId, "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b");
            Assert.Equal(runData.Results[0].ErrorMessage, "Assert.Fail failed.");
            Assert.Equal(runData.Results[0].StackTrace, "at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21");
            Assert.Equal(runData.Results[0].Priority.ToString(), "1");
            Assert.Equal(runData.Results[0].AttachmentData.ConsoleLog, "Show console log output.");
            Assert.Equal(runData.Results[0].AttachmentData.StandardError, "This is standard error message.");
            Assert.Equal(runData.Results[0].AttachmentData.AttachmentsFilePathList.Count, 1);
            Assert.True(runData.Results[0].AttachmentData.AttachmentsFilePathList[0].Contains("x.txt"));

            Assert.Equal(runData.Results[1].Outcome, "Passed");
            Assert.Equal(runData.Results[1].TestCaseTitle, "PSD_Startseite");
            Assert.Equal(runData.Results[1].ComputerName, "LAB-BUILDVNEXT");
            Assert.Equal(runData.Results[1].AutomatedTestType, "WebTest");
            Assert.Equal(runData.Results[1].AttachmentData.ConsoleLog, null);
            Assert.Equal(runData.Results[1].AttachmentData.AttachmentsFilePathList.Count, 1);
            Assert.True(runData.Results[1].AttachmentData.AttachmentsFilePathList[0].Contains("PSD_Startseite.webtestResult"));

            Assert.Equal(runData.Results[2].Outcome, "Passed");
            Assert.Equal(runData.Results[2].TestCaseTitle, "OrderedTest1");
            Assert.Equal(runData.Results[2].ComputerName, "random-DT");
            Assert.Equal(runData.Results[2].AutomatedTestType, "OrderedTest");

            Assert.Equal(runData.BuildFlavor, "debug");
            Assert.Equal(runData.BuildPlatform, "any cpu");
            // 3 files related mstest.static, 3 files related to vstest.static, and 1 file for vstest.dynamic 
            Assert.Equal(runData.Attachments.Length, 7);

            int buildId;
            int.TryParse(runData.Build.Id, out buildId);
            Assert.Equal(buildId, 1);

            Assert.Equal(runData.ReleaseUri, "releaseUri");
            Assert.Equal(runData.ReleaseEnvironmentUri, "releaseEnvironmentUri");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadsResultsReturnsCorrectValuesForHierarchicalResults()
        {
            SetupMocks();
            String trxContents = "<?xml version = \"1.0\" encoding = \"UTF-8\"?>"
                + "<TestRun id = \"ee3d8b3b-1ac9-4a7e-abfa-3d3ed2008613\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" start = \"2015-03-20T16:53:32.3349628+05:30\" finish = \"2015-03-20T16:53:32.9232329+05:30\" />"
                + "<TestDefinitions>"
                + "<UnitTest name = \"TestMethod2\" storage = \"c:/users/somerandomusername/source/repos/projectx/unittestproject4/unittestproject4/bin/debug/unittestproject4.dll\" priority = \"1\" id = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\"><Owners><Owner name = \"asdf2\" /></Owners><Execution id = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" /><TestMethod codeBase = \"c:/users/somerandomusername/source/repos/projectx/unittestproject4/unittestproject4/bin/debug/unittestproject4.dll\" adapterTypeName = \"Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAdapter\" className = \"UnitTestProject4.UnitTest1\" name = \"TestMethod2\" />"
                + "</UnitTest>"
                + "<WebTest name=\"PSD_Startseite\" storage=\"c:\\vsoagent\\a284d2cc\\vseqa1\\psd_startseite.webtest\" id=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" persistedWebTest=\"7\"><TestCategory><TestCategoryItem TestCategory=\"PSD\" /></TestCategory><Execution id=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" /></WebTest>"
                + "<OrderedTest name=\"OrderedTest1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\orderedtest1.orderedtest\" id=\"4eb63268-af79-48f1-b625-05ef09b0301a\"><Execution id=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestLinks><TestLink id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /><TestLink id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /></TestLinks></OrderedTest>"
                + "<UnitTest name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\"><Execution id=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod1\" /></UnitTest>"
                + "<UnitTest name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" priority=\"1\" id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\"><Execution id=\"5918f7d4-4619-4869-b777-71628227c62a\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod2\" /></UnitTest>"
                + "</TestDefinitions>"
                + "<TestSettings name=\"TestSettings1\" id=\"e9d264e9-30da-48df-aa95-c6b53f699464\"><Description>These are default test settings for a local test run.</Description>"
                + "<Execution>"
                + "<AgentRule name=\"LocalMachineDefaultRole\">"
                + "<DataCollectors>"
                + "<DataCollector uri=\"datacollector://microsoft/CodeCoverage/1.0\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestTools.CodeCoverage.CoveragePlugIn, Microsoft.VisualStudio.QualityTools.Plugins.CodeCoverage, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" friendlyName=\"Code Coverage (Visual Studio 2010)\">"
                + "<Configuration><CodeCoverage xmlns=\"\"><Regular>"
                + "<CodeCoverageItem binaryFile=\"C:\\mstest.static.UnitTestProject3.dll\" pdbFile=\"C:\\mstest.static.UnitTestProject3.instr.pdb\" instrumentInPlace=\"true\" />"
                + "</Regular></CodeCoverage></Configuration>"
                + "</DataCollector>"
                + "</DataCollectors>"
                + "</AgentRule>"
                + "</Execution>"
                + "</TestSettings>"
                + "<Results>"
                + "<UnitTestResult executionId = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" testId = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\" testName = \"TestMethod2\" computerName = \"SOMERANDOMCOMPUTERNAME\" duration = \"00:00:00.0834563\" startTime = \"2015-03-20T16:53:32.3099353+05:30\" endTime = \"2015-03-20T16:53:32.3939623+05:30\" testType = \"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome = \"Failed\" testListId = \"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" ><Output><StdOut>Show console log output.</StdOut><StdErr>This is standard error message.</StdErr><ErrorInfo><Message>Assert.Fail failed.</Message><StackTrace>at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21</StackTrace></ErrorInfo></Output>"
                + "<ResultFiles><ResultFile path=\"DIGANR-DEV4\\x.txt\" /></ResultFiles>"
                + "<InnerResults>"
                + "<UnitTestResult executionId = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" testId = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\" testName = \"SubResult 1 (TestMethod2)\" computerName = \"SOMERANDOMCOMPUTERNAME\" duration = \"00:00:00.0834563\" startTime = \"2015-03-20T16:53:32.3099353+05:30\" endTime = \"2015-03-20T16:53:32.3939623+05:30\" testType = \"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome = \"Failed\" testListId = \"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" ><Output><StdOut>Show console log output.</StdOut><StdErr>This is standard error message.</StdErr><ErrorInfo><Message>Assert.Fail failed.</Message><StackTrace>at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21</StackTrace></ErrorInfo></Output>"
                + "<ResultFiles><ResultFile path=\"DIGANR-DEV4\\x.txt\" /></ResultFiles>"
                + "</UnitTestResult>"
                + "</InnerResults>"
                + "</UnitTestResult>"
                + "<WebTestResult executionId=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" testId=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" testName=\"PSD_Startseite\" computerName=\"LAB-BUILDVNEXT\" duration=\"00:00:01.6887389\" startTime=\"2015-05-20T18:53:51.1063165+00:00\" endTime=\"2015-05-20T18:54:03.9160742+00:00\" testType=\"4e7599fa-5ecb-43e9-a887-cd63cf72d207\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"eb421c16-4546-435a-9c24-0d2878ea76d4\"><Output><StdOut>Do not show console log output.</StdOut></Output>"
                + "<ResultFiles>"
                + "<ResultFile path=\"PSD_Startseite.webtestResult\" />"
                + "</ResultFiles>"
                + "<WebTestResultFilePath>LOCAL SERVICE_LAB-BUILDVNEXT 2015-05-20 18_53_41\\In\\eb421c16-4546-435a-9c24-0d2878ea76d4\\PSD_Startseite.webtestResult</WebTestResultFilePath>"
                + "</WebTestResult>"
                + "<TestResultAggregation executionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"4eb63268-af79-48f1-b625-05ef09b0301a\" testName=\"OrderedTest1\" computerName=\"random-DT\" duration=\"00:00:01.4031295\" startTime=\"2017-12-14T16:27:24.2216619+05:30\" endTime=\"2017-12-14T16:27:25.6423256+05:30\" testType=\"ec4800e8-40e5-4ab3-8510-b8bf29b1904d\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\">"
                + "<InnerResults>"
                + "<UnitTestResult executionId=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" testName=\"01- CodedUITestMethod1 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.3658086\" startTime=\"2017-12-14T10:57:24.2386920+05:30\" endTime=\"2017-12-14T10:57:25.3440342+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" />"
                + "<UnitTestResult executionId=\"5918f7d4-4619-4869-b777-71628227c62a\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" testName=\"02- CodedUITestMethod2 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.0448870\" startTime=\"2017-12-14T10:57:25.3480349+05:30\" endTime=\"2017-12-14T10:57:25.3950371+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"5918f7d4-4619-4869-b777-71628227c62a\" />"
                + "</InnerResults>"
                + "</TestResultAggregation>"
                + "</Results>"
                + "<ResultSummary outcome=\"Failed\"><Counters total = \"3\" executed = \"3\" passed=\"2\" failed=\"1\" error=\"0\" timeout=\"0\" aborted=\"0\" inconclusive=\"0\" passedButRunAborted=\"0\" notRunnable=\"0\" notExecuted=\"0\" disconnected=\"0\" warning=\"0\" completed=\"0\" inProgress=\"0\" pending=\"0\" />"
                + "<CollectorDataEntries>"
                + "<Collector agentName=\"DIGANR-DEV4\" uri=\"datacollector://microsoft/CodeCoverage/2.0\" collectorDisplayName=\"Code Coverage\"><UriAttachments><UriAttachment>"
                + "<A href=\"DIGANR-DEV4\\vstest_console.dynamic.data.coverage\"></A></UriAttachment></UriAttachments>"
                + "</Collector>"
                + "<Collector agentName=\"DIGANR-DEV4\" uri=\"datacollector://microsoft/CodeCoverage/1.0\" collectorDisplayName=\"MSTestAdapter\"><UriAttachments>"
                + "<UriAttachment><A href=\"DIGANR-DEV4\\unittestproject3.dll\">c:\\vstest.static.unittestproject3.dll</A></UriAttachment>"
                + "<UriAttachment><A href=\"DIGANR-DEV4\\UnitTestProject3.instr.pdb\">C:\\vstest.static.UnitTestProject3.instr.pdb</A></UriAttachment>"
                + "</UriAttachments></Collector>"
                + "</CollectorDataEntries>"
                + "<ResultFiles>"
                + "<ResultFile path=\"vstest_console.static.data.coverage\" /></ResultFiles>"
                + "<ResultFile path=\"DIGANR-DEV4\\mstest.static.data.coverage\" />"
                + "</ResultSummary>"
                + "</TestRun>";

            var testRunData = GetTestRunData(trxContents, null, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));

            DateTime.TryParse("2015-03-20T16:53:32.3099353+05:30", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime StartedDate);
            Assert.Equal(testRunData.Results[0].StartedDate, StartedDate);

            TimeSpan.TryParse("00:00:00.0834563", out TimeSpan Duration);
            Assert.Equal(testRunData.Results[0].DurationInMs, Duration.TotalMilliseconds);

            DateTime CompletedDate = StartedDate.AddTicks(Duration.Ticks);
            Assert.Equal(testRunData.Results[0].CompletedDate, CompletedDate);

            Assert.Equal(testRunData.Results.Length, 3);

            Assert.Equal(testRunData.Results[0].Outcome, "Failed");
            Assert.Equal(testRunData.Results[0].TestCaseTitle, "TestMethod2");
            Assert.Equal(testRunData.Results[0].ComputerName, "SOMERANDOMCOMPUTERNAME");
            Assert.Equal(testRunData.Results[0].AutomatedTestType, "UnitTest");
            Assert.Equal(testRunData.Results[0].AutomatedTestName, "UnitTestProject4.UnitTest1.TestMethod2");
            Assert.Equal(testRunData.Results[0].AutomatedTestId, "f0d6b58f-dc08-9c0b-aab7-0a1411d4a346");
            Assert.Equal(testRunData.Results[0].AutomatedTestStorage, "unittestproject4.dll");
            Assert.Equal(testRunData.Results[0].AutomatedTestTypeId, "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b");
            Assert.Equal(testRunData.Results[0].ErrorMessage, "Assert.Fail failed.");
            Assert.Equal(testRunData.Results[0].StackTrace, "at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21");
            Assert.Equal(testRunData.Results[0].Priority.ToString(), "1");
            Assert.Equal(testRunData.Results[0].AttachmentData.ConsoleLog, "Show console log output.");
            Assert.Equal(testRunData.Results[0].AttachmentData.StandardError, "This is standard error message.");
            Assert.Equal(testRunData.Results[0].AttachmentData.AttachmentsFilePathList.Count, 1);
            Assert.True(testRunData.Results[0].AttachmentData.AttachmentsFilePathList[0].Contains("x.txt"));

            // Testing subResult properties
            Assert.Equal(testRunData.Results[0].TestCaseSubResultData.Count, 1);
            Assert.Equal(testRunData.Results[0].TestCaseSubResultData[0].DisplayName, "SubResult 1 (TestMethod2)");
            Assert.Equal(testRunData.Results[0].TestCaseSubResultData[0].ComputerName, "SOMERANDOMCOMPUTERNAME");
            Assert.Equal(testRunData.Results[0].TestCaseSubResultData[0].Outcome, "Failed");
            Assert.Equal(testRunData.Results[0].TestCaseSubResultData[0].ErrorMessage, "Assert.Fail failed.");
            Assert.Equal(testRunData.Results[0].TestCaseSubResultData[0].StackTrace, "at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21");
            Assert.Equal(testRunData.Results[0].TestCaseSubResultData[0].AttachmentData.ConsoleLog, "Show console log output.");
            Assert.Equal(testRunData.Results[0].TestCaseSubResultData[0].AttachmentData.StandardError, "This is standard error message.");
            Assert.Equal(testRunData.Results[0].TestCaseSubResultData[0].AttachmentData.AttachmentsFilePathList.Count, 1);
            Assert.True(testRunData.Results[0].TestCaseSubResultData[0].AttachmentData.AttachmentsFilePathList[0].Contains("x.txt"));


            Assert.Equal(testRunData.Results[1].Outcome, "Passed");
            Assert.Equal(testRunData.Results[1].TestCaseTitle, "PSD_Startseite");
            Assert.Equal(testRunData.Results[1].ComputerName, "LAB-BUILDVNEXT");
            Assert.Equal(testRunData.Results[1].AutomatedTestType, "WebTest");
            Assert.Equal(testRunData.Results[1].AttachmentData.ConsoleLog, null);
            Assert.Equal(testRunData.Results[1].AttachmentData.AttachmentsFilePathList.Count, 1);
            Assert.True(testRunData.Results[1].AttachmentData.AttachmentsFilePathList[0].Contains("PSD_Startseite.webtestResult"));

            Assert.Equal(testRunData.Results[2].Outcome, "Passed");
            Assert.Equal(testRunData.Results[2].TestCaseTitle, "OrderedTest1");
            Assert.Equal(testRunData.Results[2].ComputerName, "random-DT");
            Assert.Equal(testRunData.Results[2].AutomatedTestType, "OrderedTest");

            // Testing subResult properties
            Assert.Equal(testRunData.Results[2].TestCaseSubResultData.Count, 2);
            Assert.Equal(testRunData.Results[2].TestCaseSubResultData[0].DisplayName, "01- CodedUITestMethod1 (OrderedTest1)");
            Assert.Equal(testRunData.Results[2].TestCaseSubResultData[1].DisplayName, "02- CodedUITestMethod2 (OrderedTest1)");
            Assert.Equal(testRunData.Results[2].TestCaseSubResultData[0].Outcome, "Passed");
            Assert.Equal(testRunData.Results[2].TestCaseSubResultData[1].Outcome, "Passed");
            // 3 files related mstest.static, 3 files related to vstest.static, and 1 file for vstest.dynamic 
            Assert.Equal(testRunData.Attachments.Length, 7);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadsResultsReturnsCorrectValuesInDifferentCulture()
        {
            SetupMocks();
            CultureInfo current = CultureInfo.CurrentCulture;
            try
            {
                //German is used, as in this culture decimal seperator is comma & thousand seperator is dot
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                String trxContents = "<?xml version = \"1.0\" encoding = \"UTF-8\"?>" +
               "<TestRun id = \"ee3d8b3b-1ac9-4a7e-abfa-3d3ed2008613\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" start = \"2015-03-20T16:53:32.3349628+05:30\" finish = \"2015-03-20T16:53:32.9232329+05:30\" />" +

                 "<TestDefinitions>" +
                   "<UnitTest name = \"TestMethod2\" storage = \"c:/users/somerandomusername/source/repos/projectx/unittestproject4/unittestproject4/bin/debug/unittestproject4.dll\" priority = \"1\" id = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\"><Owners><Owner name = \"asdf2\" /></Owners><Execution id = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" /><TestMethod codeBase = \"c:/users/somerandomusername/source/repos/projectx/unittestproject4/unittestproject4/bin/debug/unittestproject4.dll\" adapterTypeName = \"Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAdapter\" className = \"UnitTestProject4.UnitTest1\" name = \"TestMethod2\" /></UnitTest>" +
                   "<WebTest name=\"PSD_Startseite\" storage=\"c:\\vsoagent\\a284d2cc\\vseqa1\\psd_startseite.webtest\" id=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" persistedWebTest=\"7\"><TestCategory><TestCategoryItem TestCategory=\"PSD\" /></TestCategory><Execution id=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" /></WebTest>" +
                   "<OrderedTest name=\"OrderedTest1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\orderedtest1.orderedtest\" id=\"4eb63268-af79-48f1-b625-05ef09b0301a\"><Execution id=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestLinks><TestLink id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /><TestLink id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /></TestLinks></OrderedTest>" +
                   "<UnitTest name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\"><Execution id=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod1\" /></UnitTest>" +
                   "<UnitTest name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" priority=\"1\" id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\"><Execution id=\"5918f7d4-4619-4869-b777-71628227c62a\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod2\" /></UnitTest>" +
                 "</TestDefinitions>" +

                 "<TestSettings name=\"TestSettings1\" id=\"e9d264e9-30da-48df-aa95-c6b53f699464\"><Description>These are default test settings for a local test run.</Description>" +
                   "<Execution>" +
                     "<AgentRule name=\"LocalMachineDefaultRole\">" +
                       "<DataCollectors>" +
                         "<DataCollector uri=\"datacollector://microsoft/CodeCoverage/1.0\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestTools.CodeCoverage.CoveragePlugIn, Microsoft.VisualStudio.QualityTools.Plugins.CodeCoverage, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" friendlyName=\"Code Coverage (Visual Studio 2010)\">" +
                           "<Configuration><CodeCoverage xmlns=\"\"><Regular>" +
                             "<CodeCoverageItem binaryFile=\"C:\\mstest.static.UnitTestProject3.dll\" pdbFile=\"C:\\mstest.static.UnitTestProject3.instr.pdb\" instrumentInPlace=\"true\" />" +
                           "</Regular></CodeCoverage></Configuration>" +
                         "</DataCollector>" +
                       "</DataCollectors>" +
                     "</AgentRule>" +
                   "</Execution>" +
                 "</TestSettings>" +

                 "<Results>" +
                   "<UnitTestResult executionId = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" testId = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\" testName = \"TestMethod2\" computerName = \"SOMERANDOMCOMPUTERNAME\" duration = \"00:00:00.0834563\" startTime = \"2015-03-20T16:53:32.3099353+05:30\" endTime = \"2015-03-20T16:53:32.3939623+05:30\" testType = \"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome = \"Failed\" testListId = \"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" ><Output><StdOut>Show console log output.</StdOut><StdErr>This is standard error message.</StdErr><ErrorInfo><Message>Assert.Fail failed.</Message><StackTrace>at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21</StackTrace></ErrorInfo></Output>" +
                     "<ResultFiles><ResultFile path=\"DIGANR-DEV4\\x.txt\" /></ResultFiles>" +
                   "</UnitTestResult>" +

                   "<WebTestResult executionId=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" testId=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" testName=\"PSD_Startseite\" computerName=\"LAB-BUILDVNEXT\" duration=\"00:00:01.6887389\" startTime=\"2015-05-20T18:53:51.1063165+00:00\" endTime=\"2015-05-20T18:54:03.9160742+00:00\" testType=\"4e7599fa-5ecb-43e9-a887-cd63cf72d207\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"eb421c16-4546-435a-9c24-0d2878ea76d4\"><Output><StdOut>Do not show console log output.</StdOut></Output>" +
                     "<ResultFiles>" +
                       "<ResultFile path=\"PSD_Startseite.webtestResult\" />" +
                     "</ResultFiles>" +
                     "<WebTestResultFilePath>LOCAL SERVICE_LAB-BUILDVNEXT 2015-05-20 18_53_41\\In\\eb421c16-4546-435a-9c24-0d2878ea76d4\\PSD_Startseite.webtestResult</WebTestResultFilePath>" +
                   "</WebTestResult>" +
                   "<TestResultAggregation executionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"4eb63268-af79-48f1-b625-05ef09b0301a\" testName=\"OrderedTest1\" computerName=\"random-DT\" duration=\"00:00:01.4031295\" startTime=\"2017-12-14T16:27:24.2216619+05:30\" endTime=\"2017-12-14T16:27:25.6423256+05:30\" testType=\"ec4800e8-40e5-4ab3-8510-b8bf29b1904d\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\">" +
                     "<InnerResults>" +
                       "<UnitTestResult executionId=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" testName=\"01- CodedUITestMethod1 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.3658086\" startTime=\"2017-12-14T10:57:24.2386920+05:30\" endTime=\"2017-12-14T10:57:25.3440342+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" />" +
                       "<UnitTestResult executionId=\"5918f7d4-4619-4869-b777-71628227c62a\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" testName=\"02- CodedUITestMethod2 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.0448870\" startTime=\"2017-12-14T10:57:25.3480349+05:30\" endTime=\"2017-12-14T10:57:25.3950371+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"5918f7d4-4619-4869-b777-71628227c62a\" />" +
                     "</InnerResults>" +
                   "</TestResultAggregation>" +
                 "</Results>" +

                 "<ResultSummary outcome=\"Failed\"><Counters total = \"3\" executed = \"3\" passed=\"2\" failed=\"1\" error=\"0\" timeout=\"0\" aborted=\"0\" inconclusive=\"0\" passedButRunAborted=\"0\" notRunnable=\"0\" notExecuted=\"0\" disconnected=\"0\" warning=\"0\" completed=\"0\" inProgress=\"0\" pending=\"0\" />" +

                   "<CollectorDataEntries>" +
                     "<Collector agentName=\"DIGANR-DEV4\" uri=\"datacollector://microsoft/CodeCoverage/2.0\" collectorDisplayName=\"Code Coverage\"><UriAttachments><UriAttachment>" +
                       "<A href=\"DIGANR-DEV4\\vstest_console.dynamic.data.coverage\"></A></UriAttachment></UriAttachments>" +
                     "</Collector>" +
                     "<Collector agentName=\"DIGANR-DEV4\" uri=\"datacollector://microsoft/CodeCoverage/1.0\" collectorDisplayName=\"MSTestAdapter\"><UriAttachments>" +
                       "<UriAttachment><A href=\"DIGANR-DEV4\\unittestproject3.dll\">c:\\vstest.static.unittestproject3.dll</A></UriAttachment>" +
                       "<UriAttachment><A href=\"DIGANR-DEV4\\UnitTestProject3.instr.pdb\">C:\\vstest.static.UnitTestProject3.instr.pdb</A></UriAttachment>" +
                     "</UriAttachments></Collector>" +
                   "</CollectorDataEntries>" +

                   "<ResultFiles>" +
                     "<ResultFile path=\"vstest_console.static.data.coverage\" /></ResultFiles>" +
                     "<ResultFile path=\"DIGANR-DEV4\\mstest.static.data.coverage\" />" +
                   "</ResultSummary>" +

               "</TestRun>";

                var runData = GetTestRunData(trxContents, null, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));

                DateTime StartedDate;
                DateTime.TryParse("2015-03-20T16:53:32.3099353+05:30", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out StartedDate);
                Assert.Equal(runData.Results[0].StartedDate, StartedDate);

                TimeSpan Duration;
                TimeSpan.TryParse("00:00:00.0834563", out Duration);
                Assert.Equal(runData.Results[0].DurationInMs, Duration.TotalMilliseconds);

                DateTime CompletedDate = StartedDate.AddTicks(Duration.Ticks);
                Assert.Equal(runData.Results[0].CompletedDate, CompletedDate);

                Assert.Equal(runData.Name, "VSTest Test Run debug any cpu");
                Assert.Equal(runData.State, "InProgress");
                Assert.Equal(runData.Results.Length, 3);

                Assert.Equal(runData.Results[0].Outcome, "Failed");
                Assert.Equal(runData.Results[0].TestCaseTitle, "TestMethod2");
                Assert.Equal(runData.Results[0].ComputerName, "SOMERANDOMCOMPUTERNAME");
                Assert.Equal(runData.Results[0].AutomatedTestType, "UnitTest");
                Assert.Equal(runData.Results[0].AutomatedTestName, "UnitTestProject4.UnitTest1.TestMethod2");
                Assert.Equal(runData.Results[0].AutomatedTestId, "f0d6b58f-dc08-9c0b-aab7-0a1411d4a346");
                Assert.Equal(runData.Results[0].AutomatedTestStorage, "unittestproject4.dll");
                Assert.Equal(runData.Results[0].AutomatedTestTypeId, "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b");
                Assert.Equal(runData.Results[0].ErrorMessage, "Assert.Fail failed.");
                Assert.Equal(runData.Results[0].StackTrace, "at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21");
                Assert.Equal(runData.Results[0].Priority.ToString(), "1");
                Assert.Equal(runData.Results[0].AttachmentData.ConsoleLog, "Show console log output.");
                Assert.Equal(runData.Results[0].AttachmentData.StandardError, "This is standard error message.");
                Assert.Equal(runData.Results[0].AttachmentData.AttachmentsFilePathList.Count, 1);
                Assert.True(runData.Results[0].AttachmentData.AttachmentsFilePathList[0].Contains("x.txt"));

                Assert.Equal(runData.Results[1].Outcome, "Passed");
                Assert.Equal(runData.Results[1].TestCaseTitle, "PSD_Startseite");
                Assert.Equal(runData.Results[1].ComputerName, "LAB-BUILDVNEXT");
                Assert.Equal(runData.Results[1].AutomatedTestType, "WebTest");
                Assert.Equal(runData.Results[1].AttachmentData.ConsoleLog, null);
                Assert.Equal(runData.Results[1].AttachmentData.AttachmentsFilePathList.Count, 1);
                Assert.True(runData.Results[1].AttachmentData.AttachmentsFilePathList[0].Contains("PSD_Startseite.webtestResult"));

                Assert.Equal(runData.Results[2].Outcome, "Passed");
                Assert.Equal(runData.Results[2].TestCaseTitle, "OrderedTest1");
                Assert.Equal(runData.Results[2].ComputerName, "random-DT");
                Assert.Equal(runData.Results[2].AutomatedTestType, "OrderedTest");

                Assert.Equal(runData.BuildFlavor, "debug");
                Assert.Equal(runData.BuildPlatform, "any cpu");
                // 3 files related mstest.static, 3 files related to vstest.static, and 1 file for vstest.dynamic 
                Assert.Equal(runData.Attachments.Length, 7);

                int buildId;
                int.TryParse(runData.Build.Id, out buildId);
                Assert.Equal(buildId, 1);

                Assert.Equal(runData.ReleaseUri, "releaseUri");
                Assert.Equal(runData.ReleaseEnvironmentUri, "releaseEnvironmentUri");
            }
            finally
            {
                CultureInfo.CurrentCulture = current;
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void CustomRunTitleIsHonoured()
        {
            SetupMocks();
            string trxContents = "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" +
                "<TestRun>" +
                "<Results>" +
                "<UnitTestResult testId=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\" testName=\"TestMethod1\" outcome=\"Passed\" />" +
                "</Results>" +
                "<TestDefinitions>" +
                "<UnitTest id=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\">" +
                "<TestMethod className=\"UnitTestProject1.UnitTest1\" name=\"TestMethod1\" />" +
                "</UnitTest>" +
                "</TestDefinitions>" +
                "</TestRun>";

            _trxResultFile = "resultsWithCustomRunTitle.trx";
            File.WriteAllText(_trxResultFile, trxContents);
            TrxResultReader reader = new TrxResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _trxResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri", "My Run Title"));

            Assert.Equal(runData.Name, "My Run Title");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadResultsDoesNotFailForBareMinimumTrx()
        {
            SetupMocks();
            string trxContents = "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" +
                "<TestRun>" +
                "<Results>" +
                "<UnitTestResult testId=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\" testName=\"TestMethod1\" outcome=\"Inconclusive\" />" +
                "</Results>" +
                "<TestDefinitions>" +
                "<UnitTest id=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\">" +
                "<TestMethod className=\"UnitTestProject1.UnitTest1\" name=\"TestMethod1\" />" +
                "</UnitTest>" +
                "</TestDefinitions>" +
                "</TestRun>";
            _trxResultFile = "bareMinimum.trx";
            File.WriteAllText(_trxResultFile, trxContents);
            TrxResultReader reader = new TrxResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _trxResultFile, new TestRunContext(null, null, null, 1, null, null, null));

            Assert.Equal(runData.Results.Length, 1);
            Assert.Equal(runData.Results[0].Outcome, "Inconclusive");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadResultsDoesNotFailWithoutStartTime()
        {
            SetupMocks();
            string trxContents = "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" +
                "<TestRun id = \"ee3d8b3b-1ac9-4a7e-abfa-3d3ed2008613\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" finish = \"2015-03-20T16:53:32.9232329+05:30\" />" +
                "<Results>" +
                "<UnitTestResult testId=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\" testName=\"TestMethod1\" outcome=\"Passed\" />" +
                "</Results>" +
                "<TestDefinitions>" +
                "<UnitTest id=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\">" +
                "<TestMethod className=\"UnitTestProject1.UnitTest1\" name=\"TestMethod1\" />" +
                "</UnitTest>" +
                "</TestDefinitions>" +
                "</TestRun>";
            _trxResultFile = "resultsWithoutStartTime.trx";
            File.WriteAllText(_trxResultFile, trxContents);
            TrxResultReader reader = new TrxResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _trxResultFile, new TestRunContext(null, null, null, 1, null, null, null));

            Assert.Equal(runData.Results.Length, 1);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadResultsDoesNotFailWithoutFinishTime()
        {
            SetupMocks();
            string trxContents = "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" +
                "<TestRun id = \"ee3d8b3b-1ac9-4a7e-abfa-3d3ed2008613\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" start = \"2015-03-20T16:53:32.3349628+05:30\" />" +
                "<Results>" +
                "<UnitTestResult testId=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\" testName=\"TestMethod1\" outcome=\"Passed\" />" +
                "</Results>" +
                "<TestDefinitions>" +
                "<UnitTest id=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\">" +
                "<TestMethod className=\"UnitTestProject1.UnitTest1\" name=\"TestMethod1\" />" +
                "</UnitTest>" +
                "</TestDefinitions>" +
                "</TestRun>";
            _trxResultFile = "resultsWithoutFinishTime";
            File.WriteAllText(_trxResultFile, trxContents);
            TrxResultReader reader = new TrxResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _trxResultFile, new TestRunContext(null, null, null, 1, null, null, null));

            Assert.Equal(runData.Results.Length, 1);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadResultsDoesNotFailWithFinishTimeLessThanStartTime()
        {
            SetupMocks();
            var runData = GetTestRunDataBasic();
            Assert.Equal(1, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyRunLevelResultsFilePresentByDefault()
        {
            SetupMocks();
            var runData = GetTestRunDataBasic();
            Assert.Equal(_trxResultFile, runData.Attachments[0]);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyRunLevelResultsFileAbsentIfSkipFlagIsSet()
        {
            SetupMocks();
            var myReader = new TrxResultReader() { AddResultsFileToRunLevelAttachments = false };
            var runData = GetTestRunDataBasic(myReader);
            Assert.Equal(0, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyResultsFilesAreAddedAsRunLevelAttachments()
        {
            SetupMocks();
            var runData = GetTestRunDataWithAttachments(2);
            Assert.Equal(4, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyCoverageSourceFilesAndPdbsAreAddedAsRunLevelAttachments()
        {
            SetupMocks();
            var runData = GetTestRunDataWithAttachments(1);
            Assert.Equal(3, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyCoverageSourceFilesAndPdbsAreAddedAsRunLevelAttachmentsWithDeployment()
        {
            SetupMocks();
            var runData = GetTestRunDataWithAttachments(13);
            Assert.Equal(3, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyDataCollectorFilesAndPdbsAreAddedAsRunLevelAttachments()
        {
            SetupMocks();
            var runData = GetTestRunDataWithAttachments(0);
            Assert.Equal(3, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyNoDataCollectorFilesAndPdbsAreAddedAsRunLevelAttachmentsIfSkipFlagIsSet()
        {
            SetupMocks();
            var myReader = new TrxResultReader() { AddResultsFileToRunLevelAttachments = false };
            var runData = GetTestRunDataWithAttachments(0, myReader);
            Assert.Equal(0, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyNoResultsFilesAreAddedAsRunLevelAttachmentsIfSkipFlagIsSet()
        {
            SetupMocks();
            var myReader = new TrxResultReader() { AddResultsFileToRunLevelAttachments = false };
            var runData = GetTestRunDataWithAttachments(2, myReader);
            Assert.Equal(0, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyNoCoverageSourceFilesAndPdbsAreAddedAsRunLevelAttachmentsIfSkipFlagIsSet()
        {
            SetupMocks();
            var myReader = new TrxResultReader() { AddResultsFileToRunLevelAttachments = false };
            var runData = GetTestRunDataWithAttachments(1, myReader);
            Assert.Equal(0, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyRunTypeIsSet()
        {
            SetupMocks();
            var runData = GetTestRunDataWithAttachments(0);
            Assert.Equal("unittest".ToLowerInvariant(), runData.Results[0].AutomatedTestType.ToLowerInvariant());
        }
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyReadResultsReturnsCorrectTestStartAndEndDateTime()
        {
            SetupMocks();
            String trxContents = "<?xml version = \"1.0\" encoding = \"UTF-8\"?>" +
               "<TestRun id = \"ee3d8b3b-1ac9-4a7e-abfa-3d3ed2008613\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" start = \"2015-03-20T16:53:32.3349628+05:30\" finish = \"2015-03-20T16:53:32.9232329+05:30\" />" +

                 "<TestDefinitions>" +
                   "<UnitTest name = \"TestMethod2\" storage = \"c:\\users\\somerandomusername\\source\\repos\\projectx\\unittestproject4\\unittestproject4\\bin\\debug\\unittestproject4.dll\" priority = \"1\" id = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\"><Owners><Owner name = \"asdf2\" /></Owners><Execution id = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" /><TestMethod codeBase = \"C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\bin\\Debug\\UnitTestProject4.dll\" adapterTypeName = \"Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAdapter\" className = \"UnitTestProject4.UnitTest1\" name = \"TestMethod2\" /></UnitTest>" +
                   "<WebTest name=\"PSD_Startseite\" storage=\"c:\\vsoagent\\a284d2cc\\vseqa1\\psd_startseite.webtest\" id=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" persistedWebTest=\"7\"><TestCategory><TestCategoryItem TestCategory=\"PSD\" /></TestCategory><Execution id=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" /></WebTest>" +
                   "<OrderedTest name=\"OrderedTest1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\orderedtest1.orderedtest\" id=\"4eb63268-af79-48f1-b625-05ef09b0301a\"><Execution id=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestLinks><TestLink id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /><TestLink id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /></TestLinks></OrderedTest>" +
                   "<UnitTest name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\"><Execution id=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod1\" /></UnitTest>" +
                   "<UnitTest name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" priority=\"1\" id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\"><Execution id=\"5918f7d4-4619-4869-b777-71628227c62a\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod2\" /></UnitTest>" +
                 "</TestDefinitions>" +

                 "<Results>" +
                   "<UnitTestResult executionId = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" testId = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\" testName = \"TestMethod2\" computerName = \"SOMERANDOMCOMPUTERNAME\" duration = \"00:00:00.0834563\" startTime = \"2015-03-20T16:53:32.3099353+05:30\" endTime = \"2015-03-20T16:53:32.3939623+05:30\" testType = \"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome = \"Failed\" testListId = \"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" ><Output><StdOut>Show console log output.</StdOut><ErrorInfo><Message>Assert.Fail failed.</Message><StackTrace>at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21</StackTrace></ErrorInfo></Output>" +
                     "<ResultFiles><ResultFile path=\"DIGANR-DEV4\\x.txt\" /></ResultFiles>" +
                   "</UnitTestResult>" +

                   "<WebTestResult executionId=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" testId=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" testName=\"PSD_Startseite\" computerName=\"LAB-BUILDVNEXT\" duration=\"00:00:01.6887389\" startTime=\"2015-05-20T18:53:51.1063165+00:00\" endTime=\"2015-05-20T18:54:03.9160742+00:00\" testType=\"4e7599fa-5ecb-43e9-a887-cd63cf72d207\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"eb421c16-4546-435a-9c24-0d2878ea76d4\"><Output><StdOut>Do not show console log output.</StdOut></Output>" +
                     "<ResultFiles>" +
                       "<ResultFile path=\"PSD_Startseite.webtestResult\" />" +
                     "</ResultFiles>" +
                     "<WebTestResultFilePath>LOCAL SERVICE_LAB-BUILDVNEXT 2015-05-20 18_53_41\\In\\eb421c16-4546-435a-9c24-0d2878ea76d4\\PSD_Startseite.webtestResult</WebTestResultFilePath>" +
                   "</WebTestResult>" +
                   "<TestResultAggregation executionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"4eb63268-af79-48f1-b625-05ef09b0301a\" testName=\"OrderedTest1\" computerName=\"random-DT\" duration=\"00:00:01.4031295\" startTime=\"2017-12-14T16:27:24.2216619+05:30\" endTime=\"2017-12-14T16:27:25.6423256+05:30\" testType=\"ec4800e8-40e5-4ab3-8510-b8bf29b1904d\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\">" +
                     "<InnerResults>" +
                       "<UnitTestResult executionId=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" testName=\"01- CodedUITestMethod1 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.3658086\" startTime=\"2017-12-14T10:57:24.2386920+05:30\" endTime=\"2017-12-14T10:57:25.3440342+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" />" +
                       "<UnitTestResult executionId=\"5918f7d4-4619-4869-b777-71628227c62a\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" testName=\"02- CodedUITestMethod2 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.0448870\" startTime=\"2017-12-14T10:57:25.3480349+05:30\" endTime=\"2017-12-14T10:57:25.3950371+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"5918f7d4-4619-4869-b777-71628227c62a\" />" +
                     "</InnerResults>" +
                   "</TestResultAggregation>" +
                 "</Results>" +

                 "<ResultSummary outcome=\"Failed\"><Counters total = \"3\" executed = \"3\" passed=\"2\" failed=\"1\" error=\"0\" timeout=\"0\" aborted=\"0\" inconclusive=\"0\" passedButRunAborted=\"0\" notRunnable=\"0\" notExecuted=\"0\" disconnected=\"0\" warning=\"0\" completed=\"0\" inProgress=\"0\" pending=\"0\" />" +
                   "<ResultFiles>" +
                     "<ResultFile path=\"vstest_console.static.data.coverage\" /></ResultFiles>" +
                   "</ResultSummary>" +

               "</TestRun>";

            var runData = GetTestRunData(trxContents, null, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));

            DateTime StartedDate;
            DateTime.TryParse("2015-03-20T16:53:32.3349628+05:30", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out StartedDate);
            Assert.Equal(runData.StartDate, StartedDate.ToString("o"));

            DateTime CompletedDate;
            DateTime.TryParse("2015-03-20T16:53:32.9232329+05:30", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out CompletedDate);
            Assert.Equal(runData.CompleteDate, CompletedDate.ToString("o"));
        }
        public void Dispose()
        {
            try
            {
                File.Delete(_trxResultFile);
            }
            catch
            {
            }
        }

        private TestRunData GetTestRunDataBasic(TrxResultReader myReader = null)
        {
            string trxContents = "<?xml version =\"1.0\" encoding=\"UTF-8\"?>" +
                                 "<TestRun id = \"ee3d8b3b-1ac9-4a7e-abfa-3d3ed2008613\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" start = \"2015-03-20T16:53:32.3349628+05:30\" finish = \"2014-03-20T16:53:32.3349628+05:30\" />" +
                                 "<Results>" +
                                 "<UnitTestResult testId=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\" testName=\"TestMethod1\" outcome=\"Passed\" />" +
                                 "</Results>" +
                                 "<TestDefinitions>" +
                                 "<UnitTest id=\"fd1a9d66-d059-cd84-23d7-f655dce255f5\">" +
                                 "<TestMethod className=\"UnitTestProject1.UnitTest1\" name=\"TestMethod1\" />" +
                                 "</UnitTest>" +
                                 "</TestDefinitions>" +
                                 "</TestRun>";

            return GetTestRunData(trxContents, myReader);
        }

        private TestRunData GetTestRunDataWithAttachments(int val, TrxResultReader myReader = null, TestRunContext trContext = null)
        {
            var trxContents = "<?xml version = \"1.0\" encoding = \"UTF-8\"?>" +
              "<TestRun id = \"ee3d8b3b-1ac9-4a7e-abfa-3d3ed2008613\" name = \"somerandomusername@SOMERANDOMCOMPUTERNAME 2015-03-20 16:53:32\" runUser = \"FAREAST\\somerandomusername\" xmlns =\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Times creation = \"2015-03-20T16:53:32.3309380+05:30\" queuing = \"2015-03-20T16:53:32.3319381+05:30\" start = \"2015-03-20T16:53:32.3349628+05:30\" finish = \"2015-03-20T16:53:32.9232329+05:30\" />" +

                "<TestDefinitions>" +
                  "<UnitTest name = \"TestMethod2\" storage = \"c:\\users\\somerandomusername\\source\\repos\\projectx\\unittestproject4\\unittestproject4\\bin\\debug\\unittestproject4.dll\" priority = \"1\" id = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\"><Owners><Owner name = \"asdf2\" /></Owners><Execution id = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" /><TestMethod codeBase = \"C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\bin\\Debug\\UnitTestProject4.dll\" adapterTypeName = \"Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAdapter\" className = \"UnitTestProject4.UnitTest1\" name = \"TestMethod2\" /></UnitTest>" +
                  "<WebTest name=\"PSD_Startseite\" storage=\"c:\\vsoagent\\a284d2cc\\vseqa1\\psd_startseite.webtest\" id=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" persistedWebTest=\"7\"><TestCategory><TestCategoryItem TestCategory=\"PSD\" /></TestCategory><Execution id=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" /></WebTest>" +
                   "<OrderedTest name=\"OrderedTest1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\orderedtest1.orderedtest\" id=\"4eb63268-af79-48f1-b625-05ef09b0301a\"><Execution id=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestLinks><TestLink id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /><TestLink id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" /></TestLinks></OrderedTest>" +
                   "<UnitTest name=\"CodedUITestMethod1\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" id=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\"><Execution id=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod1\" /></UnitTest>" +
                   "<UnitTest name=\"CodedUITestMethod2\" storage=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" priority=\"1\" id=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\"><Execution id=\"5918f7d4-4619-4869-b777-71628227c62a\" parentId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" /><TestMethod codeBase=\"c:\\users\\random\\source\\repos\\codeduitestproject1\\codeduitestproject1\\bin\\debug\\codeduitestproject1.dll\" adapterTypeName=\"executor://orderedtestadapter/v1\" className=\"CodedUITestProject1.CodedUITest1\" name=\"CodedUITestMethod2\" /></UnitTest>" +
                "</TestDefinitions>" +

                "<TestSettings name=\"TestSettings1\" id=\"e9d264e9-30da-48df-aa95-c6b53f699464\"><Description>These are default test settings for a local test run.</Description>" +
                  "<Execution>" +
                    "<AgentRule name=\"LocalMachineDefaultRole\">" +
                      "<DataCollectors>" +
                        "<DataCollector uri=\"datacollector://microsoft/CodeCoverage/1.0\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestTools.CodeCoverage.CoveragePlugIn, Microsoft.VisualStudio.QualityTools.Plugins.CodeCoverage, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" friendlyName=\"Code Coverage (Visual Studio 2010)\">" +
                          "<Configuration><CodeCoverage xmlns=\"\"><Regular>" +
                            "<CodeCoverageItem binaryFile=\"C:\\mstest.static.UnitTestProject3.dll\" pdbFile=\"C:\\mstest.static.UnitTestProject3.instr.pdb\" instrumentInPlace=\"true\" />" +
                          "</Regular></CodeCoverage></Configuration>" +
                        "</DataCollector>" +
                      "</DataCollectors>" +
                    "</AgentRule>" +
                  "</Execution>" +
                   "{3}" +
                "</TestSettings>" +

                "{0}" +
                "{1}" +

                "<ResultSummary outcome=\"Failed\"><Counters total = \"3\" executed = \"3\" passed=\"2\" failed=\"1\" error=\"0\" timeout=\"0\" aborted=\"0\" inconclusive=\"0\" passedButRunAborted=\"0\" notRunnable=\"0\" notExecuted=\"0\" disconnected=\"0\" warning=\"0\" completed=\"0\" inProgress=\"0\" pending=\"0\" />" +

                "{2}" +

                  "</ResultSummary>" +
              "</TestRun>";

            var part0 = "<Results>" +
                  "<UnitTestResult executionId = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" testId = \"f0d6b58f-dc08-9c0b-aab7-0a1411d4a346\" testName = \"TestMethod2\" computerName = \"SOMERANDOMCOMPUTERNAME\" duration = \"00:00:00.0834563\" startTime = \"2015-03-20T16:53:32.3099353+05:30\" endTime = \"2015-03-20T16:53:32.3939623+05:30\" testType = \"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome = \"Failed\" testListId = \"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory = \"48ec1e47-b9df-43b9-aef2-a2cc8742353d\" ><Output><ErrorInfo><Message>Assert.Fail failed.</Message><StackTrace>at UnitTestProject4.UnitTest1.TestMethod2() in C:\\Users\\somerandomusername\\Source\\Repos\\Projectx\\UnitTestProject4\\UnitTestProject4\\UnitTest1.cs:line 21</StackTrace></ErrorInfo></Output>" +
                    "<ResultFiles><ResultFile path=\"DIGANR-DEV4\\x.txt\" /></ResultFiles>" +
                  "</UnitTestResult>" +

                  "<WebTestResult executionId=\"eb421c16-4546-435a-9c24-0d2878ea76d4\" testId=\"01da1a13-b160-4ee6-9d84-7a6dfe37b1d2\" testName=\"PSD_Startseite\" computerName=\"LAB-BUILDVNEXT\" duration=\"00:00:01.6887389\" startTime=\"2015-05-20T18:53:51.1063165+00:00\" endTime=\"2015-05-20T18:54:03.9160742+00:00\" testType=\"4e7599fa-5ecb-43e9-a887-cd63cf72d207\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"eb421c16-4546-435a-9c24-0d2878ea76d4\">" +
                    "<ResultFiles>" +
                      "<ResultFile path=\"PSD_Startseite.webtestResult\" />" +
                    "</ResultFiles>" +
                    "<WebTestResultFilePath>LOCAL SERVICE_LAB-BUILDVNEXT 2015-05-20 18_53_41\\In\\eb421c16-4546-435a-9c24-0d2878ea76d4\\PSD_Startseite.webtestResult</WebTestResultFilePath>" +
                  "</WebTestResult>" +
                  "<TestResultAggregation executionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"4eb63268-af79-48f1-b625-05ef09b0301a\" testName=\"OrderedTest1\" computerName=\"random-DT\" duration=\"00:00:01.4031295\" startTime=\"2017-12-14T16:27:24.2216619+05:30\" endTime=\"2017-12-14T16:27:25.6423256+05:30\" testType=\"ec4800e8-40e5-4ab3-8510-b8bf29b1904d\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\">" +
                    "<InnerResults>" +
                      "<UnitTestResult executionId=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"fd846020-c6f8-3c49-3ed0-fbe1e1fd340b\" testName=\"01- CodedUITestMethod1 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.3658086\" startTime=\"2017-12-14T10:57:24.2386920+05:30\" endTime=\"2017-12-14T10:57:25.3440342+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"4f82d822-cd28-4bcc-b091-b08a66cf92e7\" />" +
                      "<UnitTestResult executionId=\"5918f7d4-4619-4869-b777-71628227c62a\" parentExecutionId=\"20927d24-2eb4-473f-b5b2-f52667b88f6f\" testId=\"1c7ece84-d949-bed1-0a4c-dfad4f9c953e\" testName=\"02- CodedUITestMethod2 (OrderedTest1)\" computerName=\"random-DT\" duration=\"00:00:00.0448870\" startTime=\"2017-12-14T10:57:25.3480349+05:30\" endTime=\"2017-12-14T10:57:25.3950371+05:30\" testType=\"13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b\" outcome=\"Passed\" testListId=\"8c84fa94-04c1-424b-9868-57a2d4851a1d\" relativeResultsDirectory=\"5918f7d4-4619-4869-b777-71628227c62a\" />" +
                    "</InnerResults>" +
                  "</TestResultAggregation>" +
                "</Results>";

            var part1 =
                  "<CollectorDataEntries>" +
                    "<Collector agentName=\"DIGANR-DEV4\" uri=\"datacollector://microsoft/CodeCoverage/2.0\" collectorDisplayName=\"Code Coverage\"><UriAttachments><UriAttachment>" +
                      "<A href=\"DIGANR-DEV4\\vstest_console.dynamic.data.coverage\"></A></UriAttachment></UriAttachments>" +
                    "</Collector>" +
                    "<Collector agentName=\"DIGANR-DEV4\" uri=\"datacollector://microsoft/CodeCoverage/1.0\" collectorDisplayName=\"MSTestAdapter\"><UriAttachments>" +
                      "<UriAttachment><A href=\"DIGANR-DEV4\\unittestproject3.dll\">c:\\vstest.static.unittestproject3.dll</A></UriAttachment>" +
                      "<UriAttachment><A href=\"DIGANR-DEV4\\UnitTestProject3.instr.pdb\">C:\\vstest.static.UnitTestProject3.instr.pdb</A></UriAttachment>" +
                    "</UriAttachments></Collector>" +
                  "</CollectorDataEntries>";

            var part2 = "<ResultFiles>" +
                    "<ResultFile path=\"vstest_console.static.data.coverage\" /></ResultFiles>" +
                    "<ResultFile path=\"DIGANR-DEV4\\mstest.static.data.coverage\" />";

            var part3 = "<Deployment runDeploymentRoot=\"results\"></Deployment>";


            switch (val)
            {
                case 0:
                    trxContents = string.Format(trxContents, part0, string.Empty, string.Empty, string.Empty);
                    break;
                case 1:
                    trxContents = string.Format(trxContents, string.Empty, part1, string.Empty, string.Empty);
                    break;
                case 2:
                    trxContents = string.Format(trxContents, string.Empty, string.Empty, part2, string.Empty);
                    break;
                case 3:
                    trxContents = string.Format(trxContents, string.Empty, string.Empty, string.Empty, string.Empty);
                    break;
                case 13:
                    trxContents = string.Format(trxContents, string.Empty, part1, string.Empty, part3);
                    break;
                default:
                    trxContents = string.Format(trxContents, part0, part1, part2);
                    break;
            }

            return GetTestRunData(trxContents, myReader, trContext);
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            TestHostContext hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();
            List<string> warnings;
            var variables = new Variables(hc, new Dictionary<string, VariableValue>(), out warnings);
            _ec.Setup(x => x.Variables).Returns(variables);
        }

        private TestRunData GetTestRunData(string trxContents, TrxResultReader myReader = null, TestRunContext trContext = null)
        {
            _trxResultFile = "results.trx";
            File.WriteAllText(_trxResultFile, trxContents);
            var reader = myReader ?? new TrxResultReader();
            var runData = reader.ReadResults(_ec.Object, _trxResultFile,
                trContext ?? new TestRunContext(null, null, null, 1, null, null, null));
            return runData;
        }
    }
}
