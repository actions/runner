using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public class CTestResultReader : AgentService, IResultReader
    {
        public Type ExtensionType => typeof(IResultReader);
        public string Name => "CTest";

        public CTestResultReader()
        {
            AddResultsFileToRunLevelAttachments = true;
        }

        /// <summary>
        /// Reads a CTest results file from disk, converts it into a TestRunData object.        
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="runContext"></param>
        /// <returns></returns>
        public TestRunData ReadResults(IExecutionContext executionContext, string filePath, TestRunContext runContext = null)
        {
            _ec = executionContext;

            // Read Xml File
            XmlDocument doc = new XmlDocument();
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings
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

            // init test run summary information - run name, host name, start time
            List<TestCaseResultData> results = new List<TestCaseResultData>();
            string runName = ParseRunName(runContext);
            InitialiseRunUserIdRef(runContext);

            //run environment - platform, config and hostname
            string platform = runContext != null ? runContext.Platform : string.Empty;
            string config = runContext != null ? runContext.Configuration : string.Empty;
            string runUser = runContext != null ? runContext.Owner : string.Empty;
            string hostName = string.Empty;

            //Parse the run start and finish times.
            XmlNode node = doc.SelectSingleNode("/Site/Testing");
            _runStartDate = DateTime.MinValue;
            _runFinishDate = DateTime.MinValue;
            ParseRunStartAndFinishDates(node);

            //create test run data object
            TestRunData testRunData = new TestRunData(
                name: runName,
                startedDate: (_runStartDate == DateTime.MinValue) ? string.Empty : _runStartDate.ToString("o"),
                completedDate: (_runFinishDate == DateTime.MinValue) ? string.Empty : _runFinishDate.ToString("o"),
                state: TestRunState.InProgress.ToString(),
                isAutomated: true,
                buildId: runContext != null ? runContext.BuildId : 0,
                buildFlavor: config,
                buildPlatform: platform,
                releaseUri: runContext != null ? runContext.ReleaseUri : null,
                releaseEnvironmentUri: runContext != null ? runContext.ReleaseEnvironmentUri : null
                )
            {
                Attachments = AddResultsFileToRunLevelAttachments ? new string[] { filePath } : new string[0]
            };

            // Read results
            XmlNodeList resultsNodes = doc.SelectNodes("/Site/Testing/Test");
            results.AddRange(ReadActualResults(resultsNodes));
            testRunData.Results = results.ToArray();

            return testRunData;
        }

        #region Private Methods

        /// <summary>
        /// Creates a test run data object.        
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void ParseRunStartAndFinishDates(XmlNode node)
        {
            int startTime, endTime;
            if (int.TryParse(node.SelectSingleNode("./StartTestTime")?.InnerText, out startTime) && int.TryParse(node.SelectSingleNode("./EndTestTime")?.InnerText, out endTime)
                && startTime>0 && endTime>0)
            {
                _ec.Debug("Setting run start and finish times.");

                _runStartDate = (new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(startTime);
                _runFinishDate = (new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(endTime);

                if (_runFinishDate < _runStartDate)
                {
                    _runFinishDate = _runStartDate = DateTime.MinValue;
                    _ec.Warning("Run finish date is less than start date.Resetting to min value.");
                }
            }
        }


        /// <summary>
        /// Reads all test result nodes     
        /// </summary>
        /// <param name="resultsNodes"></param>
        /// <returns>List of test case results</returns>
        private List<TestCaseResultData> ReadActualResults(XmlNodeList resultsNodes)
        {
            List<TestCaseResultData> results = new List<TestCaseResultData>();
            var resultXmlNodes = resultsNodes.Cast<XmlNode>();

            foreach (var testCaseNode in resultXmlNodes)
            {
                TestCaseResultData resultCreateModel = new TestCaseResultData();

                // find test case title and other information
                resultCreateModel.TestCaseTitle = testCaseNode.SelectSingleNode("./Name")?.InnerText;
                resultCreateModel.AutomatedTestName = testCaseNode.SelectSingleNode("./FullName")?.InnerText;
                resultCreateModel.AutomatedTestStorage = testCaseNode.SelectSingleNode("./Path")?.InnerText;
                resultCreateModel.AutomatedTestType = ParserName;

                // find duration of test case, starttime and endtime
                resultCreateModel.DurationInMs = (long)GetTestCaseResultDuration(testCaseNode).TotalMilliseconds;

                // start time of test case is kept as run start time sice test case start is not available
                resultCreateModel.StartedDate = _runStartDate;
                resultCreateModel.CompletedDate = resultCreateModel.StartedDate.AddMilliseconds(resultCreateModel.DurationInMs);

                // find test case outcome
                resultCreateModel.Outcome = GetTestCaseOutcome(testCaseNode).ToString();

                // If test outcome is failed, fill stacktrace and error message
                if (resultCreateModel.Outcome.ToString().Equals(TestOutcome.Failed.ToString()))
                {
                    XmlNode failure;
                    // Stacktrace
                    if ((failure = testCaseNode.SelectSingleNode("./Results/Measurement/Value")) != null)
                    {
                        if (!string.IsNullOrEmpty(failure.InnerText))
                            resultCreateModel.StackTrace = failure.InnerText;
                    }
                }
                else
                {
                    // fill console logs
                    resultCreateModel.AttachmentData = new AttachmentData();
                    XmlNode stdOutputLog = testCaseNode.SelectSingleNode("./Results/Measurement/Value");
                    if (!string.IsNullOrEmpty(stdOutputLog?.InnerText))
                    {
                        resultCreateModel.AttachmentData.ConsoleLog = stdOutputLog.InnerText;
                    }
                }

                resultCreateModel.State = "Completed";

                //other properties
                if (_runUserIdRef != null)
                {
                    resultCreateModel.RunBy = _runUserIdRef;
                    resultCreateModel.Owner = _runUserIdRef;
                }

                //Mandatory fields. Skip if they are not available.
                if (!string.IsNullOrEmpty(resultCreateModel.AutomatedTestName)
                && !string.IsNullOrEmpty(resultCreateModel.TestCaseTitle))
                {
                    results.Add(resultCreateModel);
                }
            }

            return results;
        }

        /// <summary>
        /// Gets test case duration     
        /// </summary>
        /// <param name="resultsNode"></param>
        /// <returns>Test case duration</returns>
        private TimeSpan GetTestCaseResultDuration(XmlNode testCaseNode)
        {
            TimeSpan testCaseDuration = TimeSpan.Zero;
            XmlNode executionTime;
            if ((executionTime = testCaseNode.SelectSingleNode("./Results/NamedMeasurement[@name='Execution Time']/Value")) != null)
            {
                double.TryParse(executionTime.InnerText, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out double duration);
                // Duration of a test case cannot be less than zero
                testCaseDuration = (duration < 0) ? testCaseDuration : TimeSpan.FromSeconds(duration);
            }

            return testCaseDuration;
        }

        /// <summary>
        /// Gets test case outcome     
        /// </summary>
        /// <param name="resultsNode"></param>
        /// <returns>Test case outcome</returns>
        private TestOutcome GetTestCaseOutcome(XmlNode testCaseNode)
        {
            if (testCaseNode.Attributes == null)
            {
                return TestOutcome.NotExecuted;
            }

            string outcome = testCaseNode.Attributes["Status"].Value;
            if (string.Equals(outcome, "passed", StringComparison.OrdinalIgnoreCase))
            {
                return TestOutcome.Passed;
            }
            else if (string.Equals(outcome, "failed", StringComparison.OrdinalIgnoreCase))
            {
                return TestOutcome.Failed;
            }
            else if (string.Equals(outcome, "notrun", StringComparison.OrdinalIgnoreCase))
            {
                return TestOutcome.NotExecuted;
            }

            return TestOutcome.NotExecuted;
        }

        private string ParseRunName(TestRunContext runContext)
        {
            string runName = ParserName + " Test Run";
            if (runContext != null)
            {
                runName = !String.IsNullOrWhiteSpace(runContext.RunName)
                    ? runContext.RunName
                    : $"{runName} {runContext.Configuration} {runContext.Platform}";
            }
            return runName;
        }

        private void InitialiseRunUserIdRef(TestRunContext runContext)
        {
            string runUser = "";
            if (runContext?.Owner != null)
            {
                runUser = runContext.Owner;
            }

            _runUserIdRef = new IdentityRef()
            {
                DisplayName = runUser
            };
        }
        #endregion

        #region Private Members 
        private IdentityRef _runUserIdRef;
        DateTime _runStartDate;
        DateTime _runFinishDate;
        IExecutionContext _ec;
        #endregion

        #region Constants
        public const string ParserName = "CTest";
        #endregion

        public bool AddResultsFileToRunLevelAttachments
        {
            get;
            set;
        }
    }
}