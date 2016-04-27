using Microsoft.TeamFoundation.TestManagement.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public class TestCaseResultData : TestResultCreateModel
    {
        public string[] Attachments { get; set; }
    }
}