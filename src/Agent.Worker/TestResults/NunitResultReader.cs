using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    internal class NUnitResultsXmlReader
    {
        public NUnitResultsXmlReader() 
        {
        }

        public TestRunData GetTestRunData(string filePath, XmlDocument doc, XmlNode testResultsNode, TestRunContext runContext, bool addResultsAsAttachments)
        {
            var results = new List<TestCaseResultData>();

            //read test run summary information - run name, start time
            string runName = "NUnit Test Run";
            DateTime runStartTime = DateTime.MinValue; //Use local time instead of UTC as TestRunData uses local time for defaults. Also assuming timestamp is local is more accurate in cases where tests were run on build machine
            TimeSpan totalRunDuration = TimeSpan.Zero;
            TimeSpan totalTestCaseDuration = TimeSpan.Zero;

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
                    DateTime.TryParse(testResultsNode.Attributes["date"].Value, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out dateFromXml);
                }

                TimeSpan timeFromXml = TimeSpan.Zero;
                if (testResultsNode.Attributes["time"] != null)
                {
                    TimeSpan.TryParse(testResultsNode.Attributes["time"].Value, CultureInfo.InvariantCulture, out timeFromXml);
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

            XmlNode envNode = doc.SelectSingleNode(RootNodeName + "/environment");
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
            testRunData.Attachments = addResultsAsAttachments ? new string[] { filePath } : new string[0];

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
            XmlNodeList testCaseNodes = startNode.SelectNodes(TestCaseNodeName); //all test-case nodes under testAssemblyNode
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
                        double.TryParse(testCaseNode.Attributes["time"].Value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out duration);
                        // Duration of a test case cannot be less than zero
                        testCaseDuration = ( duration < 0 ) ? testCaseDuration : TimeSpan.FromSeconds(duration);
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

                        // console log
                        XmlNode consoleLog = testCaseNode.SelectSingleNode("./output");
                        if (consoleLog != null && !string.IsNullOrWhiteSpace(consoleLog.InnerText))
                        {
                            resultCreateModel.ConsoleLog = consoleLog.InnerText;
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

                    resultCreateModel.AutomatedTestType = "NUnit";

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

            XmlNodeList testSuiteNodes = startNode.SelectNodes(InnerTestSuiteNodeName);
            if (testSuiteNodes != null)
            {
                foreach (XmlNode testSuiteNode in testSuiteNodes)
                {
                    results.AddRange(FindTestCaseNodes(testSuiteNode, hostName, runUserIdRef, assemblyStartTime, testStorage));
                }
            }

            return results;
        }

        public string InnerTestSuiteNodeName { get; set; }
        public string TestCaseNodeName { get; set; }
        public string RootNodeName { get; set; }

    }


    public interface INUnitResultsXmlReader
    {
        TestRunData GetTestRunData(string filePath, XmlDocument doc, XmlNode testResultsNode, TestRunContext runContext, bool addResultsAsAttachments);
    }

    internal class NUnit2ResultsXmlReader: NUnitResultsXmlReader, INUnitResultsXmlReader
    {
        public new TestRunData GetTestRunData(string filePath, XmlDocument doc, XmlNode testResultsNode, TestRunContext runContext, bool addResultsAsAttachments)
        {
            this.InnerTestSuiteNodeName = "results/test-suite";
            this.TestCaseNodeName = "results/test-case";
            this.RootNodeName = "test-results";

            return base.GetTestRunData(filePath, doc, testResultsNode, runContext, addResultsAsAttachments);
        }
    }

    internal class NUnit3ResultsXmlReader: INUnitResultsXmlReader
    {
        private IdentityRef _runUserIdRef;
        private string _platform;
        private const string _defaultRunName = "NUnit Test Run";
        private TestCaseResultData getTestCaseResultData(XmlNode testCaseResultNode, string assemblyName, string hostname)
        {
            var testCaseResultData = new TestCaseResultData();

            if (!string.IsNullOrEmpty(assemblyName))
            {
                testCaseResultData.AutomatedTestStorage = assemblyName;
            }
            testCaseResultData.ComputerName = hostname;
            testCaseResultData.TestCaseTitle = testCaseResultNode.Attributes["name"]?.Value;
            testCaseResultData.AutomatedTestName = testCaseResultNode.Attributes["fullname"]?.Value;
            double duration = 0;
            double.TryParse(testCaseResultNode.Attributes["duration"]?.Value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out duration);
            // Ensure Duration cannot be negative
            duration = (duration <0) ? 0 : duration;
            testCaseResultData.DurationInMs = TimeSpan.FromSeconds(duration).TotalMilliseconds;
            var testExecutionStartedOn = DateTime.MinValue;
            DateTime.TryParse(testCaseResultNode.Attributes["start-time"]?.Value, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out testExecutionStartedOn);
            testCaseResultData.StartedDate = testExecutionStartedOn;
            var testExecutionEndedOn = DateTime.MinValue;
            DateTime.TryParse(testCaseResultNode.Attributes["end-time"]?.Value, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out testExecutionEndedOn);
            testCaseResultData.CompletedDate = testExecutionEndedOn; 
            if (testCaseResultNode.Attributes["result"] != null)
            {
                if (string.Equals(testCaseResultNode.Attributes["result"].Value, "Passed", StringComparison.OrdinalIgnoreCase))
                {
                    testCaseResultData.Outcome = TestOutcome.Passed.ToString();
                }
                else if (string.Equals(testCaseResultNode.Attributes["result"].Value, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    testCaseResultData.Outcome = TestOutcome.Failed.ToString();
                }
                else if (string.Equals(testCaseResultNode.Attributes["result"].Value, "Skipped", StringComparison.OrdinalIgnoreCase))
                {
                    testCaseResultData.Outcome = TestOutcome.NotExecuted.ToString();
                } 
                else
                {
                    testCaseResultData.Outcome = TestOutcome.Inconclusive.ToString();
                }
                var failureNode = testCaseResultNode.SelectSingleNode("failure");
                if (failureNode != null)
                {
                    var failureMessageNode = failureNode.SelectSingleNode("message");
                    var failureStackTraceNode = failureNode.SelectSingleNode("stack-trace");
                    testCaseResultData.ErrorMessage = failureMessageNode?.InnerText;
                    testCaseResultData.StackTrace = failureStackTraceNode?.InnerText;

                    // console log
                    XmlNode consoleLog = testCaseResultNode.SelectSingleNode("output");
                    if (consoleLog != null && !string.IsNullOrWhiteSpace(consoleLog.InnerText))
                    {
                        testCaseResultData.ConsoleLog = consoleLog.InnerText;
                    }

                }
            }
            testCaseResultData.State = "Completed";
            testCaseResultData.AutomatedTestType = "NUnit";
            if (_runUserIdRef != null)
            {
                testCaseResultData.RunBy = _runUserIdRef;
                testCaseResultData.Owner = _runUserIdRef;
            }
            return testCaseResultData;
        }
        public TestRunData GetTestRunData(string filePath, XmlDocument doc, XmlNode testResultsNode, TestRunContext runContext, bool addResultsAsAttachments)
        {
            var testRunNode = doc.SelectSingleNode("test-run");
            var testRunStartedOn = DateTime.MinValue;
            var testRunEndedOn = DateTime.MinValue;
            var testCaseResults = new List<TestCaseResultData>();
            _runUserIdRef = runContext != null ? new IdentityRef() { DisplayName = runContext.Owner } : null;
            _platform = runContext != null ? runContext.Platform : string.Empty;
            if (testRunNode.Attributes["start-time"] != null)
            {
                DateTime.TryParse(testRunNode.Attributes["start-time"]?.Value, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out testRunStartedOn);
                DateTime.TryParse(testRunNode.Attributes["end-time"]?.Value, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out testRunEndedOn);
                var testAssemblyNodes = testRunNode.SelectNodes("//test-suite[@type='Assembly']");
                if (testAssemblyNodes != null)
                {
                    foreach (XmlNode testAssemblyNode in testAssemblyNodes)
                    {
                        var environmentNode = testAssemblyNode.SelectSingleNode("environment");
                        var hostname = String.Empty;
                        var assemblyName = (testAssemblyNode.Attributes["name"] != null ? testAssemblyNode.Attributes["name"].Value : null);  
                        if (environmentNode != null)
                        {
                            if (environmentNode.Attributes["machine-name"] != null)
                            {
                                hostname = environmentNode.Attributes["machine-name"].Value;
                            }
                            if (environmentNode.Attributes["platform"] != null)
                            {
                                // override platform
                                _platform = environmentNode.Attributes["platform"].Value;
                            }
                        }
                        var testCaseNodes = testAssemblyNode.SelectNodes(".//test-case");
                        if (testCaseNodes != null)
                        {
                            foreach (XmlNode testCaseNode in testCaseNodes)
                            {
                                testCaseResults.Add(getTestCaseResultData(testCaseNode, assemblyName, hostname));
                            }
                        }
                    }
                }
            }
            TestRunData testRunData = new TestRunData(
                name: runContext != null && !string.IsNullOrWhiteSpace(runContext.RunName) ? runContext.RunName : _defaultRunName,
                startedDate: (testRunStartedOn == DateTime.MinValue ? string.Empty : testRunStartedOn.ToString("o")),
                completedDate: (testRunEndedOn == DateTime.MinValue ? string.Empty : testRunEndedOn.ToString("o")),
                state: TestRunState.InProgress.ToString(),
                isAutomated: true,
                buildId: runContext != null ? runContext.BuildId : 0,
                buildFlavor: runContext != null ? runContext.Configuration : string.Empty,
                buildPlatform: _platform,
                releaseUri: runContext != null ? runContext.ReleaseUri : null,
                releaseEnvironmentUri: runContext != null ? runContext.ReleaseEnvironmentUri : null
            );
            testRunData.Results = testCaseResults.ToArray();
            testRunData.Attachments = addResultsAsAttachments ? new string[] { filePath } : new string[0];
            return testRunData;
        }
    }

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
            var doc = new XmlDocument();
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

            INUnitResultsXmlReader nunitResultsReader = null;
            var testResultsNode = doc.SelectSingleNode("test-results");
            if (testResultsNode == null)
            {
                testResultsNode = doc.SelectSingleNode("test-run");
                if (testResultsNode == null)
                {
                    throw new NotSupportedException(StringUtil.Loc("InvalidFileFormat"));
                }
                nunitResultsReader = new NUnit3ResultsXmlReader();
            }
            else
            {
                nunitResultsReader = new NUnit2ResultsXmlReader();
            }

            return nunitResultsReader.GetTestRunData(filePath, doc, testResultsNode, runContext, AddResultsFileToRunLevelAttachments);
            
        }


        public bool AddResultsFileToRunLevelAttachments { get; set; }
    }
}