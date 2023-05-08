using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Handlers;
using Newtonsoft.Json;
using Sdk.RSWebApi.Contracts;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;
using Pipelines = GitHub.DistributedTask.Pipelines;

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
        Guid EmbeddedId { get; }
        string ScopeName { get; }
        string SiblingScopeName { get; }
        string ContextName { get; }
        ActionRunStage Stage { get; }
        Task ForceCompleted { get; }
        TaskResult? Result { get; set; }
        TaskResult? Outcome { get; set; }
        string ResultCode { get; set; }
        TaskResult? CommandResult { get; set; }
        CancellationToken CancellationToken { get; }
        GlobalContext Global { get; }

        Dictionary<string, string> IntraActionState { get; }
        Dictionary<string, VariableValue> JobOutputs { get; }
        ActionsEnvironmentReference ActionsEnvironment { get; }
        ActionsStepTelemetry StepTelemetry { get; }
        DictionaryContextData ExpressionValues { get; }
        IList<IFunctionInfo> ExpressionFunctions { get; }
        JobContext JobContext { get; }

        // Only job level ExecutionContext has JobSteps
        Queue<IStep> JobSteps { get; }

        // Only job level ExecutionContext has PostJobSteps
        Stack<IStep> PostJobSteps { get; }
        Dictionary<Guid, string> EmbeddedStepsWithPostRegistered { get; }

        // Keep track of embedded steps states
        Dictionary<Guid, Dictionary<string, string>> EmbeddedIntraActionState { get; }

        bool EchoOnActionCommand { get; set; }

        bool IsEmbedded { get; }

        List<string> StepEnvironmentOverrides { get; }

        ExecutionContext Root { get; }

        // Initialize
        void InitializeJob(Pipelines.AgentJobRequestMessage message, CancellationToken token);
        void CancelToken();
        IExecutionContext CreateChild(Guid recordId, string displayName, string refName, string scopeName, string contextName, ActionRunStage stage, Dictionary<string, string> intraActionState = null, int? recordOrder = null, IPagingLogger logger = null, bool isEmbedded = false, CancellationTokenSource cancellationTokenSource = null, Guid embeddedId = default(Guid), string siblingScopeName = null);
        IExecutionContext CreateEmbeddedChild(string scopeName, string contextName, Guid embeddedId, ActionRunStage stage, Dictionary<string, string> intraActionState = null, string siblingScopeName = null);

        // logging
        long Write(string tag, string message);
        void QueueAttachFile(string type, string name, string filePath);
        void QueueSummaryFile(string name, string filePath, Guid stepRecordId);

        // timeline record update methods
        void Start(string currentOperation = null);
        TaskResult Complete(TaskResult? result = null, string currentOperation = null, string resultCode = null);
        void SetEnvContext(string name, string value);
        void SetRunnerContext(string name, string value);
        string GetGitHubContext(string name);
        void SetGitHubContext(string name, string value);
        void SetOutput(string name, string value, out string reference);
        void SetTimeout(TimeSpan? timeout);
        void AddIssue(Issue issue, string message = null);
        void Progress(int percentage, string currentOperation = null);
        void UpdateDetailTimelineRecord(TimelineRecord record);

        void UpdateTimelineRecordDisplayName(string displayName);

        // matchers
        void Add(OnMatcherChanged handler);
        void Remove(OnMatcherChanged handler);
        void AddMatchers(IssueMatchersConfig matcher);
        void RemoveMatchers(IEnumerable<string> owners);
        IEnumerable<IssueMatcherConfig> GetMatchers();

        // others
        void ForceTaskComplete();
        void RegisterPostJobStep(IStep step);
        void PublishStepTelemetry();

        void ApplyContinueOnError(TemplateToken continueOnError);
        void UpdateGlobalStepsContext();

        void WriteWebhookPayload();
    }

    public sealed class ExecutionContext : RunnerService, IExecutionContext
    {
        private const int _maxIssueCount = 100;
        private const int _throttlingDelayReportThreshold = 10 * 1000; // Don't report throttling with less than 10 seconds delay
        private const int _maxIssueMessageLength = 4096; // Don't send issue with huge message since we can't forward them from actions to check annotation.
        private const int _maxIssueCountInTelemetry = 3; // Only send the first 3 issues to telemetry
        private const int _maxIssueMessageLengthInTelemetry = 256; // Only send the first 256 characters of issue message to telemetry

        private readonly TimelineRecord _record = new();
        private readonly Dictionary<Guid, TimelineRecord> _detailRecords = new();
        private readonly object _loggerLock = new();
        private readonly object _matchersLock = new();

        private event OnMatcherChanged _onMatcherChanged;

        private IssueMatcherConfig[] _matchers;

        private IPagingLogger _logger;
        private IJobServerQueue _jobServerQueue;
        private ExecutionContext _parentExecutionContext;

        private Guid _mainTimelineId;
        private Guid _detailTimelineId;
        private bool _expandedForPostJob = false;
        private int _childTimelineRecordOrder = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private TaskCompletionSource<int> _forceCompleted = new();
        private bool _throttlingReported = false;

        // only job level ExecutionContext will track throttling delay.
        private long _totalThrottlingDelayInMilliseconds = 0;
        private bool _stepTelemetryPublished = false;

        public Guid Id => _record.Id;
        public Guid EmbeddedId { get; private set; }
        public string ScopeName { get; private set; }
        public string SiblingScopeName { get; private set; }
        public string ContextName { get; private set; }
        public ActionRunStage Stage { get; private set; }
        public Task ForceCompleted => _forceCompleted.Task;
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        public Dictionary<string, string> IntraActionState { get; private set; }
        public Dictionary<string, VariableValue> JobOutputs { get; private set; }

        public ActionsEnvironmentReference ActionsEnvironment { get; private set; }
        public ActionsStepTelemetry StepTelemetry { get; } = new ActionsStepTelemetry();
        public DictionaryContextData ExpressionValues { get; } = new DictionaryContextData();
        public IList<IFunctionInfo> ExpressionFunctions { get; } = new List<IFunctionInfo>();

        // Shared pointer across job-level execution context and step-level execution contexts
        public GlobalContext Global { get; private set; }

        // Only job level ExecutionContext has JobSteps
        public Queue<IStep> JobSteps { get; private set; }

        // Only job level ExecutionContext has PostJobSteps
        public Stack<IStep> PostJobSteps { get; private set; }

        // Only job level ExecutionContext has StepsWithPostRegistered
        public HashSet<Guid> StepsWithPostRegistered { get; private set; }

        // Only job level ExecutionContext has EmbeddedStepsWithPostRegistered
        public Dictionary<Guid, string> EmbeddedStepsWithPostRegistered { get; private set; }

        public Dictionary<Guid, Dictionary<string, string>> EmbeddedIntraActionState { get; private set; }

        public bool EchoOnActionCommand { get; set; }

        // An embedded execution context shares the same record ID, record name, and logger
        // as its enclosing execution context.
        public bool IsEmbedded { get; private set; }

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

        public TaskResult? Outcome { get; set; }

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

        public ExecutionContext Root
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

        public List<string> StepEnvironmentOverrides { get; } = new List<string>();

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

        public void RegisterPostJobStep(IStep step)
        {
            string siblingScopeName = null;
            if (this.IsEmbedded)
            {
                if (step is IActionRunner actionRunner)
                {
                    if (Root.EmbeddedStepsWithPostRegistered.ContainsKey(actionRunner.Action.Id))
                    {
                        Trace.Info($"'post' of '{actionRunner.DisplayName}' already push to child post step stack.");
                    }
                    else
                    {
                        Root.EmbeddedStepsWithPostRegistered[actionRunner.Action.Id] = actionRunner.Condition;
                    }
                    return;
                }
            }
            else if (step is IActionRunner actionRunner && !Root.StepsWithPostRegistered.Add(actionRunner.Action.Id))
            {
                Trace.Info($"'post' of '{actionRunner.DisplayName}' already push to post step stack.");
                return;
            }
            if (step is IActionRunner runner)
            {
                siblingScopeName = runner.Action.ContextName;
            }

            step.ExecutionContext = Root.CreatePostChild(step.DisplayName, IntraActionState, siblingScopeName);
            Root.PostJobSteps.Push(step);
        }

        public IExecutionContext CreateChild(
            Guid recordId,
            string displayName,
            string refName,
            string scopeName,
            string contextName,
            ActionRunStage stage,
            Dictionary<string, string> intraActionState = null,
            int? recordOrder = null,
            IPagingLogger logger = null,
            bool isEmbedded = false,
            CancellationTokenSource cancellationTokenSource = null,
            Guid embeddedId = default(Guid),
            string siblingScopeName = null)
        {
            Trace.Entering();

            var child = new ExecutionContext();
            child.Initialize(HostContext);
            child.Global = Global;
            child.ScopeName = scopeName;
            child.ContextName = contextName;
            child.Stage = stage;
            child.EmbeddedId = embeddedId;
            child.SiblingScopeName = siblingScopeName;
            if (intraActionState == null)
            {
                child.IntraActionState = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                child.IntraActionState = intraActionState;
            }
            foreach (var pair in ExpressionValues)
            {
                child.ExpressionValues[pair.Key] = pair.Value;
            }
            foreach (var item in ExpressionFunctions)
            {
                child.ExpressionFunctions.Add(item);
            }
            child._cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
            child._parentExecutionContext = this;
            child.EchoOnActionCommand = EchoOnActionCommand;

            if (recordOrder != null)
            {
                child.InitializeTimelineRecord(_mainTimelineId, recordId, _record.Id, ExecutionContextType.Task, displayName, refName, recordOrder);
            }
            else
            {
                child.InitializeTimelineRecord(_mainTimelineId, recordId, _record.Id, ExecutionContextType.Task, displayName, refName, ++_childTimelineRecordOrder);
            }
            if (logger != null)
            {
                child._logger = logger;
            }
            else
            {
                child._logger = HostContext.CreateService<IPagingLogger>();
                child._logger.Setup(_mainTimelineId, recordId);
            }

            child.IsEmbedded = isEmbedded;
            child.StepTelemetry.StepId = recordId;
            child.StepTelemetry.Stage = stage.ToString();
            child.StepTelemetry.IsEmbedded = isEmbedded;
            child.StepTelemetry.StepContextName = child.GetFullyQualifiedContextName(); ;

            return child;
        }

        /// <summary>
        /// An embedded execution context shares the same record ID, record name, logger,
        /// and a linked cancellation token.
        /// </summary>
        public IExecutionContext CreateEmbeddedChild(
            string scopeName,
            string contextName,
            Guid embeddedId,
            ActionRunStage stage,
            Dictionary<string, string> intraActionState = null,
            string siblingScopeName = null)
        {
            return Root.CreateChild(_record.Id, _record.Name, _record.Id.ToString("N"), scopeName, contextName, stage, logger: _logger, isEmbedded: true, cancellationTokenSource: CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token), intraActionState: intraActionState, embeddedId: embeddedId, siblingScopeName: siblingScopeName);
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
            if (_totalThrottlingDelayInMilliseconds > _throttlingDelayReportThreshold)
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

            PublishStepTelemetry();

            var stepResult = new StepResult
            {
                ExternalID = _record.Id,
                Conclusion = _record.Result ?? TaskResult.Succeeded,
                Status = _record.State,
                Number = _record.Order,
                Name = _record.Name,
                StartedAt = _record.StartTime,
                CompletedAt = _record.FinishTime,
                Annotations = new List<Annotation>()
            };

            _record.Issues?.ForEach(issue =>
            {
                var annotation = issue.ToAnnotation();
                if (annotation != null)
                {
                    stepResult.Annotations.Add(annotation.Value);
                }
            });

            Global.StepsResult.Add(stepResult);

            if (Root != this)
            {
                // only dispose TokenSource for step level ExecutionContext
                _cancellationTokenSource?.Dispose();
            }

            _logger.End();

            UpdateGlobalStepsContext();

            return Result.Value;
        }

        public void UpdateGlobalStepsContext()
        {
            // Skip if generated context name. Generated context names start with "__". After 3.2 the server will never send an empty context name.
            if (!string.IsNullOrEmpty(ContextName) && !ContextName.StartsWith("__", StringComparison.Ordinal))
            {
                Global.StepsContext.SetOutcome(ScopeName, ContextName, (Outcome ?? Result ?? TaskResult.Succeeded).ToActionResult());
                Global.StepsContext.SetConclusion(ScopeName, ContextName, (Result ?? TaskResult.Succeeded).ToActionResult());
            }
        }

        public void SetRunnerContext(string name, string value)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            var runnerContext = ExpressionValues["runner"] as RunnerContext;
            runnerContext[name] = new StringContextData(value);
        }

        public void SetEnvContext(string name, string value)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));

