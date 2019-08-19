using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GitHub.Runner.Worker.Container;
using GitHub.Services.WebApi;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using Newtonsoft.Json;
using System.Text;

namespace GitHub.Runner.Worker
{
    public class ExecutionContextType
    {
        public static string Job = "Job";
        public static string Task = "Task";
    }

    [ServiceLocator(Default = typeof(ExecutionContext))]
    public interface IExecutionContext : IRunnerService
    {
        Guid Id { get; }
        string ScopeName { get; }
        string ContextName { get; }
        Task ForceCompleted { get; }
        TaskResult? Result { get; set; }
        string ResultCode { get; set; }
        TaskResult? CommandResult { get; set; }
        CancellationToken CancellationToken { get; }
        List<ServiceEndpoint> Endpoints { get; }
        List<SecureFile> SecureFiles { get; }

        PlanFeatures Features { get; }
        Variables Variables { get; }
        HashSet<string> OutputVariables { get; }
        IDictionary<String, String> EnvironmentVariables { get; }
        IDictionary<String, ContextScope> Scopes { get; }
        StepsContext StepsContext { get; }
        IDictionary<String, PipelineContextData> ExpressionValues { get; }
        List<IAsyncCommandContext> AsyncCommands { get; }
        List<string> PrependPath { get; }
        ContainerInfo Container { get; }
        List<ContainerInfo> SidecarContainers { get; }
        JobContext JobContext { get; }

        // Initialize
        void InitializeJob(Pipelines.AgentJobRequestMessage message, CancellationToken token);
        void CancelToken();
        IExecutionContext CreateChild(Guid recordId, string displayName, string refName, string scopeName, string contextName, Variables taskVariables = null, bool outputForward = false);

        // logging
        bool WriteDebug { get; }
        long Write(string tag, string message);
        void QueueAttachFile(string type, string name, string filePath);

        // timeline record update methods
        void Start(string currentOperation = null);
        TaskResult Complete(TaskResult? result = null, string currentOperation = null, string resultCode = null);
        string GetRunnerContext(string name);
        void SetRunnerContext(string name, string value);
        string GetGitHubContext(string name);
        void SetGitHubContext(string name, string value);
        void SetOutput(string name, string value, out string reference);
        void SetTimeout(TimeSpan? timeout);
        void AddIssue(Issue issue, string message = null);
        void Progress(int percentage, string currentOperation = null);
        void UpdateDetailTimelineRecord(TimelineRecord record);

        // matchers
        void Add(OnMatcherChanged handler);
        void Remove(OnMatcherChanged handler);
        void AddMatchers(IssueMatchersConfig matcher);
        void RemoveMatchers(IEnumerable<string> owners);
        IEnumerable<IssueMatcherConfig> GetMatchers();

        // others
        void ForceTaskComplete();
    }

    public sealed class ExecutionContext : RunnerService, IExecutionContext
    {
        private const int _maxIssueCount = 10;

        private readonly TimelineRecord _record = new TimelineRecord();
        private readonly Dictionary<Guid, TimelineRecord> _detailRecords = new Dictionary<Guid, TimelineRecord>();
        private readonly object _loggerLock = new object();
        private readonly List<IAsyncCommandContext> _asyncCommands = new List<IAsyncCommandContext>();
        private readonly HashSet<string> _outputvariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object _matchersLock = new object();

        private event OnMatcherChanged _onMatcherChanged;

        private IssueMatcherConfig[] _matchers;

        private IRunnerLogPlugin _logPlugin;
        private IPagingLogger _logger;
        private IJobServerQueue _jobServerQueue;
        private ExecutionContext _parentExecutionContext;

        private bool _outputForward = false;
        private Guid _mainTimelineId;
        private Guid _detailTimelineId;
        private int _childTimelineRecordOrder = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private TaskCompletionSource<int> _forceCompleted = new TaskCompletionSource<int>();
        private bool _throttlingReported = false;

        // only job level ExecutionContext will track throttling delay.
        private long _totalThrottlingDelayInMilliseconds = 0;

