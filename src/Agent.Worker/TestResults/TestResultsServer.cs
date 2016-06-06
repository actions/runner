using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Client;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.TestResults
{
    [ServiceLocator(Default = typeof(TestResultsServer))]
    public interface ITestResultsServer : IAgentService
    {
        void InitializeServer(VssConnection connection);
        Task<List<TestCaseResult>> AddTestResultsToTestRunAsync(TestResultCreateModel[] currentBatch, string projectName, int testRunId, CancellationToken cancellationToken = default(CancellationToken));
        Task<TestRun> CreateTestRunAsync(string projectName, RunCreateModel testRunData, CancellationToken cancellationToken = default(CancellationToken));
        Task<TestRun> UpdateTestRunAsync(string projectName, int testRunId, RunUpdateModel updateModel, CancellationToken cancellationToken = default(CancellationToken));
        Task<TestAttachmentReference> CreateTestRunAttachmentAsync(TestAttachmentRequestModel reqModel, string projectName, int testRunId, CancellationToken cancellationToken = default(CancellationToken));
        Task<TestAttachmentReference> CreateTestResultAttachmentAsync(TestAttachmentRequestModel reqModel, string projectName, int testRunId, int testCaseResultId, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class TestResultsServer : AgentService, ITestResultsServer
    {
        private VssConnection _connection;

        private TestManagementHttpClient TestHttpClient { get; set; }

        public void InitializeServer(VssConnection connection)
        {
            ArgUtil.NotNull(connection, nameof(connection));
            _connection = connection;

            TestHttpClient = connection.GetClient<TestManagementHttpClient>();
        }

        public async Task<List<TestCaseResult>> AddTestResultsToTestRunAsync(
            TestResultCreateModel[] currentBatch,
            string projectName,
            int testRunId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TestHttpClient.AddTestResultsToTestRunAsync(currentBatch, projectName, testRunId, cancellationToken);
        }

        public async Task<TestRun> CreateTestRunAsync(
           string projectName,
           RunCreateModel testRunData,
           CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TestHttpClient.CreateTestRunAsync(testRunData, projectName, cancellationToken);
        }

        public async Task<TestRun> UpdateTestRunAsync(
         string projectName,
         int testRunId,
         RunUpdateModel updateModel,
         CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TestHttpClient.UpdateTestRunAsync(updateModel, projectName, testRunId, cancellationToken);
        }

        public async Task<TestAttachmentReference> CreateTestRunAttachmentAsync(
            TestAttachmentRequestModel reqModel,
            string projectName,
            int testRunId,
         CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TestHttpClient.CreateTestRunAttachmentAsync(reqModel, projectName, testRunId, cancellationToken);
        }

        public async Task<TestAttachmentReference> CreateTestResultAttachmentAsync(
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