#if OS_WINDOWS
            var envContext = ExpressionValues["env"] as DictionaryContextData;
            envContext[name] = new StringContextData(value);
#else
            var envContext = ExpressionValues["env"] as CaseSensitiveDictionaryContextData;
            envContext[name] = new StringContextData(value);
#endif

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

            // Skip if generated context name. Generated context names start with "__". After 3.2 the server will never send an empty context name.
            if (string.IsNullOrEmpty(ContextName) || ContextName.StartsWith("__", StringComparison.Ordinal))
            {
                reference = null;
                return;
            }

            // todo: restrict multiline?

            Global.StepsContext.SetOutput(ScopeName, ContextName, name, value, out reference);
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
            if (issue.Message.Length > _maxIssueMessageLength)
            {
                issue.Message = issue.Message[.._maxIssueMessageLength];
            }

            // Tracking the line number (logFileLineNumber) and step number (stepNumber) for each issue that gets created
            // Actions UI from the run summary page use both values to easily link to an exact locations in logs where annotations originate from
            if (_record.Order != null)
            {
                issue.Data["stepNumber"] = _record.Order.ToString();
            }

            if (issue.Type == IssueType.Error)
            {
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
            else if (issue.Type == IssueType.Notice)
            {
                if (!string.IsNullOrEmpty(logMessage))
                {
                    long logLineNumber = Write(WellKnownTags.Notice, logMessage);
                    issue.Data["logFileLineNumber"] = logLineNumber.ToString();
                }

                if (_record.NoticeCount < _maxIssueCount)
                {
                    _record.Issues.Add(issue);
                }

                _record.NoticeCount++;
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

        public void UpdateTimelineRecordDisplayName(string displayName)
        {
            ArgUtil.NotNull(displayName, nameof(displayName));
            _record.Name = displayName;
            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
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

            Global = new GlobalContext();

            // Plan
            Global.Plan = message.Plan;
            Global.Features = PlanUtil.GetFeatures(message.Plan);

            // Endpoints
            Global.Endpoints = message.Resources.Endpoints;

            // Ser debug using vars context if debug variables are not already present.
            var variables = message.Variables;
            SetDebugUsingVars(variables, message.ContextData);

            Global.Variables = new Variables(HostContext, variables);

            if (Global.Variables.GetBoolean("DistributedTask.ForceInternalNodeVersionOnRunnerTo12") ?? false)
            {
                Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, "node12");
            }

            // Environment variables shared across all actions
            Global.EnvironmentVariables = new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer);

            // Job defaults shared across all actions
            Global.JobDefaults = new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            // Job Telemetry
            Global.JobTelemetry = new List<JobTelemetry>();

            // ActionsStepTelemetry for entire job
            Global.StepsTelemetry = new List<ActionsStepTelemetry>();

            // Steps results for entire job
            Global.StepsResult = new List<StepResult>();

            // Job level annotations
            Global.JobAnnotations = new List<Annotation>();

            // Job Outputs
            JobOutputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);

            // Actions environment
            ActionsEnvironment = message.ActionsEnvironment;


            // Service container info
            Global.ServiceContainers = new List<ContainerInfo>();

            // Steps context (StepsRunner manages adding the scoped steps context)
            Global.StepsContext = new StepsContext();

            // File table
            Global.FileTable = new List<String>(message.FileTable ?? new string[0]);

            // What type of job request is running (i.e. Run Service vs. pipelines)
            Global.Variables.Set(Constants.Variables.System.JobRequestType, message.MessageType);

            // Expression values
            if (message.ContextData?.Count > 0)
            {
                foreach (var pair in message.ContextData)
                {
                    ExpressionValues[pair.Key] = pair.Value;
                }
            }

            ExpressionValues["secrets"] = Global.Variables.ToSecretsContext();
            ExpressionValues["runner"] = new RunnerContext();
            ExpressionValues["job"] = new JobContext();

            Trace.Info("Initialize GitHub context");
            var githubAccessToken = new StringContextData(Global.Variables.Get("system.github.token"));
            var base64EncodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"x-access-token:{githubAccessToken}"));
            HostContext.SecretMasker.AddValue(base64EncodedToken);
            var githubJob = Global.Variables.Get("system.github.job");
            var githubContext = new GitHubContext();
            githubContext["token"] = githubAccessToken;
            if (!string.IsNullOrEmpty(githubJob))
            {
                githubContext["job"] = new StringContextData(githubJob);
            }
            var githubDictionary = ExpressionValues["github"].AssertDictionary("github");
            foreach (var pair in githubDictionary)
            {
                githubContext[pair.Key] = pair.Value;
            }
            ExpressionValues["github"] = githubContext;

            Trace.Info("Initialize Env context");