        public Guid Id => _record.Id;
        public string ScopeName { get; private set; }
        public string ContextName { get; private set; }
        public Task ForceCompleted => _forceCompleted.Task;
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        public List<ServiceEndpoint> Endpoints { get; private set; }
        public List<SecureFile> SecureFiles { get; private set; }
        public Variables Variables { get; private set; }
        // public Variables TaskVariables { get; private set; }
        public HashSet<string> OutputVariables => _outputvariables;
        public IDictionary<String, String> EnvironmentVariables { get; private set; }
        public IDictionary<String, ContextScope> Scopes { get; private set; }
        public StepsContext StepsContext { get; private set; }
        public IDictionary<String, PipelineContextData> ExpressionValues { get; } = new Dictionary<String, PipelineContextData>();
        public bool WriteDebug { get; private set; }
        public List<string> PrependPath { get; private set; }
        public ContainerInfo Container { get; private set; }
        public List<ContainerInfo> SidecarContainers { get; private set; }

        public List<IAsyncCommandContext> AsyncCommands => _asyncCommands;

        public TaskResult? Result
        {
            get
            {
                return _record.Result;
            }
            set
            {
                _record.Result = value;
            }
        }

        public TaskResult? CommandResult { get; set; }

        private string ContextType => _record.RecordType;

        public string ResultCode
        {
            get
            {
                return _record.ResultCode;
            }
            set
            {
                _record.ResultCode = value;
            }
        }

        public PlanFeatures Features { get; private set; }

        private ExecutionContext Root
        {
            get
            {
                var result = this;

                while (result._parentExecutionContext != null)
                {
                    result = result._parentExecutionContext;
                }

                return result;
            }
        }

        public JobContext JobContext
        {
            get
            {
                return ExpressionValues["job"] as JobContext;
            }
        }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _jobServerQueue = HostContext.GetService<IJobServerQueue>();
        }

        public void CancelToken()
        {
            try
            {
                _cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException e)
            {
                Trace.Info($"Attempted to cancel a disposed token, the execution is already complete: {e.ToString()}");
            }
        }

