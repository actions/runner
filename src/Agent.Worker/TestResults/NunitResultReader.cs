using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public class NUnitResultReader : AgentService, IResultReader
    {
        public Type ExtensionType => typeof(IResultReader);
        public string Name => "NUnit";

        public NUnitResultReader()
        {
            AddResultsFileToRunLevelAttachments = true;
        }

        public TestRunData ReadResults(IExecutionContext executionContext, string filePath, TestRunContext runContext = null)
        {
            List<TestCaseResultData> results = new List<TestCaseResultData>();

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

            //read test run summary information - run name, start time
            string runName = Name + " Test Run";
            DateTime runStartTime = DateTime.MinValue; //Use local time instead of UTC as TestRunData uses local time for defaults. Also assuming timestamp is local is more accurate in cases where tests were run on build machine
            TimeSpan totalRunDuration = TimeSpan.Zero;
            TimeSpan totalTestCaseDuration = TimeSpan.Zero;

            XmlNode testResultsNode = doc.SelectSingleNode("test-results");
            if (testResultsNode != null)
            {
                //get test run summary information
                if (testResultsNode.Attributes["name"] != null)
                {
                    runName = testResultsNode.Attributes["name"].Value;
                }

                //run times
                DateTime dateFromXml = DateTime.MinValue.Date; //Use local time instead of UTC as TestRunData uses local time for defaults.
                if (testResultsNode.Attributes["date"] != null)
                {
                    DateTime.TryParse(testResultsNode.Attributes["date"].Value, out dateFromXml);
                }

                TimeSpan timeFromXml = TimeSpan.Zero;
                if (testResultsNode.Attributes["time"] != null)
                {
                    TimeSpan.TryParse(testResultsNode.Attributes["time"].Value, out timeFromXml);
                }

                //assume runtimes from xml are current local time since timezone information is not in the xml, if xml datetime > current local time, fallback to local start time
                DateTime runStartDateTimeFromXml = new DateTime(dateFromXml.Ticks).AddTicks(timeFromXml.Ticks);
                if (runStartTime == DateTime.MinValue)
                {
                    runStartTime = runStartDateTimeFromXml;
                }
            }

            //run environment - platform, config and hostname
            string platform = runContext != null ? runContext.Platform : string.Empty;
            string config = runContext != null ? runContext.Configuration : string.Empty;
            string runUser = runContext != null ? runContext.Owner : string.Empty;
            string hostName = string.Empty;

            XmlNode envNode = doc.SelectSingleNode("test-results/environment");
            if (envNode != null)
            {
                if (envNode.Attributes["machine-name"] != null && envNode.Attributes["machine-name"].Value != null)
                {
                    hostName = envNode.Attributes["machine-name"].Value;
                }

                if (envNode.Attributes["platform"] != null && envNode.Attributes["platform"].Value != null && runContext != null && runContext.BuildId > 0)
                {
                    //We cannot publish platform information without a valid build id.
                    platform = envNode.Attributes["platform"].Value;
                }
            }

            //run owner
            IdentityRef runUserIdRef = null;
            if (!string.IsNullOrEmpty(runUser))
            {
                runUserIdRef = new IdentityRef() { DisplayName = runUser };
            }

            //get all test assemblies
            if (testResultsNode != null)
            {
                XmlNodeList testAssemblyNodes = testResultsNode.SelectNodes("test-suite");
                if (testAssemblyNodes != null)
                {
                    foreach (XmlNode testAssemblyNode in testAssemblyNodes)
                    {
                        var assemblyStartTime = (runStartTime == DateTime.MinValue) ? DateTime.MinValue : runStartTime + totalTestCaseDuration;
                        List<TestCaseResultData> testCases = FindTestCaseNodes(testAssemblyNode, hostName, runUserIdRef, assemblyStartTime);
                        if (testCases != null)
                        {
                            results.AddRange(testCases);
                            testCases.ForEach(x => totalTestCaseDuration += TimeSpan.FromMilliseconds(x.DurationInMs));
                        }
                    }
                }
            }

            if (TimeSpan.Compare(totalRunDuration, totalTestCaseDuration) < 0)
            {
                totalRunDuration = totalTestCaseDuration; //run duration may not be set in the xml, so use total test case duration 
            }

            if (runContext != null && !string.IsNullOrWhiteSpace(runContext.RunName))
            {
                runName = runContext.RunName;
            }

            //create test run data
            TestRunData testRunData = new TestRunData(
                name: runName,
                startedDate: (runStartTime == DateTime.MinValue) ? string.Empty : runStartTime.ToString("o"),
                completedDate: (runStartTime == DateTime.MinValue) ? string.Empty : runStartTime.Add(totalRunDuration).ToString("o"),
                state: TestRunState.InProgress.ToString(),
                isAutomated: true,
                buildId: runContext != null ? runContext.BuildId : 0,
                buildFlavor: config,
                buildPlatform: platform,
                releaseUri: runContext != null ? runContext.ReleaseUri : null,
                releaseEnvironmentUri: runContext != null ? runContext.ReleaseEnvironmentUri : null
                );

            testRunData.Results = results.ToArray();
            testRunData.Attachments = AddResultsFileToRunLevelAttachments ? new string[] { filePath } : new string[0];

            return testRunData;
        }

        private List<TestCaseResultData> FindTestCaseNodes(XmlNode startNode, string hostName, IdentityRef runUserIdRef, DateTime assemblyStartTime, string assemblyName = null)
        {
            List<TestCaseResultData> results = new List<TestCaseResultData>();

            string testStorage = assemblyName;
            if (startNode.Attributes["type"] != null && startNode.Attributes["type"].Value != null && startNode.Attributes["type"].Value.Equals("assembly", StringComparison.OrdinalIgnoreCase))
            {
                if (startNode.Attributes["name"] != null && startNode.Attributes["name"].Value != null)
                {
                    testStorage = startNode.Attributes["name"].Value;
                }
            }

            //get each test case result information
            XmlNodeList testCaseNodes = startNode.SelectNodes("results/test-case"); //all test-case nodes under testAssemblyNode
            if (testCaseNodes != null)
            {
                DateTime testCaseStartTime = assemblyStartTime;
                foreach (XmlNode testCaseNode in testCaseNodes)
                {
                    TestCaseResultData resultCreateModel = new TestCaseResultData();

                    //test case name and type
                    if (testCaseNode.Attributes["name"] != null && testCaseNode.Attributes["name"].Value != null)
                    {
                        resultCreateModel.TestCaseTitle = testCaseNode.Attributes["name"].Value;
                        resultCreateModel.AutomatedTestName = testCaseNode.Attributes["name"].Value;
                    }

                    if (!string.IsNullOrEmpty(testStorage))
                    {
                        resultCreateModel.AutomatedTestStorage = testStorage;
                    }

                    //test case duration, starttime and endtime
                    TimeSpan testCaseDuration = TimeSpan.Zero;
                    if (testCaseNode.Attributes["time"] != null && testCaseNode.Attributes["time"].Value != null)
                    {
                        double duration = 0;
                        double.TryParse(testCaseNode.Attributes["time"].Value, out duration);
                        testCaseDuration = TimeSpan.FromSeconds(duration);
                    }
                    resultCreateModel.DurationInMs = testCaseDuration.TotalMilliseconds;

                    if (assemblyStartTime != DateTime.MinValue)
                    {
                        resultCreateModel.StartedDate = testCaseStartTime;
                        resultCreateModel.CompletedDate = testCaseStartTime.AddTicks(testCaseDuration.Ticks);
                        testCaseStartTime = testCaseStartTime.AddTicks(1) + testCaseDuration; //next start time
                    }

                    //test run outcome
                    if (testCaseNode.SelectSingleNode("./failure") != null)
                    {
                        resultCreateModel.Outcome = "Failed";

                        XmlNode failureMessageNode, failureStackTraceNode;

                        if ((failureMessageNode = testCaseNode.SelectSingleNode("./failure/message")) != null && !string.IsNullOrWhiteSpace(failureMessageNode.InnerText))
                        {
                            resultCreateModel.ErrorMessage = failureMessageNode.InnerText;
                        }
                        // stack trace
                        if ((failureStackTraceNode = testCaseNode.SelectSingleNode("./failure/stack-trace")) != null && !string.IsNullOrWhiteSpace(failureStackTraceNode.InnerText))
                        {
                            resultCreateModel.StackTrace = failureStackTraceNode.InnerText;
                        }
                    }
                    else
                    {
                        if (testCaseNode.Attributes["result"] != null && string.Equals(testCaseNode.Attributes["result"].Value, "Ignored", StringComparison.OrdinalIgnoreCase))
                        {
                            resultCreateModel.Outcome = "NotExecuted";
                        }
                        else
                        {
                            resultCreateModel.Outcome = "Passed";
                        }
                    }

                    resultCreateModel.State = "Completed";

                    resultCreateModel.AutomatedTestType = Name;

                    //other properties
                    if (runUserIdRef != null)
                    {
                        resultCreateModel.RunBy = runUserIdRef;
                        resultCreateModel.Owner = runUserIdRef;
                    }

                    resultCreateModel.ComputerName = hostName;

                    if (!string.IsNullOrEmpty(resultCreateModel.AutomatedTestName) && !string.IsNullOrEmpty(resultCreateModel.TestCaseTitle))
                    {
                        results.Add(resultCreateModel);
                    }

                }
            }

            XmlNodeList testSuiteNodes = startNode.SelectNodes("results/test-suite");
            if (testSuiteNodes != null)
            {
                foreach (XmlNode testSuiteNode in testSuiteNodes)
                {
                    results.AddRange(FindTestCaseNodes(testSuiteNode, hostName, runUserIdRef, assemblyStartTime, testStorage));
                }
            }

            return results;
        }

        public bool AddResultsFileToRunLevelAttachments
        {
            get;
            set;
        }
    }
}