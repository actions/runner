using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    /// <summary>
    /// This class is used to contain attachments list for the test run and the attachments for sub results.
    /// </summary>
    public class AttachmentData
    {
        /// <summary>
        /// List of Filepaths of attachments associated with the TestRun.
        /// </summary>
        public IList<string> AttachmentsFilePathList { get; set; }

        /// <summary>
        /// Console log of the Test Run.
        /// </summary>
        public string ConsoleLog { get; set; }

        /// <summary>
        /// Standard Error of the Test Run.
        /// </summary>
        public string StandardError { get; set; }
    }
}