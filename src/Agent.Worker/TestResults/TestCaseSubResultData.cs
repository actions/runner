using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.TestManagement.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public class TestCaseSubResultData
    {
        public int Id;

        public TestCaseResultIdentifier TestResult { get; set; }

        public int ParentId;

        public int SequenceId;

        public string DisplayName;

        public ResultGroupType ResultGroupType;

        public string Outcome;

        public string Comment;

        public string ErrorMessage;

        public DateTime StartedDate;

        public DateTime CompletedDate;

        public long DurationInMs;

        public ShallowReference Configuration;

        public DateTime LastUpdatedDate;

        public string ComputerName;

        public string StackTrace;

        public List<CustomTestField> CustomFields;

        public string Url;

        public List<TestCaseSubResultData> SubResultData { get; set; }

        public AttachmentData AttachmentData { get; set; }
    }
}