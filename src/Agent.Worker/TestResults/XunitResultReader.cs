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
    public class XUnitResultReader : AgentService, IResultReader
    {
        public Type ExtensionType => typeof(IResultReader);
        public string Name => "XUnit";

        public XUnitResultReader()
        {
            AddResultsFileToRunLevelAttachments = true;
        }
        //Based on the XUnit V2 format: http://xunit.github.io/docs/format-xml-v2.html
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

            string runName = Name + " Test Run";
            string runUser = "";
            if (runContext != null)
            {
                if (!string.IsNullOrWhiteSpace(runContext.RunName))
                {
                    runName = runContext.RunName;
                }
                else
                {
                    runName = string.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", runName, runContext.Configuration, runContext.Platform);
                }

                if (runContext.Owner != null)
                {
                    runUser = runContext.Owner;
                }
            }

            IdentityRef runUserIdRef = new IdentityRef();
            runUserIdRef.DisplayName = runUser;

            var minStartTime = DateTime.MaxValue;
            var maxCompletedTime = DateTime.MinValue;
            bool dateTimeParseError = true;
            bool assemblyRunDateTimeAttributesNotPresent = false;
            bool assemblyTimeAttributeNotPresent = false;
            double assemblyRunDuration = 0;
            double testRunDuration = 0;

            XmlNodeList assemblyNodes = doc.SelectNodes("/assemblies/assembly");
            foreach (XmlNode assemblyNode in assemblyNodes)
            {
                var assemblyRunStartTimeStamp = DateTime.MinValue;
                if (assemblyNode.Attributes["run-date"] != null && assemblyNode.Attributes["run-time"] != null)
                {
                    string runDate = assemblyNode.Attributes["run-date"].Value;
                    string runTime = assemblyNode.Attributes["run-time"].Value;

                    var startDate = DateTime.Now;
                    var startTime = TimeSpan.Zero;
                    if (DateTime.TryParse(runDate, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out startDate) && 
                        TimeSpan.TryParse(runTime, CultureInfo.InvariantCulture, out startTime))
                    {
                        dateTimeParseError = false;
                    }
                    assemblyRunStartTimeStamp = startDate + startTime;
                    if (minStartTime > assemblyRunStartTimeStamp)
                    {
                        minStartTime = assemblyRunStartTimeStamp;
                    }
                }
                else 
                {
                    assemblyRunDateTimeAttributesNotPresent = true;
                }
                if (!assemblyTimeAttributeNotPresent && assemblyNode.Attributes["time"] != null)
                {
                    double assemblyDuration = 0;
                    Double.TryParse(assemblyNode.Attributes["time"].Value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out assemblyDuration);
                    assemblyRunDuration = assemblyRunDuration + assemblyDuration;
                    var durationFromSeconds = TimeSpan.FromSeconds(assemblyDuration);

                    // no assemblystarttime available so dont calculate assemblycompletedtime
                    if (assemblyRunStartTimeStamp != DateTime.MinValue)
                    {
                        DateTime assemblyRunCompleteTimeStamp =
                           assemblyRunStartTimeStamp.AddTicks(durationFromSeconds.Ticks);

                        //finding maximum comleted time
                        if (maxCompletedTime < assemblyRunCompleteTimeStamp)
                        {
                            maxCompletedTime = assemblyRunCompleteTimeStamp;
                        }

                    }
                }
                else
                {
                    assemblyTimeAttributeNotPresent = true;
                }
                XmlNodeList testCaseNodeList = assemblyNode.SelectNodes("./collection/test");
                foreach (XmlNode testCaseNode in testCaseNodeList)
                {
                    TestCaseResultData resultCreateModel = new TestCaseResultData()
                    {
                        Priority = TestManagementConstants.UnspecifiedPriority,  //Priority is int type so if no priority set then its 255.
                    };

                    //Test storage.
                    if (assemblyNode.Attributes["name"] != null)
                    {
                        resultCreateModel.AutomatedTestStorage = Path.GetFileName(assemblyNode.Attributes["name"].Value);
                    }

                    //Fully Qualified Name.
                    if (testCaseNode.Attributes["name"] != null)
                    {
                        resultCreateModel.AutomatedTestName = testCaseNode.Attributes["name"].Value;
                    }

                    //Test Method Name.
                    if (testCaseNode.Attributes["method"] != null)
                    {
                        resultCreateModel.TestCaseTitle = testCaseNode.Attributes["method"].Value;
                    }

                    //Test duration.
                    if (testCaseNode.Attributes["time"] != null && testCaseNode.Attributes["time"].Value != null)
                    {
                        double duration = 0;
                        double.TryParse(testCaseNode.Attributes["time"].Value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out duration);
                        var durationFromSeconds = TimeSpan.FromSeconds(duration);
                        resultCreateModel.DurationInMs = durationFromSeconds.TotalMilliseconds;

                        // no assemblystarttime available so dont set testcase start and completed
                        if (assemblyRunStartTimeStamp != DateTime.MinValue)
                        {
                            resultCreateModel.StartedDate = assemblyRunStartTimeStamp;
                            resultCreateModel.CompletedDate = assemblyRunStartTimeStamp.AddTicks(durationFromSeconds.Ticks);
                            assemblyRunStartTimeStamp = assemblyRunStartTimeStamp.AddTicks(1) + durationFromSeconds;
                            //next start time
                        }

                        //Calculate overall run duration.
                        testRunDuration += duration;
                    }


                    //Test outcome.
                    if (testCaseNode.SelectSingleNode("./failure") != null)
                    {
                        resultCreateModel.Outcome = TestOutcome.Failed.ToString();

                        //Error message.
                        XmlNode failureMessageNode = testCaseNode.SelectSingleNode("./failure/message");
                        if (failureMessageNode != null && !string.IsNullOrWhiteSpace(failureMessageNode.InnerText))
                        {
                            resultCreateModel.ErrorMessage = failureMessageNode.InnerText;
                        }

                        //Stack trace.
                        XmlNode failureStackTraceNode = testCaseNode.SelectSingleNode("./failure/stack-trace");
                        if (failureStackTraceNode != null && !string.IsNullOrWhiteSpace(failureStackTraceNode.InnerText))
                        {
                            resultCreateModel.StackTrace = failureStackTraceNode.InnerText;
                        }
                    }
                    else if (testCaseNode.Attributes["result"] != null && string.Equals(testCaseNode.Attributes["result"].Value, "pass", StringComparison.OrdinalIgnoreCase))
                    {
                        resultCreateModel.Outcome = TestOutcome.Passed.ToString();
                    }
                    else
                    {
                        resultCreateModel.Outcome = TestOutcome.NotExecuted.ToString();
                    }

                    //Test priority.
                    XmlNode priorityTrait = testCaseNode.SelectSingleNode("./traits/trait[@name='priority']");
                    if (priorityTrait != null && priorityTrait.Attributes["value"] != null)
                    {
                        var priorityValue = priorityTrait.Attributes["value"].Value;
                        resultCreateModel.Priority = !string.IsNullOrEmpty(priorityValue) ? Convert.ToInt32(priorityValue)
                                                        : TestManagementConstants.UnspecifiedPriority;
                    }

                    //Test owner.
                    XmlNode ownerNode = testCaseNode.SelectSingleNode("./traits/trait[@name='owner']");
                    if (ownerNode != null && ownerNode.Attributes["value"] != null && ownerNode.Attributes["value"].Value != null)
                    {
                        IdentityRef ownerIdRef = new IdentityRef();
                        ownerIdRef.DisplayName = ownerNode.Attributes["value"].Value;
                        ownerIdRef.DirectoryAlias = ownerNode.Attributes["value"].Value;
                        resultCreateModel.Owner = ownerIdRef;
                    }

                    resultCreateModel.RunBy = runUserIdRef;

                    resultCreateModel.State = "Completed";

                    resultCreateModel.AutomatedTestType = Name;

                    if (!string.IsNullOrEmpty(resultCreateModel.AutomatedTestName) && !string.IsNullOrEmpty(resultCreateModel.TestCaseTitle))
                    {
                        results.Add(resultCreateModel);
                    }
                }
            }
            if (dateTimeParseError || assemblyRunDateTimeAttributesNotPresent)
            {
                executionContext.Warning("Atleast for one assembly start time was not obtained due to tag not present or parsing issue, total run duration will now be summation of time taken by each assembly");

                if (assemblyTimeAttributeNotPresent)
                {
                    executionContext.Warning("Atleast for one assembly time tag is not present, total run duration will now be summation of time from all test runs");
                }
            }

            //if minimum start time is not available then set it to present time
            minStartTime = minStartTime == DateTime.MaxValue ? DateTime.UtcNow : minStartTime ;

            //if start time cannot be obtained even for one assembly then fallback duration to sum of assembly run time
            //if assembly run time cannot be obtained even for one assembly then fallback duration to total test run
            maxCompletedTime = dateTimeParseError || assemblyRunDateTimeAttributesNotPresent || maxCompletedTime == DateTime.MinValue ? minStartTime.Add(assemblyTimeAttributeNotPresent ? TimeSpan.FromSeconds(testRunDuration) : TimeSpan.FromSeconds(assemblyRunDuration)) : maxCompletedTime;

            executionContext.Output(string.Format("Obtained XUnit Test Run Start Date: {0} and Completed Date: {1}", minStartTime.ToString("o"), maxCompletedTime.ToString("o")));
            TestRunData testRunData = new TestRunData(
                name: runName,
                buildId: runContext != null ? runContext.BuildId : 0,
                startedDate: minStartTime.ToString("o"),
                completedDate: maxCompletedTime.ToString("o"),
                state: TestRunState.InProgress.ToString(),
                isAutomated: true,
                buildFlavor: runContext != null ? runContext.Configuration : null,
                buildPlatform: runContext != null ? runContext.Platform : null,
                releaseUri: runContext != null ? runContext.ReleaseUri : null,
                releaseEnvironmentUri: runContext != null ? runContext.ReleaseEnvironmentUri : null
                );

            testRunData.Results = results.ToArray();
            testRunData.Attachments = AddResultsFileToRunLevelAttachments ? new string[] { filePath } : new string[0];

            return testRunData;
        }

        public bool AddResultsFileToRunLevelAttachments
        {
            get;
            set;
        }
    }
}
