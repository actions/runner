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
                    DtdProcessing = DtdProcessing.Ignore,
                    XmlResolver = null
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
                        TestSuiteSummary testSuiteSummary = ReadTestSuite(testSuiteNode, runUserIdRef);
                        runSummary.Duration = runSummary.Duration.Add(testSuiteSummary.Duration);
                        runSummary.Results.AddRange(testSuiteSummary.Results);
                        runSummary.Host = testSuiteSummary.Host;
                        runSummary.Name = testSuiteSummary.Name;
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
                }
            }

            if (runContext != null && !string.IsNullOrWhiteSpace(runContext.RunName))
            {
                runSummary.Name = runContext.RunName;
            }

            if (runSummary.Results.Count > 0)
            {
                //first testsuite starteddate is the starteddate of the run
                runSummary.TimeStamp = DateTime.Parse(runSummary.Results[0].StartedDate);
            }

            //create test run data
            var testRunData = new TestRunData(
                name: runSummary.Name,
                startedDate: runSummary.TimeStamp.ToString("o"),
                completedDate: runSummary.TimeStamp.Add(runSummary.Duration).ToString("o"),
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
            TimeSpan totalTestSuiteDuration = TimeSpan.Zero;
            TimeSpan totalTestCaseDuration = TimeSpan.Zero;

            if (rootNode.Attributes["name"] != null && rootNode.Attributes["name"].Value != null)
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
                if (DateTime.TryParse(timestampNode.Value, out timestampFromXml))
                {
                    testSuiteSummary.TimeStamp = timestampFromXml;
                }
            }

            totalTestSuiteDuration = GetTimeSpan(rootNode);

            DateTime testSuiteStartTime = testSuiteSummary.TimeStamp;

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

                    //test case duration
                    TimeSpan testCaseDuration = GetTimeSpan(testCaseNode);
                    resultCreateModel.DurationInMs = testCaseDuration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);

                    resultCreateModel.StartedDate = testCaseStartTime.ToString("o");
                    resultCreateModel.CompletedDate = testCaseStartTime.AddTicks(testCaseDuration.Ticks).ToString("o");
                    testCaseStartTime = testCaseStartTime.AddTicks(1) + testCaseDuration; //next start time

                    //test case outcome
                    XmlNode failure, error, skipped;

                    if ((failure = testCaseNode.SelectSingleNode("./failure")) != null)
                    {
                        ProcessFailureNode(failure, resultCreateModel);
                    }
                    else if ((error = testCaseNode.SelectSingleNode("./error")) != null)
                    {
                        ProcessFailureNode(error, resultCreateModel);
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
                        resultCreateModel.Owner = runUserIdRef;
                    }

                    if (!string.IsNullOrEmpty(resultCreateModel.AutomatedTestName) && !string.IsNullOrEmpty(resultCreateModel.TestCaseTitle))
                    {
                        testSuiteSummary.Results.Add(resultCreateModel);
                    }

                }
            }

            if (TimeSpan.Compare(totalTestSuiteDuration, totalTestCaseDuration) < 0)
            {
                totalTestSuiteDuration = totalTestCaseDuration; //run duration may not be set in the xml, so use total test case duration 
            }
            testSuiteSummary.Duration = totalTestSuiteDuration;

            return testSuiteSummary;
        }

        private TimeSpan GetTimeSpan(XmlNode rootNode)
        {
            var time = TimeSpan.Zero;
            if (rootNode.Attributes["time"] != null)
            {
                var timeValue = rootNode.Attributes["time"].Value;
                if (timeValue != null)
                {
                    double timeInSeconds = 0.0;
                    if (double.TryParse(timeValue, out timeInSeconds))
                    {
                        time = TimeSpan.FromSeconds(timeInSeconds);
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

        class TestSuiteSummary
        {
            public string Name { get; set; }

            public string Host { get; set; }

            public DateTime TimeStamp { get; set; }

            public TimeSpan Duration { get; set; }

            public List<TestCaseResultData> Results { get; set; }

            public TestSuiteSummary(string name)
            {
                Name = name;
                Host = string.Empty;
                TimeStamp = DateTime.Now;
                Duration = TimeSpan.Zero;
                Results = new List<TestCaseResultData>();
            }
        }
    }
}