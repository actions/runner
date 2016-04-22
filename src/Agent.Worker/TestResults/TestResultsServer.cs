using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    public class TestResultsServer
    {
        private VssConnection _connection;

        private TestManagementHttpClient TestHttpClient { get; }

        public TestResultsServer(VssConnection connection)
        {
            ArgUtil.NotNull(connection, nameof(connection));
            _connection = connection;

            TestHttpClient = connection.GetClient<TestManagementHttpClient>();
        }

        public async Task<List<TestCaseResult>> AddTestResultsToTestRun(
            TestResultCreateModel[] currentBatch,
            string projectName,
            int testRunId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TestHttpClient.AddTestResultsToTestRunAsync(currentBatch, projectName, testRunId, cancellationToken);
        }

        public async Task<TestRun> CreateTestRun(
           string projectName,
           RunCreateModel testRunData,
           CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TestHttpClient.CreateTestRunAsync(projectName, testRunData, cancellationToken);
        }

        public async Task<TestRun> UpdateTestRun(
         string projectName,
         int testRunId,
         RunUpdateModel updateModel,
         CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TestHttpClient.UpdateTestRunAsync(projectName, testRunId, updateModel, cancellationToken);
        }

        public async Task<TestAttachmentReference> CreateTestRunAttachment(
            TestAttachmentRequestModel reqModel,
            string projectName,
            int testRunId,
         CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TestHttpClient.CreateTestRunAttachmentAsync(reqModel, projectName, testRunId, cancellationToken);
        }

        public async Task<TestAttachmentReference> CreateTestResultAttachment(
            TestAttachmentRequestModel reqModel,
            string projectName,
            int testRunId,
            int testCaseResultId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TestHttpClient.CreateTestResultAttachmentAsync(reqModel, projectName, testRunId, testCaseResultId, cancellationToken);
        }
    }
}
