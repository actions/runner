using Microsoft.TeamFoundation.TestManagement.WebApi;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public class TestRunData : RunCreateModel
    {
        public TestRunData(string name = "", string iteration = "", int[] pointIds = null, ShallowReference plan = null, ShallowReference testSettings = null, int buildId = 0, string state = "", bool? isAutomated = default(bool?), string errorMessage = "", string dueDate = "", string type = "", string controller = "", string buildDropLocation = "", string comment = "", string testEnvironmentId = "", string startedDate = "", string completedDate = "", int[] configIds = null, RunFilter filter = null, ShallowReference dtlTestEnvironment = null, DtlEnvironmentDetails environmentDetails = null, string buildPlatform = null, string buildFlavor = null, string releaseUri = null, string releaseEnvironmentUri = null)
        : base(name, iteration, pointIds, plan, testSettings, buildId, state, isAutomated, errorMessage, dueDate, type, controller, buildDropLocation, comment, testEnvironmentId, startedDate, completedDate, configIds, filter, dtlTestEnvironment, environmentDetails, null, buildPlatform, buildFlavor, releaseUri, releaseEnvironmentUri)
        {
            CustomTestFields = new List<CustomTestField>();
        }

        public string[] Attachments { get; set; }

        // Results 
        public TestCaseResultData[] Results { get; set; }

        public void AddCustomField(string fieldName, string value)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(fieldName))
            {
                return;
            }

            CustomTestFields.Add(new CustomTestField() { FieldName = fieldName, Value = value });
        }
    }
}