#if OS_WINDOWS
            ExpressionValues["env"] = new DictionaryContextData();
#else

            ExpressionValues["env"] = new CaseSensitiveDictionaryContextData();
#endif

            // Prepend Path
            Global.PrependPath = new List<string>();

            // JobSteps for job ExecutionContext
            JobSteps = new Queue<IStep>();

            // PostJobSteps for job ExecutionContext
            PostJobSteps = new Stack<IStep>();

            // StepsWithPostRegistered for job ExecutionContext
            StepsWithPostRegistered = new HashSet<Guid>();

            // EmbeddedStepsWithPostRegistered for job ExecutionContext
            EmbeddedStepsWithPostRegistered = new Dictionary<Guid, string>();

            // EmbeddedIntraActionState for job ExecutionContext
            EmbeddedIntraActionState = new Dictionary<Guid, Dictionary<string, string>>();

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

            // Initialize 'echo on action command success' property, default to false, unless Step_Debug is set
            EchoOnActionCommand = Global.Variables.Step_Debug ?? false;

            // Verbosity (from GitHub.Step_Debug).
            Global.WriteDebug = Global.Variables.Step_Debug ?? false;

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

            _jobServerQueue.QueueWebConsoleLine(_record.Id, msg, totalLines);
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

        public void QueueSummaryFile(string name, string filePath, Guid stepRecordId)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            ArgUtil.NotNullOrEmpty(filePath, nameof(filePath));

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Can't upload (name:{name}) file: {filePath}. File does not exist.");
            }
            _jobServerQueue.QueueResultsUpload(stepRecordId, name, filePath, ChecksAttachmentType.StepSummary, deleteSource: false, finalize: true, firstBlock: true, totalLines: 0);
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
                this.Debug($"Added matchers: {joinedOwners}. Problem matchers scan action output for known warning or error strings and report these inline.");
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
                this.Debug($"Removed matchers: {joinedOwners}");
            }
        }

        // Get issue matchers
        public IEnumerable<IssueMatcherConfig> GetMatchers()
        {
            // Lock not required since the list is immutable
            return Root._matchers ?? Array.Empty<IssueMatcherConfig>();
        }

        public void PublishStepTelemetry()
        {
            if (!_stepTelemetryPublished)
            {
                // Add to the global steps telemetry only if we have something to log.
                if (!string.IsNullOrEmpty(StepTelemetry?.Type))
                {
                    if (!IsEmbedded)
                    {
                        StepTelemetry.Result = _record.Result;
                    }

                    if (!IsEmbedded &&
                        _record.FinishTime != null &&
                        _record.StartTime != null)
                    {
                        StepTelemetry.ExecutionTimeInSeconds = (int)Math.Ceiling((_record.FinishTime - _record.StartTime)?.TotalSeconds ?? 0);
                        StepTelemetry.StartTime = _record.StartTime;
                        StepTelemetry.FinishTime = _record.FinishTime;
                    }

                    if (!IsEmbedded &&
                        _record.Issues.Count > 0)
                    {
                        foreach (var issue in _record.Issues)
                        {
                            if ((issue.Type == IssueType.Error || issue.Type == IssueType.Warning) &&
                                !string.IsNullOrEmpty(issue.Message))
                            {
                                string issueTelemetry;
                                if (issue.Message.Length > _maxIssueMessageLengthInTelemetry)
                                {
                                    issueTelemetry = $"{issue.Message[.._maxIssueMessageLengthInTelemetry]}";
                                }
                                else
                                {
                                    issueTelemetry = issue.Message;
                                }

                                StepTelemetry.ErrorMessages.Add(issueTelemetry);

                                // Only send over the first 3 issues to avoid sending too much data.
                                if (StepTelemetry.ErrorMessages.Count >= _maxIssueCountInTelemetry)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    Trace.Info($"Publish step telemetry for current step {StringUtil.ConvertToJson(StepTelemetry)}.");
                    Global.StepsTelemetry.Add(StepTelemetry);
                    _stepTelemetryPublished = true;
                }
            }
            else
            {
                Trace.Info($"Step telemetry has already been published.");
            }
        }

        public void WriteWebhookPayload()
        {
            // Makes directory for event_path data
            var tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);
            var workflowDirectory = Path.Combine(tempDirectory, "_github_workflow");
            Directory.CreateDirectory(workflowDirectory);
            var gitHubEvent = GetGitHubContext("event");

            // adds the GitHub event path/file if the event exists
            if (gitHubEvent != null)
            {
                var workflowFile = Path.Combine(workflowDirectory, "event.json");
                Trace.Info($"Write event payload to {workflowFile}");
                File.WriteAllText(workflowFile, gitHubEvent, new UTF8Encoding(false));
                SetGitHubContext("event_path", workflowFile);
            }
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
            _record.NoticeCount = 0;

            if (parentTimelineRecordId != null && parentTimelineRecordId.Value != Guid.Empty)
            {
                _record.ParentId = parentTimelineRecordId;
            }
            else if (parentTimelineRecordId == null)
            {
                _record.AgentPlatform = VarUtil.OS;
            }

            var configuration = HostContext.GetService<IConfigurationStore>();
            _record.WorkerName = configuration.GetSettings().AgentName;

            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
        }

        private void JobServerQueueThrottling_EventReceived(object sender, ThrottlingEventArgs data)
        {
            Interlocked.Add(ref _totalThrottlingDelayInMilliseconds, Convert.ToInt64(data.Delay.TotalMilliseconds));

            if (!_throttlingReported &&
                _totalThrottlingDelayInMilliseconds > _throttlingDelayReportThreshold)
            {
                this.Warning(string.Format("The job is currently being throttled by the server. You may experience delays in console line output, job status reporting, and action log uploads."));

                _throttlingReported = true;
            }
        }

        private IExecutionContext CreatePostChild(string displayName, Dictionary<string, string> intraActionState, string siblingScopeName = null)
        {
            if (!_expandedForPostJob)
            {
                Trace.Info($"Reserve record order {_childTimelineRecordOrder + 1} to {_childTimelineRecordOrder * 2} for post job actions.");
                _expandedForPostJob = true;
                _childTimelineRecordOrder = _childTimelineRecordOrder * 2;
            }

            var newGuid = Guid.NewGuid();
            return CreateChild(newGuid, displayName, newGuid.ToString("N"), null, null, ActionRunStage.Post, intraActionState, _childTimelineRecordOrder - Root.PostJobSteps.Count, siblingScopeName: siblingScopeName);
        }

        // Sets debug using vars context in case debug variables are not present.
        private static void SetDebugUsingVars(IDictionary<string, VariableValue> variables, IDictionary<string, PipelineContextData> contextData)
        {
            if (contextData != null &&
                contextData.TryGetValue(PipelineTemplateConstants.Vars, out var varsPipelineContextData) &&
                varsPipelineContextData != null &&
                varsPipelineContextData is DictionaryContextData varsContextData)
            {
                // Set debug variables only when StepDebug/RunnerDebug variables are not present.
                if (!variables.ContainsKey(Constants.Variables.Actions.StepDebug) &&
                    varsContextData.TryGetValue(Constants.Variables.Actions.StepDebug, out var stepDebugValue) &&
                    stepDebugValue is StringContextData)
                {
                    variables[Constants.Variables.Actions.StepDebug] = stepDebugValue.ToString();
                }

                if (!variables.ContainsKey(Constants.Variables.Actions.RunnerDebug) &&
                    varsContextData.TryGetValue(Constants.Variables.Actions.RunnerDebug, out var runDebugValue) &&
                    runDebugValue is StringContextData)
                {
                    variables[Constants.Variables.Actions.RunnerDebug] = runDebugValue.ToString();
                }
            }
        }

        public void ApplyContinueOnError(TemplateToken continueOnErrorToken)
        {
            if (Result != TaskResult.Failed)
            {
                return;
            }
            var continueOnError = false;
            try
            {
                var templateEvaluator = this.ToPipelineTemplateEvaluator();
                continueOnError = templateEvaluator.EvaluateStepContinueOnError(continueOnErrorToken, ExpressionValues, ExpressionFunctions);
            }
            catch (Exception ex)
            {
                Trace.Info("The step failed and an error occurred when attempting to determine whether to continue on error.");
                Trace.Error(ex);
                this.Error("The step failed and an error occurred when attempting to determine whether to continue on error.");
                this.Error(ex);
            }

            if (continueOnError)
            {
                Outcome = Result;
                Result = TaskResult.Succeeded;
                Trace.Info($"Updated step result (continue on error)");
            }

            UpdateGlobalStepsContext();
        }
    }

    // The Error/Warning/etc methods are created as extension methods to simplify unit testing.
    // Otherwise individual overloads would need to be implemented (depending on the unit test).
    public static class ExecutionContextExtension
    {
        public static string GetFullyQualifiedContextName(this IExecutionContext context)
        {
            if (!string.IsNullOrEmpty(context.ScopeName))
            {
                return $"{context.ScopeName}.{context.ContextName}";
            }

            return context.ContextName;
        }

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
        public static void InfrastructureError(this IExecutionContext context, string message)
        {
            context.AddIssue(new Issue() { Type = IssueType.Error, Message = message, IsInfrastructureIssue = true });
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
            if (context.Global.WriteDebug)
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

        public static IEnumerable<KeyValuePair<string, object>> ToExpressionState(this IExecutionContext context)
        {
            return new[] { new KeyValuePair<string, object>(nameof(IExecutionContext), context) };
        }

        public static PipelineTemplateEvaluator ToPipelineTemplateEvaluator(this IExecutionContext context, ObjectTemplating.ITraceWriter traceWriter = null)
        {
            if (traceWriter == null)
            {
                traceWriter = context.ToTemplateTraceWriter();
            }
            var schema = PipelineTemplateSchemaFactory.GetSchema();
            return new PipelineTemplateEvaluator(traceWriter, schema, context.Global.FileTable)
            {
                MaxErrorMessageLength = int.MaxValue, // Don't truncate error messages otherwise we might not scrub secrets correctly
            };
        }

        public static ObjectTemplating.ITraceWriter ToTemplateTraceWriter(this IExecutionContext context)
        {
            return new TemplateTraceWriter(context);
        }

        public static DictionaryContextData GetExpressionValues(this IExecutionContext context, IStepHost stepHost)
        {
            if (stepHost is ContainerStepHost)
            {

                var expressionValues = context.ExpressionValues.Clone() as DictionaryContextData;
                context.UpdatePathsInExpressionValues("github", expressionValues, stepHost);
                context.UpdatePathsInExpressionValues("runner", expressionValues, stepHost);
                return expressionValues;
            }
            else
            {
                return context.ExpressionValues.Clone() as DictionaryContextData;
            }
        }

        private static void UpdatePathsInExpressionValues(this IExecutionContext context, string contextName, DictionaryContextData expressionValues, IStepHost stepHost)
        {
            var dict = expressionValues[contextName].AssertDictionary($"expected context {contextName} to be a dictionary");
            context.ResolvePathsInExpressionValuesDictionary(dict, stepHost);
            expressionValues[contextName] = dict;
        }

        private static void ResolvePathsInExpressionValuesDictionary(this IExecutionContext context, DictionaryContextData dict, IStepHost stepHost)
        {
            foreach (var key in dict.Keys.ToList())
            {
                if (dict[key] is StringContextData)
                {
                    var value = dict[key].ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        dict[key] = new StringContextData(stepHost.ResolvePathForStepHost(context, value));
                    }
                }
                else if (dict[key] is DictionaryContextData)
                {
                    var innerDict = dict[key].AssertDictionary("expected dictionary");
                    context.ResolvePathsInExpressionValuesDictionary(innerDict, stepHost);
                    var updatedDict = new DictionaryContextData();
                    foreach (var k in innerDict.Keys.ToList())
                    {
                        updatedDict[k] = innerDict[k];
                    }
                    dict[key] = updatedDict;
                }
                else if (dict[key] is CaseSensitiveDictionaryContextData)
                {
                    var innerDict = dict[key].AssertDictionary("expected dictionary");
                    context.ResolvePathsInExpressionValuesDictionary(innerDict, stepHost);
                    var updatedDict = new CaseSensitiveDictionaryContextData();
                    foreach (var k in innerDict.Keys.ToList())
                    {
                        updatedDict[k] = innerDict[k];
                    }
                    dict[key] = updatedDict;
                }
            }
        }
    }

    internal sealed class TemplateTraceWriter : ObjectTemplating.ITraceWriter
    {
        private readonly IExecutionContext _executionContext;

        internal TemplateTraceWriter(IExecutionContext executionContext)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
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
        public static readonly string Notice = "##[notice]";
        public static readonly string Debug = "##[debug]";
    }
}