        public void ForceTaskComplete()
        {
            Trace.Info("Force finish current task in 5 sec.");
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                _forceCompleted?.TrySetResult(1);
            });
        }

        public IExecutionContext CreateChild(Guid recordId, string displayName, string refName, string scopeName, string contextName, Variables taskVariables = null, bool outputForward = false)
        {
            Trace.Entering();

            var child = new ExecutionContext();
            child.Initialize(HostContext);
            child.ScopeName = scopeName;
            child.ContextName = contextName;
            child.Features = Features;
            child.Variables = Variables;
            child.Endpoints = Endpoints;
            child.SecureFiles = SecureFiles;
            // child.TaskVariables = taskVariables;
            child.EnvironmentVariables = EnvironmentVariables;
            child.Scopes = Scopes;
            child.StepsContext = StepsContext;
            foreach (var pair in ExpressionValues)
            {
                child.ExpressionValues[pair.Key] = pair.Value;
            }
            child._cancellationTokenSource = new CancellationTokenSource();
            child.WriteDebug = WriteDebug;
            child._parentExecutionContext = this;
            child.PrependPath = PrependPath;
            child.Container = Container;
            child.SidecarContainers = SidecarContainers;
            child._outputForward = outputForward;

            child.InitializeTimelineRecord(_mainTimelineId, recordId, _record.Id, ExecutionContextType.Task, displayName, refName, ++_childTimelineRecordOrder);

            child._logger = HostContext.CreateService<IPagingLogger>();
            child._logger.Setup(_mainTimelineId, recordId);

            return child;
        }

        public void Start(string currentOperation = null)
        {
            _record.CurrentOperation = currentOperation ?? _record.CurrentOperation;
            _record.StartTime = DateTime.UtcNow;
            _record.State = TimelineRecordState.InProgress;

            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
        }

        public TaskResult Complete(TaskResult? result = null, string currentOperation = null, string resultCode = null)
        {
            if (result != null)
            {
                Result = result;
            }

            // report total delay caused by server throttling.
            if (_totalThrottlingDelayInMilliseconds > 0)
            {
                this.Warning($"The job has experienced {TimeSpan.FromMilliseconds(_totalThrottlingDelayInMilliseconds).TotalSeconds} seconds total delay caused by server throttling.");
            }

            _record.CurrentOperation = currentOperation ?? _record.CurrentOperation;
            _record.ResultCode = resultCode ?? _record.ResultCode;
            _record.FinishTime = DateTime.UtcNow;
            _record.PercentComplete = 100;
            _record.Result = _record.Result ?? TaskResult.Succeeded;
            _record.State = TimelineRecordState.Completed;

            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);

            // complete all detail timeline records.
            if (_detailTimelineId != Guid.Empty && _detailRecords.Count > 0)
            {
                foreach (var record in _detailRecords)
                {
                    record.Value.FinishTime = record.Value.FinishTime ?? DateTime.UtcNow;
                    record.Value.PercentComplete = record.Value.PercentComplete ?? 100;
                    record.Value.Result = record.Value.Result ?? TaskResult.Succeeded;
                    record.Value.State = TimelineRecordState.Completed;

                    _jobServerQueue.QueueTimelineRecordUpdate(_detailTimelineId, record.Value);
                }
            }

            _cancellationTokenSource?.Dispose();

            _logger.End();

            return Result.Value;
        }

        public void SetRunnerContext(string name, string value)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            var runnerContext = ExpressionValues["runner"] as RunnerContext;
            runnerContext[name] = new StringContextData(value);
        }

        public string GetRunnerContext(string name)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            var runnerContext = ExpressionValues["runner"] as RunnerContext;
            if (runnerContext.TryGetValue(name, out var value))
            {
                return value as StringContextData;
            }
            else
            {
                return null;
            }
        }

        public void SetGitHubContext(string name, string value)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            var githubContext = ExpressionValues["github"] as GitHubContext;
            githubContext[name] = new StringContextData(value);
        }

        public string GetGitHubContext(string name)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            var githubContext = ExpressionValues["github"] as GitHubContext;
            if (githubContext.TryGetValue(name, out var value))
            {
                if (value is StringContextData)
                {
                    return value as StringContextData;
                }
                else
                {
                    return value.ToJToken().ToString(Formatting.Indented);
                }
            }
            else
            {
                return null;
            }
        }

        public void SetOutput(string name, string value, out string reference)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));

            if (String.IsNullOrEmpty(ContextName))
            {
                reference = null;
                return;
            }

            // todo: restrict multiline?

            StepsContext.SetOutput(ScopeName, ContextName, name, value, out reference);
        }

        public void SetTimeout(TimeSpan? timeout)
        {
            if (timeout != null)
            {
                _cancellationTokenSource.CancelAfter(timeout.Value);
            }
        }

        public void Progress(int percentage, string currentOperation = null)
        {
            if (percentage > 100 || percentage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage));
            }

            _record.CurrentOperation = currentOperation ?? _record.CurrentOperation;
            _record.PercentComplete = Math.Max(percentage, _record.PercentComplete.Value);

            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
        }

        // This is not thread safe, the caller need to take lock before calling issue()
        public void AddIssue(Issue issue, string logMessage = null)
        {
            ArgUtil.NotNull(issue, nameof(issue));

            if (string.IsNullOrEmpty(logMessage))
            {
                logMessage = issue.Message;
            }

            issue.Message = HostContext.SecretMasker.MaskSecrets(issue.Message);

            if (issue.Type == IssueType.Error)
            {
                // tracking line number for each issue in log file
                // log UI use this to navigate from issue to log
                if (!string.IsNullOrEmpty(logMessage))
                {
                    long logLineNumber = Write(WellKnownTags.Error, logMessage);
                    issue.Data["logFileLineNumber"] = logLineNumber.ToString();
                }

                if (_record.ErrorCount < _maxIssueCount)
                {
                    _record.Issues.Add(issue);
                }

                _record.ErrorCount++;
            }
            else if (issue.Type == IssueType.Warning)
            {
                // tracking line number for each issue in log file
                // log UI use this to navigate from issue to log
                if (!string.IsNullOrEmpty(logMessage))
                {
                    long logLineNumber = Write(WellKnownTags.Warning, logMessage);
                    issue.Data["logFileLineNumber"] = logLineNumber.ToString();
                }

                if (_record.WarningCount < _maxIssueCount)
                {
                    _record.Issues.Add(issue);
                }

                _record.WarningCount++;
            }

            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
        }

        public void UpdateDetailTimelineRecord(TimelineRecord record)
        {
            ArgUtil.NotNull(record, nameof(record));

            if (record.RecordType == ExecutionContextType.Job)
            {
                throw new ArgumentOutOfRangeException(nameof(record));
            }

            if (_detailTimelineId == Guid.Empty)
            {
                // create detail timeline
                _detailTimelineId = Guid.NewGuid();
                _record.Details = new Timeline(_detailTimelineId);

                _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
            }

            TimelineRecord existRecord;
            if (_detailRecords.TryGetValue(record.Id, out existRecord))
            {
                existRecord.Name = record.Name ?? existRecord.Name;
                existRecord.RecordType = record.RecordType ?? existRecord.RecordType;
                existRecord.Order = record.Order ?? existRecord.Order;
                existRecord.ParentId = record.ParentId ?? existRecord.ParentId;
                existRecord.StartTime = record.StartTime ?? existRecord.StartTime;
                existRecord.FinishTime = record.FinishTime ?? existRecord.FinishTime;
                existRecord.PercentComplete = record.PercentComplete ?? existRecord.PercentComplete;
                existRecord.CurrentOperation = record.CurrentOperation ?? existRecord.CurrentOperation;
                existRecord.Result = record.Result ?? existRecord.Result;
                existRecord.ResultCode = record.ResultCode ?? existRecord.ResultCode;
                existRecord.State = record.State ?? existRecord.State;

                _jobServerQueue.QueueTimelineRecordUpdate(_detailTimelineId, existRecord);
            }
            else
            {
                _detailRecords[record.Id] = record;
                _jobServerQueue.QueueTimelineRecordUpdate(_detailTimelineId, record);
            }
        }

        public void InitializeJob(Pipelines.AgentJobRequestMessage message, CancellationToken token)
        {
            // Validation
            Trace.Entering();
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Resources, nameof(message.Resources));
            ArgUtil.NotNull(message.Variables, nameof(message.Variables));
            ArgUtil.NotNull(message.Plan, nameof(message.Plan));

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            // Features
            Features = PlanUtil.GetFeatures(message.Plan);

            // Endpoints
            Endpoints = message.Resources.Endpoints;

            // SecureFiles
            SecureFiles = message.Resources.SecureFiles;

            // Variables
            Variables = new Variables(HostContext, message.Variables);

            // Environment variables shared across all actions
            EnvironmentVariables = new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer);

            // Steps context (StepsRunner manages adding the scoped steps context)
            StepsContext = new StepsContext();

            // Scopes
            Scopes = new Dictionary<String, ContextScope>(StringComparer.OrdinalIgnoreCase);
            if (message.Scopes?.Count > 0)
            {
                foreach (var scope in message.Scopes)
                {
                    Scopes[scope.Name] = scope;
                }
            }

            // Expression values
            if (message.ContextData?.Count > 0)
            {
                foreach (var pair in message.ContextData)
                {
                    ExpressionValues[pair.Key] = pair.Value;
                }
            }

            ExpressionValues["secrets"] = Variables.ToSecretsContext();
            ExpressionValues["runner"] = new RunnerContext();
            ExpressionValues["job"] = new JobContext();

            if (!ExpressionValues.ContainsKey("github"))
            {
                var githubContext = new GitHubContext();
                ExpressionValues["github"] = githubContext;

                // Populate action environment variables
                var selfRepo = message.Resources.Repositories.Single(x => string.Equals(x.Alias, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase));

                // GITHUB_ACTOR=ericsciple
                githubContext["actor"] = new StringContextData(selfRepo.Properties.Get<Pipelines.VersionInfo>(Pipelines.RepositoryPropertyNames.VersionInfo)?.Author ?? string.Empty);

                // GITHUB_REPOSITORY=bryanmacfarlane/actionstest
                githubContext["repository"] = new StringContextData(selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Name, string.Empty));

                // GITHUB_WORKSPACE=/github/workspace

                // GITHUB_SHA=1a204f473f6001b7fac9c6453e76702f689a41a9
                githubContext["sha"] = new StringContextData(selfRepo.Version);

                // GITHUB_REF=refs/heads/master
                githubContext["ref"] = new StringContextData(selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Ref, string.Empty));

                // GITHUB_TOKEN=TOKEN
                if (selfRepo.Endpoint != null)
                {
                    var repoEndpoint = message.Resources.Endpoints.FirstOrDefault(x => x.Id == selfRepo.Endpoint.Id);
                    if (repoEndpoint?.Authorization?.Parameters != null && repoEndpoint.Authorization.Parameters.ContainsKey("accessToken"))
                    {
                        var githubAccessToken = repoEndpoint.Authorization.Parameters["accessToken"];
                        githubContext["token"] = new StringContextData(githubAccessToken);
                        var base64EncodingToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"x-access-token:{githubAccessToken}"));
                        HostContext.SecretMasker.AddValue(base64EncodingToken);
                    }
                }

                // HOME=/github/home
                // Environment["HOME"] = "/github/home";

                // GITHUB_WORKFLOW=test on push
                githubContext["workflow"] = new StringContextData(Variables.Build_DefinitionName);

                // GITHUB_EVENT_NAME=push
                githubContext["event_name"] = new StringContextData(Variables.Get("build.reason"));

                // GITHUB_ACTION=dump.env
                githubContext["action"] = new StringContextData(Variables.Build_Number);

                // GITHUB_EVENT_PATH=/github/workflow/event.json
            }
            else
            {
                var githubContext = new GitHubContext();
                var ghDictionary = (DictionaryContextData)ExpressionValues["github"];
                Trace.Info("Initialize GitHub context");
                foreach (var pair in ghDictionary)
                {
                    githubContext.Add(pair.Key, pair.Value);
                }

                // GITHUB_TOKEN=TOKEN
                var githubAccessToken = Variables.Get("system.github.token");
                if (!string.IsNullOrEmpty(githubAccessToken))
                {
                    githubContext["token"] = new StringContextData(githubAccessToken);
                    var base64EncodingToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"x-access-token:{githubAccessToken}"));
                    HostContext.SecretMasker.AddValue(base64EncodingToken);
                }
                else
                {
                    var selfRepo = message.Resources.Repositories.Single(x => string.Equals(x.Alias, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase));
                    if (selfRepo.Endpoint != null)
                    {
                        var repoEndpoint = message.Resources.Endpoints.FirstOrDefault(x => x.Id == selfRepo.Endpoint.Id);
                        if (repoEndpoint?.Authorization?.Parameters != null && repoEndpoint.Authorization.Parameters.ContainsKey("accessToken"))
                        {
                            githubAccessToken = repoEndpoint.Authorization.Parameters["accessToken"];
                            githubContext["token"] = new StringContextData(githubAccessToken);
                            var base64EncodingToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"x-access-token:{githubAccessToken}"));
                            HostContext.SecretMasker.AddValue(base64EncodingToken);
                        }
                    }
                }
                ExpressionValues["github"] = githubContext;
            }

            // Prepend Path
            PrependPath = new List<string>();

            // Docker (JobContainer)
            if (!string.IsNullOrEmpty(message.JobContainer))
            {
                Container = new ContainerInfo(HostContext, message.Resources.Containers.Single(x => string.Equals(x.Alias, message.JobContainer, StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                Container = null;
            }

            // Docker (Sidecar Containers)
            SidecarContainers = new List<ContainerInfo>();
            foreach (var sidecar in message.JobSidecarContainers)
            {
                var networkAlias = sidecar.Key;
                var containerResourceAlias = sidecar.Value;
                var containerResource = message.Resources.Containers.Single(c => string.Equals(c.Alias, containerResourceAlias, StringComparison.OrdinalIgnoreCase));
                SidecarContainers.Add(new ContainerInfo(HostContext, containerResource, isJobContainer: false) { ContainerNetworkAlias = networkAlias });
            }

            // Proxy variables
            //             var agentWebProxy = HostContext.GetService<IRunnerWebProxy>();
            //             if (!string.IsNullOrEmpty(agentWebProxy.ProxyAddress))
            //             {
            //                 SetRunnerContext("proxyurl", agentWebProxy.ProxyAddress);

            //                 if (!string.IsNullOrEmpty(agentWebProxy.ProxyUsername))
            //                 {
            //                     SetRunnerContext("proxyusername", agentWebProxy.ProxyUsername);
            //                 }

            //                 if (!string.IsNullOrEmpty(agentWebProxy.ProxyPassword))
            //                 {
            //                     HostContext.SecretMasker.AddValue(agentWebProxy.ProxyPassword);
            //                     SetRunnerContext("proxypassword", agentWebProxy.ProxyPassword);
            //                 }

            //                 if (agentWebProxy.ProxyBypassList.Count > 0)
            //                 {
            //                     SetRunnerContext("proxybypasslist", JsonUtility.ToString(agentWebProxy.ProxyBypassList));
            //                 }
            //             }

            //             // Certificate variables
            //             var agentCert = HostContext.GetService<IRunnerCertificateManager>();
            //             if (agentCert.SkipServerCertificateValidation)
            //             {
            //                 SetRunnerContext("sslskipcertvalidation", bool.TrueString);
            //             }

            //             if (!string.IsNullOrEmpty(agentCert.CACertificateFile))
            //             {
            //                 SetRunnerContext("sslcainfo", agentCert.CACertificateFile);
            //             }

            //             if (!string.IsNullOrEmpty(agentCert.ClientCertificateFile) &&
            //                 !string.IsNullOrEmpty(agentCert.ClientCertificatePrivateKeyFile) &&
            //                 !string.IsNullOrEmpty(agentCert.ClientCertificateArchiveFile))
            //             {
            //                 SetRunnerContext("clientcertfile", agentCert.ClientCertificateFile);
            //                 SetRunnerContext("clientcertprivatekey", agentCert.ClientCertificatePrivateKeyFile);
            //                 SetRunnerContext("clientcertarchive", agentCert.ClientCertificateArchiveFile);

            //                 if (!string.IsNullOrEmpty(agentCert.ClientCertificatePassword))
            //                 {
            //                     HostContext.SecretMasker.AddValue(agentCert.ClientCertificatePassword);
            //                     SetRunnerContext("clientcertpassword", agentCert.ClientCertificatePassword);
            //                 }
            //             }

            //             // Runtime option variables
            //             var runtimeOptions = HostContext.GetService<IConfigurationStore>().GetRunnerRuntimeOptions();
            //             if (runtimeOptions != null)
            //             {
            // #if OS_WINDOWS
            //                 if (runtimeOptions.GitUseSecureChannel)
            //                 {
            //                     SetRunnerContext("gituseschannel", runtimeOptions.GitUseSecureChannel.ToString());
            //                 }
            // #endif                
            //             }

            // Job timeline record.
            InitializeTimelineRecord(
                timelineId: message.Timeline.Id,
                timelineRecordId: message.JobId,
                parentTimelineRecordId: null,
                recordType: ExecutionContextType.Job,
                displayName: message.JobDisplayName,
                refName: message.JobName,
                order: null); // The job timeline record's order is set by server.

            // Logger (must be initialized before writing warnings).
            _logger = HostContext.CreateService<IPagingLogger>();
            _logger.Setup(_mainTimelineId, _record.Id);

            // Verbosity (from GitHub.Step_Debug).
            WriteDebug = Variables.Step_Debug ?? false;

            // Hook up JobServerQueueThrottling event, we will log warning on server tarpit.
            _jobServerQueue.JobServerQueueThrottling += JobServerQueueThrottling_EventReceived;
        }

        // Do not add a format string overload. In general, execution context messages are user facing and
        // therefore should be localized. Use the Loc methods from the StringUtil class. The exception to
        // the rule is command messages - which should be crafted using strongly typed wrapper methods.
        public long Write(string tag, string message)
        {
            string msg = HostContext.SecretMasker.MaskSecrets($"{tag}{message}");
            long totalLines;
            lock (_loggerLock)
            {
                totalLines = _logger.TotalLines + 1;
                _logger.Write(msg);
            }

            // write to job level execution context's log file.
            if (_parentExecutionContext != null)
            {
                lock (_parentExecutionContext._loggerLock)
                {
                    _parentExecutionContext._logger.Write(msg);
                }
            }

            // write to plugin daemon, 
            if (_outputForward)
            {
                if (_logPlugin == null)
                {
                    _logPlugin = HostContext.GetService<IRunnerLogPlugin>();
                }

                _logPlugin.Write(_record.Id, msg);
            }

            _jobServerQueue.QueueWebConsoleLine(_record.Id, msg);
            return totalLines;
        }

        public void QueueAttachFile(string type, string name, string filePath)
        {
            ArgUtil.NotNullOrEmpty(type, nameof(type));
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            ArgUtil.NotNullOrEmpty(filePath, nameof(filePath));

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Can't attach (type:{type} name:{name}) file: {filePath}. File does not exist.");
            }

            _jobServerQueue.QueueFileUpload(_mainTimelineId, _record.Id, type, name, filePath, deleteSource: false);
        }

        // Add OnMatcherChanged
        public void Add(OnMatcherChanged handler)
        {
            Root._onMatcherChanged += handler;
        }

        // Remove OnMatcherChanged
        public void Remove(OnMatcherChanged handler)
        {
            Root._onMatcherChanged -= handler;
        }

        // Add Issue matchers
        public void AddMatchers(IssueMatchersConfig config)
        {
            var root = Root;

            // Lock
            lock (root._matchersLock)
            {
                var newMatchers = new List<IssueMatcherConfig>();

                // Prepend
                var newOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var matcher in config.Matchers)
                {
                    newOwners.Add(matcher.Owner);
                    newMatchers.Add(matcher);
                }

                // Add existing non-matching
                var existingMatchers = root._matchers ?? Array.Empty<IssueMatcherConfig>();
                newMatchers.AddRange(existingMatchers.Where(x => !newOwners.Contains(x.Owner)));

                // Store
                root._matchers = newMatchers.ToArray();

                // Fire events
                foreach (var matcher in config.Matchers)
                {
                    root._onMatcherChanged(null, new MatcherChangedEventArgs(matcher));
                }

                // Output
                var owners = config.Matchers.Select(x => $"'{x.Owner}'");
                var joinedOwners = string.Join(", ", owners);
                // todo: loc
                this.Output($"Added matchers: {joinedOwners}");
            }
        }

        // Remove issue matcher
        public void RemoveMatchers(IEnumerable<string> owners)
        {
            var root = Root;
            var distinctOwners = new HashSet<string>(owners, StringComparer.OrdinalIgnoreCase);
            var removedMatchers = new List<IssueMatcherConfig>();
            var newMatchers = new List<IssueMatcherConfig>();

            // Lock
            lock (root._matchersLock)
            {
                // Remove
                var existingMatchers = root._matchers ?? Array.Empty<IssueMatcherConfig>();
                foreach (var matcher in existingMatchers)
                {
                    if (distinctOwners.Contains(matcher.Owner))
                    {
                        removedMatchers.Add(matcher);
                    }
                    else
                    {
                        newMatchers.Add(matcher);
                    }
                }

                // Store
                root._matchers = newMatchers.ToArray();

                // Fire events
                foreach (var removedMatcher in removedMatchers)
                {
                    root._onMatcherChanged(null, new MatcherChangedEventArgs(new IssueMatcherConfig { Owner = removedMatcher.Owner }));
                }

                // Output
                owners = removedMatchers.Select(x => $"'{x.Owner}'");
                var joinedOwners = string.Join(", ", owners);
                // todo: loc
                this.Output($"Removed matchers: {joinedOwners}");
            }
        }

        // Get issue matchers
        public IEnumerable<IssueMatcherConfig> GetMatchers()
        {
            // Lock not required since the list is immutable
            return Root._matchers ?? Array.Empty<IssueMatcherConfig>();
        }

        private void InitializeTimelineRecord(Guid timelineId, Guid timelineRecordId, Guid? parentTimelineRecordId, string recordType, string displayName, string refName, int? order)
        {
            _mainTimelineId = timelineId;
            _record.Id = timelineRecordId;
            _record.RecordType = recordType;
            _record.Name = displayName;
            _record.RefName = refName;
            _record.Order = order;
            _record.PercentComplete = 0;
            _record.State = TimelineRecordState.Pending;
            _record.ErrorCount = 0;
            _record.WarningCount = 0;

            if (parentTimelineRecordId != null && parentTimelineRecordId.Value != Guid.Empty)
            {
                _record.ParentId = parentTimelineRecordId;
            }

            var configuration = HostContext.GetService<IConfigurationStore>();
            _record.WorkerName = configuration.GetSettings().AgentName;

            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
        }

        private void JobServerQueueThrottling_EventReceived(object sender, ThrottlingEventArgs data)
        {
            Interlocked.Add(ref _totalThrottlingDelayInMilliseconds, Convert.ToInt64(data.Delay.TotalMilliseconds));

            if (!_throttlingReported)
            {
                this.Warning(string.Format("The job is currently being throttled by the server. You may experience delays in console line output, job status reporting, and action log uploads."));

                if (!String.IsNullOrEmpty(this.Variables.System_TFCollectionUrl))
                {
                    // Construct a URL to the resource utilization page, to aid the user debug throttling issues
                    UriBuilder uriBuilder = new UriBuilder(Variables.System_TFCollectionUrl);
                    NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
                    DateTime endTime = DateTime.UtcNow;
                    string queryDate = endTime.AddHours(-1).ToString("s") + "," + endTime.ToString("s");

                    uriBuilder.Path += (Variables.System_TFCollectionUrl.EndsWith("/") ? "" : "/") + "_usersSettings/usage";
                    query["tab"] = "pipelines";
                    query["queryDate"] = queryDate;

                    // Global RU link
                    uriBuilder.Query = query.ToString();
                    string global = $"Link to resource utilization page (global 1-hour view): {uriBuilder.ToString()}.";

                    if (!String.IsNullOrEmpty(this.Variables.Build_DefinitionName))
                    {
                        query["keywords"] = this.Variables.Build_Number;
                        query["definition"] = this.Variables.Build_DefinitionName;
                    }

                    // RU link scoped for the build/release
                    uriBuilder.Query = query.ToString();
                    this.Warning($"{global}\nLink to resource utilization page (1-hour view by pipeline): {uriBuilder.ToString()}.");
                }

                _throttlingReported = true;
            }
        }
    }

    // The Error/Warning/etc methods are created as extension methods to simplify unit testing.
    // Otherwise individual overloads would need to be implemented (depending on the unit test).
    public static class ExecutionContextExtension
    {
        public static void Error(this IExecutionContext context, Exception ex)
        {
            context.Error(ex.Message);
            context.Debug(ex.ToString());
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Error(this IExecutionContext context, string message)
        {
            context.AddIssue(new Issue() { Type = IssueType.Error, Message = message });
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Warning(this IExecutionContext context, string message)
        {
            context.AddIssue(new Issue() { Type = IssueType.Warning, Message = message });
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Output(this IExecutionContext context, string message)
        {
            context.Write(null, message);
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Command(this IExecutionContext context, string message)
        {
            context.Write(WellKnownTags.Command, message);
        }

        //
        // Verbose output is enabled by setting ACTIONS_STEP_DEBUG
        // It's meant to help the end user debug their definitions.
        // Why are my inputs not working?  It's not meant for dev debugging which is diag
        //
        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Debug(this IExecutionContext context, string message)
        {
            if (context.WriteDebug)
            {
                var multilines = message?.Replace("\r\n", "\n")?.Split("\n");
                if (multilines != null)
                {
                    foreach (var line in multilines)
                    {
                        context.Write(WellKnownTags.Debug, line);
                    }
                }
            }
        }

        public static ObjectTemplating.ITraceWriter ToTemplateTraceWriter(this IExecutionContext context)
        {
            return new TemplateTraceWriter(context);
        }
    }

    internal sealed class TemplateTraceWriter : ObjectTemplating.ITraceWriter
    {
        private readonly IExecutionContext _executionContext;

        internal TemplateTraceWriter(IExecutionContext executionContext)
        {
            _executionContext = executionContext;
        }

        public void Error(string format, params Object[] args)
        {
            _executionContext.Error(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public void Info(string format, params Object[] args)
        {
            _executionContext.Debug(string.Format(CultureInfo.CurrentCulture, $"{format}", args));
        }

        public void Verbose(string format, params Object[] args)
        {
            // todo: switch to verbose?
            _executionContext.Debug(string.Format(CultureInfo.CurrentCulture, $"{format}", args));
        }
    }

    public static class WellKnownTags
    {
        public static readonly string Section = "##[section]";
        public static readonly string Command = "##[command]";
        public static readonly string Error = "##[error]";
        public static readonly string Warning = "##[warning]";
        public static readonly string Debug = "##[debug]";
    }
}
