using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    [ServiceLocator(Default = typeof(CodeCoverageServer))]
    public interface ICodeCoverageServer : IAgentService
    {
        void InitializeServer(IExecutionContext context, VssConnection connection);

        /// <summary>
        /// Publish Artifact to build
        /// </summary>
        void CreateArtifact(string project, int buildId, string type, string name, string fileContainerPath, bool browsable,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Publish code coverage summary
        /// </summary>
        void PublishCoverageSummary(string project, int buildId, IEnumerable<CodeCoverageStatistics> coverageData,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    internal class CodeCoverageServer : AgentService, ICodeCoverageServer
    {
        private const int _retryIntervalToConnectToTfsInMilliseconds = 1000;
        private const int _maxRetryCount = 10;
        private IExecutionContext _context;

        public void InitializeServer(IExecutionContext context, VssConnection connection)
        {
            ArgUtil.NotNull(context, nameof(context));
            _context = context;

            ArgUtil.NotNull(connection, nameof(connection));
            BuildHttpClient = connection.GetClient<BuildHttpClient>();
            TestHttpClient = connection.GetClient<TestManagementHttpClient>();
        }

        public void CreateArtifact(string project, int buildId, string type, string name, string fileContainerPath, bool browsable, CancellationToken cancellationToken = default(CancellationToken))
        {
            _context.Debug("Creating build artifacts with following parameters ");
            _context.Debug(StringUtil.Format(" project {0}", project));
            _context.Debug(StringUtil.Format(" buildId {0}", buildId));
            _context.Debug(StringUtil.Format(" type {0}", type));
            _context.Debug(StringUtil.Format(" name {0}", name));
            _context.Debug(StringUtil.Format(" fileContainerPath {0}", fileContainerPath));
            _context.Debug(StringUtil.Format(" browsable {0}", browsable));

            var browsableProperty = (browsable) ? bool.TrueString : bool.FalseString;
            var uploadArtifactCommand = new Command("Artifact", "Upload")
            {
                Properties =
                {
                    { "containerfolder", name},
                    { "artifactname", name },
                    { "artifacttype", type },
                    { "browsable", browsableProperty },
                },
                Data = fileContainerPath
            };
            var artifactCommand = new ArtifactCommands();
            artifactCommand.ProcessCommand(_context, uploadArtifactCommand);
        }

        public void PublishCoverageSummary(string project, int buildId, IEnumerable<CodeCoverageStatistics> coverageData, CancellationToken cancellationToken = default(CancellationToken))
        {
            _context.Debug("PublishCoverageSummary with following parameters ");
            _context.Debug(StringUtil.Format(" project {0}", project));
            _context.Debug(StringUtil.Format(" buildId {0}", buildId));
            foreach (var coverage in coverageData)
            {
                _context.Debug(StringUtil.Format(" {0}- {1} of {2} covered.", coverage.Label,
                                         coverage.Covered, coverage.Total));
            }

            // <todo: Bug 402783> We are currently passing BuildFlavor and BuildPlatform = "" There value are required be passed to command
            RunTaskWithRetriesOnServiceUnavailability(() => TestHttpClient.UpdateCodeCoverageSummaryAsync(project, buildId,
                new CodeCoverageData() { BuildFlavor = "", BuildPlatform = "", CoverageStats = coverageData.ToList() },
                cancellationToken: cancellationToken));
        }


        public BuildHttpClient BuildHttpClient { get; set; }
        public TestManagementHttpClient TestHttpClient { get; set; }


        /// <summary>
        /// Run the given function with a retry loop
        /// The function is a http call to TFS
        /// The idea is to build some resiliency into the calls wrt network connectivity
        /// </summary>
        /// <typeparam name="T">Function return type. It should be of type Task</typeparam>
        /// <param name="function">Function to retry</param>
        /// <param name="sync">Should the http call be sync?</param>
        /// <returns>Task of type T</returns>
        private T RunTaskWithRetriesOnServiceUnavailability<T>(Func<T> getTask) where T : System.Threading.Tasks.Task
        {
            var retryCount = _maxRetryCount;
            while (retryCount-- > 0)
            {
                try
                {
                    var restTask = getTask();

                    if (restTask != null)
                    {
                        restTask.Wait();
                    }
                    else
                    {
                        _context.Warning("RunTaskWithRetriesOnServiceUnavailability: getTask() returned null");
                    }

                    return restTask;
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException is TeamFoundationServiceUnavailableException)
                    {
                        _context.Warning(StringUtil.Loc("TFSConnectionDownRetrying", retryCount, ex.InnerException));

                        if (retryCount == 0)
                        {
                            throw;
                        }

                        Thread.Sleep(_retryIntervalToConnectToTfsInMilliseconds);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return null;
        }
    }
}
