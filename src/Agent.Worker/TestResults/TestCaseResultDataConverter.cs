using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.TestManagement.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public static class TestCaseResultDataConverter
    {
        public static void Convert(TestCaseResultData testCaseResultData, TestCaseResult testCaseResultWebApi)
        {
            testCaseResultWebApi.Area = testCaseResultData.Area;
            testCaseResultWebApi.AssociatedBugs = testCaseResultData.AssociatedBugs;
            testCaseResultWebApi.AutomatedTestId = testCaseResultData.AutomatedTestId;
            testCaseResultWebApi.AutomatedTestName = testCaseResultData.AutomatedTestName;
            testCaseResultWebApi.AutomatedTestStorage = testCaseResultData.AutomatedTestStorage;
            testCaseResultWebApi.AutomatedTestType = testCaseResultData.AutomatedTestType;
            testCaseResultWebApi.AutomatedTestTypeId = testCaseResultData.AutomatedTestTypeId;
            testCaseResultWebApi.Build = testCaseResultData.Build;
            testCaseResultWebApi.BuildReference = testCaseResultData.BuildReference;
            testCaseResultWebApi.Comment = testCaseResultData.Comment;
            testCaseResultWebApi.CompletedDate = testCaseResultData.CompletedDate;
            testCaseResultWebApi.ComputerName = testCaseResultData.ComputerName;
            testCaseResultWebApi.Configuration = testCaseResultData.Configuration;
            testCaseResultWebApi.CreatedDate = testCaseResultData.CreatedDate;
            testCaseResultWebApi.CustomFields = testCaseResultData.CustomFields;
            testCaseResultWebApi.DurationInMs = testCaseResultData.DurationInMs;
            testCaseResultWebApi.ErrorMessage = testCaseResultData.ErrorMessage;
            testCaseResultWebApi.FailureType = testCaseResultData.FailureType;
            testCaseResultWebApi.Id = testCaseResultData.Id;
            testCaseResultWebApi.IterationDetails = testCaseResultData.IterationDetails;
            testCaseResultWebApi.LastUpdatedBy = testCaseResultData.LastUpdatedBy;
            testCaseResultWebApi.LastUpdatedDate = testCaseResultData.LastUpdatedDate;
            testCaseResultWebApi.Outcome = testCaseResultData.Outcome;
            testCaseResultWebApi.Owner = testCaseResultData.Owner;
            testCaseResultWebApi.Priority = testCaseResultData.Priority;
            testCaseResultWebApi.Project = testCaseResultData.Project;
            testCaseResultWebApi.Release = testCaseResultData.Release;
            testCaseResultWebApi.ReleaseReference = testCaseResultData.ReleaseReference;
            testCaseResultWebApi.ResetCount = testCaseResultData.ResetCount;
            testCaseResultWebApi.ResolutionState = testCaseResultData.ResolutionState;
            testCaseResultWebApi.ResolutionStateId = testCaseResultData.ResolutionStateId;
            testCaseResultWebApi.ResultGroupType = testCaseResultData.ResultGroupType;
            testCaseResultWebApi.Revision = testCaseResultData.Revision;
            testCaseResultWebApi.RunBy = testCaseResultData.RunBy;
            testCaseResultWebApi.StackTrace = testCaseResultData.StackTrace;
            testCaseResultWebApi.StartedDate = testCaseResultData.StartedDate;
            testCaseResultWebApi.State = testCaseResultData.State;
            testCaseResultWebApi.TestCase = testCaseResultData.TestCase;
            testCaseResultWebApi.TestCaseReferenceId = testCaseResultData.TestCaseReferenceId;
            testCaseResultWebApi.TestCaseTitle = testCaseResultData.TestCaseTitle;
            testCaseResultWebApi.TestPlan = testCaseResultData.TestPlan;
            testCaseResultWebApi.TestPoint = testCaseResultData.TestPoint;
            testCaseResultWebApi.TestRun = testCaseResultData.TestRun;
            testCaseResultWebApi.TestSuite = testCaseResultData.TestSuite;
            testCaseResultWebApi.Url = testCaseResultData.Url;

            ConvertSubResults(testCaseResultData, testCaseResultWebApi);
        }

        private static void ConvertSubResults(TestCaseResultData testCaseResultData, TestCaseResult testCaseResultWebApi)
        {
            if (testCaseResultData.TestCaseSubResultData == null || !testCaseResultData.TestCaseSubResultData.Any())
            {
                return;
            }

            testCaseResultWebApi.SubResults = new List<TestSubResult>();
            foreach (var subResultData in testCaseResultData.TestCaseSubResultData)
            {
                var subResultWebApi = new TestSubResult();
                TestCaseSubResultDataConverter.Convert(subResultData, subResultWebApi);
                testCaseResultWebApi.SubResults.Add(subResultWebApi);
            }
        }
    }
}
