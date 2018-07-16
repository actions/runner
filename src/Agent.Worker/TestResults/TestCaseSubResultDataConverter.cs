using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.TestManagement.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public static class TestCaseSubResultDataConverter
    {
        public static void Convert(TestCaseSubResultData testCaseSubResultData, TestSubResult testSubResultWebApi)
        {
            testSubResultWebApi.CompletedDate = testCaseSubResultData.CompletedDate;
            testSubResultWebApi.Comment = testCaseSubResultData.Comment;
            testSubResultWebApi.ComputerName = testCaseSubResultData.ComputerName;
            testSubResultWebApi.Configuration = testCaseSubResultData.Configuration;
            testSubResultWebApi.CustomFields = testCaseSubResultData.CustomFields;
            testSubResultWebApi.DurationInMs = testCaseSubResultData.DurationInMs;
            testSubResultWebApi.DisplayName = testCaseSubResultData.DisplayName;
            testSubResultWebApi.ErrorMessage = testCaseSubResultData.ErrorMessage;
            testSubResultWebApi.Id = testCaseSubResultData.Id;
            testSubResultWebApi.LastUpdatedDate = testCaseSubResultData.LastUpdatedDate;
            testSubResultWebApi.Outcome = testCaseSubResultData.Outcome;
            testSubResultWebApi.ParentId = testCaseSubResultData.ParentId;
            testSubResultWebApi.ResultGroupType = testCaseSubResultData.ResultGroupType;
            testSubResultWebApi.StackTrace = testCaseSubResultData.StackTrace;
            testSubResultWebApi.SequenceId = testCaseSubResultData.SequenceId;
            testSubResultWebApi.StartedDate = testCaseSubResultData.StartedDate;
            testSubResultWebApi.TestResult = testCaseSubResultData.TestResult;
            testSubResultWebApi.Url = testCaseSubResultData.Url;
            ConvertSubResults(testCaseSubResultData, testSubResultWebApi);
        }

        private static void ConvertSubResults(TestCaseSubResultData testCaseSubResultData, TestSubResult testSubResultWebApi)
        {
            if (testCaseSubResultData.SubResultData == null || !testCaseSubResultData.SubResultData.Any())
            {
                return;
            }

            testSubResultWebApi.SubResults = new List<TestSubResult>();
            foreach (var subResultData in testCaseSubResultData.SubResultData)
            {
                var subResultWebApi = new TestSubResult();
                TestCaseSubResultDataConverter.Convert(subResultData, subResultWebApi);
                testSubResultWebApi.SubResults.Add(subResultWebApi);
            }
        }
    }
}
