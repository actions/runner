using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public class JUnitResultReader : AgentService, IResultReader
    {
        public Type ExtensionType => typeof(IResultReader);
        public string Name => "JUnit";

        public JUnitResultReader()
        {
            AddResultsFileToRunLevelAttachments = true;
        }

        /// <summary>
        /// Reads a JUnit results file from disk, converts it into a TestRunData object.        
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="runContext"></param>
        /// <returns></returns>
        public TestRunData ReadResults(IExecutionContext executionContext, string filePath, TestRunContext runContext = null)
        {
            // http://windyroad.com.au/dl/Open%20Source/JUnit.xsd
            
            XmlDocument doc = new XmlDocument();
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore
                };

                using (XmlReader reader = XmlReader.Create(filePath, settings))
                {
                    doc.Load(reader);
                }
            }
            catch (XmlException ex)
            {
                executionContext.Warning(StringUtil.Loc("FailedToReadFile", filePath, ex.Message));
                return null;
            }

            //init test run summary information - run name, host name, start time
            TestSuiteSummary runSummary = new TestSuiteSummary(Name);

            IdentityRef runUserIdRef = null;
            string runUser = runContext != null ? runContext.Owner : string.Empty;
            if (!string.IsNullOrEmpty(runUser))
            {
                runUserIdRef = new IdentityRef() { DisplayName = runUser };
            }

            var presentTime = DateTime.UtcNow;
            runSummary.TimeStamp = DateTime.MaxValue;
            var maxCompletedTime = DateTime.MinValue;

            //read data from testsuite nodes

            XmlNode testSuitesNode = doc.SelectSingleNode("testsuites");
            if (testSuitesNode != null)
            {
                //found testsuites node - some plugins generate it like karma junit plugin
                XmlNodeList testSuiteNodeList = doc.SelectNodes("/testsuites/testsuite");
                if (testSuiteNodeList != null)
                {
                    foreach (XmlNode testSuiteNode in testSuiteNodeList)
                    {
                        //for each available suites get all suite details
                        TestSuiteSummary testSuiteSummary = ReadTestSuite(testSuiteNode, runUserIdRef);

                        // sum up testsuite durations and test case durations, decision on what to use will be taken later
                        runSummary.TotalTestCaseDuration = runSummary.TotalTestCaseDuration.Add(testSuiteSummary.TotalTestCaseDuration);
                        runSummary.TestSuiteDuration = runSummary.TestSuiteDuration.Add(testSuiteSummary.TestSuiteDuration);
                        runSummary.SuiteTimeDataAvailable = runSummary.SuiteTimeDataAvailable && testSuiteSummary.SuiteTimeDataAvailable;
                        runSummary.SuiteTimeStampAvailable = runSummary.SuiteTimeStampAvailable && testSuiteSummary.SuiteTimeStampAvailable;
                        runSummary.Host = testSuiteSummary.Host;
                        runSummary.Name = testSuiteSummary.Name;
                        //stop calculating timestamp information, if timestamp data is not avilable for even one test suite
                        if (testSuiteSummary.SuiteTimeStampAvailable)
                        {
                            runSummary.TimeStamp = runSummary.TimeStamp > testSuiteSummary.TimeStamp ? testSuiteSummary.TimeStamp : runSummary.TimeStamp;
                            DateTime completedTime = testSuiteSummary.TimeStamp.AddTicks(testSuiteSummary.TestSuiteDuration.Ticks);
                            maxCompletedTime = maxCompletedTime < completedTime ? completedTime : maxCompletedTime;
                        }
                        runSummary.Results.AddRange(testSuiteSummary.Results);
                    }

                    if (testSuiteNodeList.Count > 1)
                    {
                        runSummary.Name = Name + "_" + Path.GetFileName(filePath);
                    }
                }
            }
            else
            {
                XmlNode testSuiteNode = doc.SelectSingleNode("testsuite");
                if (testSuiteNode != null)
                {
                    runSummary = ReadTestSuite(testSuiteNode, runUserIdRef);
                    //only if start time is available then only we need to calculate completed time
                    if (runSummary.TimeStamp != DateTime.MaxValue)
                    {
                        DateTime completedTime = runSummary.TimeStamp.AddTicks(runSummary.TestSuiteDuration.Ticks);
                        maxCompletedTime = maxCompletedTime < completedTime ? completedTime : maxCompletedTime;
                    }
                    else
                    {
                        runSummary.SuiteTimeStampAvailable = false;
                    }
                }
            }

            if (runContext != null && !string.IsNullOrWhiteSpace(runContext.RunName))
            {
                runSummary.Name = runContext.RunName;
            }

            if (!runSummary.SuiteTimeStampAvailable)
            {
                executionContext.Output("Timestamp is not available for one or more testsuites. Total run duration is being calculated as the sum of time durations of detected testsuites");
               
                if (!runSummary.SuiteTimeDataAvailable)
                {
                    executionContext.Output("Time is not available for one or more testsuites. Total run duration is being calculated as the sum of time durations of detected testcases");
                }
            }
            //if start time is not calculated then it should be initialized as present time
            runSummary.TimeStamp = runSummary.TimeStamp == DateTime.MaxValue 
                ? presentTime 
                : runSummary.TimeStamp;
            //if suite timestamp data is not available even for single testsuite, then fallback to testsuite run time
            //if testsuite run time is not available even for single testsuite, then fallback to total test case duration
            maxCompletedTime = !runSummary.SuiteTimeStampAvailable || maxCompletedTime == DateTime.MinValue 
                ? runSummary.TimeStamp.Add(runSummary.SuiteTimeDataAvailable ? runSummary.TestSuiteDuration 
                : runSummary.TotalTestCaseDuration) : maxCompletedTime;
            //create test run data
            var testRunData = new TestRunData(
                name: runSummary.Name,
                startedDate: runSummary.TimeStamp != DateTime.MinValue ? runSummary.TimeStamp.ToString("o") : null,
                completedDate: maxCompletedTime != DateTime.MinValue ? maxCompletedTime.ToString("o") : null,
                state: TestRunState.InProgress.ToString(),
                isAutomated: true,
                buildId: runContext != null ? runContext.BuildId : 0,
                buildFlavor: runContext != null ? runContext.Configuration : string.Empty,
                buildPlatform: runContext != null ? runContext.Platform : string.Empty,
                releaseUri: runContext != null ? runContext.ReleaseUri : null,
                releaseEnvironmentUri: runContext != null ? runContext.ReleaseEnvironmentUri : null
                )
            {
                Results = runSummary.Results.ToArray(),
                Attachments = AddResultsFileToRunLevelAttachments ? new string[] { filePath } : new string[0]

            };

            return testRunData;
        }

        public bool AddResultsFileToRunLevelAttachments
        {
            get;
            set;
        }

        /// <summary>
        /// Read testcases under testsuite node in xml
        /// </summary>
        /// <param name="rootNode"></param>
        private TestSuiteSummary ReadTestSuite(XmlNode rootNode, IdentityRef runUserIdRef)
        {
            TestSuiteSummary testSuiteSummary = new TestSuiteSummary(Name);
            
            XmlNodeList innerTestSuiteNodeList = rootNode.SelectNodes("./testsuite");
            if(innerTestSuiteNodeList != null)
            {
                foreach(XmlNode innerTestSuiteNode in innerTestSuiteNodeList)
                {
                    TestSuiteSummary innerTestSuiteSummary = ReadTestSuite(innerTestSuiteNode , runUserIdRef);
                    testSuiteSummary.Results.AddRange(innerTestSuiteSummary.Results);
                }
            }
            
            TimeSpan totalTestSuiteDuration = TimeSpan.Zero;
            TimeSpan totalTestCaseDuration = TimeSpan.Zero;

            if (rootNode.Attributes["name"] != null && rootNode.Attributes["name"].Value != null && rootNode.Attributes["name"].Value != string.Empty)
            {
                testSuiteSummary.Name = rootNode.Attributes["name"].Value;
            }

            if (rootNode.Attributes["hostname"] != null && rootNode.Attributes["hostname"].Value != null)
            {
                testSuiteSummary.Host = rootNode.Attributes["hostname"].Value;
            }

            //assume runtimes from xml are current local time since timezone information is not in the xml, if xml datetime > current local time, fallback to local start time
            DateTime timestampFromXml = DateTime.MinValue;
            XmlAttribute timestampNode = rootNode.Attributes["timestamp"];
            if (timestampNode != null && timestampNode.Value != null)
            {
                if (DateTime.TryParse(timestampNode.Value, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestampFromXml))
                {
                    testSuiteSummary.TimeStamp = timestampFromXml;
                }
            }

            if (timestampFromXml == DateTime.MinValue)
            {
                testSuiteSummary.SuiteTimeStampAvailable = false;
            }

            bool SuiteTimeDataAvailable = false;
            totalTestSuiteDuration = GetTimeSpan(rootNode, out SuiteTimeDataAvailable);
            testSuiteSummary.SuiteTimeDataAvailable = SuiteTimeDataAvailable;

            var testSuiteStartTime = testSuiteSummary.TimeStamp;

            //find test case nodes in JUnit result xml
            XmlNodeList testCaseNodes = rootNode.SelectNodes("./testcase");
            if (testCaseNodes != null)
            {
                DateTime testCaseStartTime = testSuiteStartTime;

                //Add test case results to the test run
                foreach (XmlNode testCaseNode in testCaseNodes)
                {
                    TestCaseResultData resultCreateModel = new TestCaseResultData();

                    //test case name and type
                    if (testCaseNode.Attributes["name"] != null && testCaseNode.Attributes["name"].Value != null)
                    {
                        resultCreateModel.TestCaseTitle = testCaseNode.Attributes["name"].Value;
                        resultCreateModel.AutomatedTestName = testCaseNode.Attributes["name"].Value;
                    }

                    if (testCaseNode.Attributes["classname"] != null && testCaseNode.Attributes["classname"].Value != null)
                    {
                        resultCreateModel.AutomatedTestStorage = testCaseNode.Attributes["classname"].Value;
                    }

                    if (testCaseNode.Attributes["owner"]?.Value != null)
                    {
                        var ownerName = testCaseNode.Attributes["owner"].Value;
                        resultCreateModel.Owner = new IdentityRef { DisplayName = ownerName, DirectoryAlias = ownerName };
                    }

                    //test case duration
                    bool TestCaseTimeDataAvailable = false;
                    var testCaseDuration = GetTimeSpan(testCaseNode, out TestCaseTimeDataAvailable);
                    totalTestCaseDuration = totalTestCaseDuration + testCaseDuration;
                    resultCreateModel.DurationInMs = testCaseDuration.TotalMilliseconds;
                    resultCreateModel.StartedDate = testCaseStartTime;
                    resultCreateModel.CompletedDate = testCaseStartTime.AddTicks(testCaseDuration.Ticks);
                    testCaseStartTime = testCaseStartTime.AddTicks(1) + testCaseDuration; //next start time

                    //test case outcome
                    XmlNode failure, error, skipped;

                    if ((failure = testCaseNode.SelectSingleNode("./failure")) != null)
                    {
                        ProcessFailureNode(failure, resultCreateModel);
                        AddSystemLogsToResult(testCaseNode, resultCreateModel);
                    }
                    else if ((error = testCaseNode.SelectSingleNode("./error")) != null)
                    {
                        ProcessFailureNode(error, resultCreateModel);
                        AddSystemLogsToResult(testCaseNode, resultCreateModel);
                    }
                    else if ((skipped = testCaseNode.SelectSingleNode("./skipped")) != null)
                    {
                        resultCreateModel.Outcome = TestOutcome.NotExecuted.ToString();
                        if (skipped.Attributes["message"] != null && !string.IsNullOrWhiteSpace(skipped.Attributes["message"].Value))
                        {
                            resultCreateModel.ErrorMessage = skipped.Attributes["message"].Value;
                        }
                    }
                    else
                    {
                        resultCreateModel.Outcome = TestOutcome.Passed.ToString();
                    }

                    resultCreateModel.State = "Completed";

                    resultCreateModel.AutomatedTestType = Name;

                    //other properties - host name and user
                    resultCreateModel.ComputerName = testSuiteSummary.Host;

                    if (runUserIdRef != null)
                    {
                        resultCreateModel.RunBy = runUserIdRef;
                    }

                    if (!string.IsNullOrEmpty(resultCreateModel.AutomatedTestName) && !string.IsNullOrEmpty(resultCreateModel.TestCaseTitle))
                    {
                        testSuiteSummary.Results.Add(resultCreateModel);
                    }

                }
            }

            testSuiteSummary.TestSuiteDuration = totalTestSuiteDuration;
            testSuiteSummary.TotalTestCaseDuration = totalTestCaseDuration;

            return testSuiteSummary;
        }

        private static TimeSpan GetTimeSpan(XmlNode rootNode, out bool TimeDataAvailable)
        {
            var time = TimeSpan.Zero;
            TimeDataAvailable = false;
            if (rootNode.Attributes["time"] != null)
            {
                var timeValue = rootNode.Attributes["time"].Value;
                if (timeValue != null)
                {
                    // Ensure that the time data is a positive value within range
                    if (Double.TryParse(timeValue, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out double timeInSeconds) 
                        && !Double.IsNaN(timeInSeconds) 
                        && !Double.IsInfinity(timeInSeconds)
                        && timeInSeconds >= 0)
                    {
                        time = TimeSpan.FromSeconds(timeInSeconds);
                        TimeDataAvailable = true;
                    }
                }
            }

            return time;
        }

        private void ProcessFailureNode(XmlNode failure, TestCaseResultData resultCreateModel)
        {
            resultCreateModel.Outcome = TestOutcome.Failed.ToString();
            if (failure.Attributes["message"] != null && !string.IsNullOrWhiteSpace(failure.Attributes["message"].Value))
            {
                resultCreateModel.ErrorMessage = failure.Attributes["message"].Value;
            }

            if (!string.IsNullOrWhiteSpace(failure.InnerText))
            {
                resultCreateModel.StackTrace = failure.InnerText;
            }
        }

        private void AddSystemLogsToResult(XmlNode testCaseNode, TestCaseResultData resultCreateModel)
        {
            XmlNode stdout, stderr;

            // Standard output logs
            stdout = testCaseNode.SelectSingleNode("./system-out");

            resultCreateModel.AttachmentData = new AttachmentData();
            
            if (stdout != null && !string.IsNullOrWhiteSpace(stdout.InnerText))
            {
                resultCreateModel.AttachmentData.ConsoleLog = stdout.InnerText;
            }

            // Standard error logs
            stderr = testCaseNode.SelectSingleNode("./system-err");
            if (stderr != null && !string.IsNullOrWhiteSpace(stderr.InnerText))
            {
                resultCreateModel.AttachmentData.StandardError = stderr.InnerText;
            }
        }

        class TestSuiteSummary
        {
            public string Name { get; set; }

            public string Host { get; set; }

            public DateTime TimeStamp { get; set; }

            public TimeSpan TestSuiteDuration { get; set; }

            public List<TestCaseResultData> Results { get; set; }

            public TimeSpan TotalTestCaseDuration { get; set; }
            
            public bool SuiteTimeDataAvailable { get; set; }
            
            public bool SuiteTimeStampAvailable { get; set; }

            public TestSuiteSummary(string name)
            {
                Name = name;
                Host = string.Empty;
                TimeStamp = DateTime.UtcNow;
                TestSuiteDuration = TimeSpan.Zero;
                SuiteTimeDataAvailable = true;
                SuiteTimeStampAvailable = true;
                Results = new List<TestCaseResultData>();
                TotalTestCaseDuration = TimeSpan.Zero;
            }
        }
    }
}