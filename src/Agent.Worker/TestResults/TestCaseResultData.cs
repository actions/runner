using Microsoft.TeamFoundation.TestManagement.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public class TestCaseResultData : TestCaseResult
    {
        public string ConsoleLog { get; set; }

        public string[] Attachments { get; set; }
    }
}