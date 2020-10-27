using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Container;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
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
        string ScopeName { get; }
        string ContextName { get; }
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
        DictionaryContextData ExpressionValues { get; }
        IList<IFunctionInfo> ExpressionFunctions { get; }
        JobContext JobContext { get; }

        // Only job level ExecutionContext has JobSteps
        Queue<IStep> JobSteps { get; }

        // Only job level ExecutionContext has PostJobSteps
        Stack<IStep> PostJobSteps { get; }

        bool EchoOnActionCommand { get; set; }

        bool InsideComposite { get; }

        ExecutionContext Root { get; }

        // Initialize
        void InitializeJob(Pipelines.AgentJobRequestMessage message, CancellationToken token);
        void CancelToken();
        IExecutionContext CreateChild(Guid recordId, string displayName, string refName, string scopeName, string contextName, Dictionary<string, string> intraActionState = null, int? recordOrder = null, IPagingLogger logger = null, bool insideComposite = false, CancellationTokenSource cancellationTokenSource = null);

        // logging
        long Write(string tag, string message);
        void QueueAttachFile(string type, string name, string filePath);

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
        IStep CreateCompositeStep(string scopeName, IActionRunner step, DictionaryContextData inputsData, Dictionary<string, string> envData);
    }

    public sealed class ExecutionContext : RunnerService, IExecutionContext
    {
        private const int _maxIssueCount = 10;
        private const int _throttlingDelayReportThreshold = 10 * 1000; // Don't report throttling with less than 10 seconds delay

        private readonly TimelineRecord _record = new TimelineRecord();
        private readonly Dictionary<Guid, TimelineRecord> _detailRecords = new Dictionary<Guid, TimelineRecord>();
        private readonly object _loggerLock = new object();
        private readonly object _matchersLock = new object();

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
        private TaskCompletionSource<int> _forceCompleted = new TaskCompletionSource<int>();
        private bool _throttlingReported = false;

        // only job level ExecutionContext will track throttling delay.
        private long _totalThrottlingDelayInMilliseconds = 0;

        public Guid Id => _record.Id;
        public string ScopeName { get; private set; }
        public string ContextName { get; private set; }
        public Task ForceCompleted => _forceCompleted.Task;
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        public Dictionary<string, string> IntraActionState { get; private set; }
        public Dictionary<string, VariableValue> JobOutputs { get; private set; }

        public ActionsEnvironmentReference ActionsEnvironment { get; private set; }
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

        public bool EchoOnActionCommand { get; set; }

        public bool InsideComposite { get; private set; }

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
            if (step is IActionRunner actionRunner && !Root.StepsWithPostRegistered.Add(actionRunner.Action.Id))
            {
                Trace.Info($"'post' of '{actionRunner.DisplayName}' already push to post step stack.");
                return;
            }

            step.ExecutionContext = Root.CreatePostChild(step.DisplayName, IntraActionState);
            Root.PostJobSteps.Push(step);
        }

        /// <summary>
        /// Helper function used in CompositeActionHandler::RunAsync to
        /// add a child node, aka a step, to the current job to the Root.JobSteps based on the location.
        /// </summary>
        public IStep CreateCompositeStep(
            string scopeName,
            IActionRunner step,
            DictionaryContextData inputsData,
            Dictionary<string, string> envData)
        {
            step.ExecutionContext = Root.CreateChild(_record.Id, _record.Name, _record.Id.ToString("N"), scopeName, step.Action.ContextName, logger: _logger, insideComposite: true, cancellationTokenSource: CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token));
            step.ExecutionContext.ExpressionValues["inputs"] = inputsData;
            step.ExecutionContext.ExpressionValues["steps"] = Global.StepsContext.GetScope(step.ExecutionContext.GetFullyQualifiedContextName());

            // Add the composite action environment variables to each step.
#if OS_WINDOWS
            var envContext = new DictionaryContextData();
#else
            var envContext = new CaseSensitiveDictionaryContextData();
#endif
            foreach (var pair in envData)
            {
                envContext[pair.Key] = new StringContextData(pair.Value ?? string.Empty);
            }
            step.ExecutionContext.ExpressionValues["env"] = envContext;

            return step;
        }

        public IExecutionContext CreateChild(Guid recordId, string displayName, string refName, string scopeName, string contextName, Dictionary<string, string> intraActionState = null, int? recordOrder = null, IPagingLogger logger = null, bool insideComposite = false, CancellationTokenSource cancellationTokenSource = null)
        {
            Trace.Entering();

            var child = new ExecutionContext();
            child.Initialize(HostContext);
            child.Global = Global;
            child.ScopeName = scopeName;
            child.ContextName = contextName;
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

            child.InsideComposite = insideComposite;

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

            if (Root != this)
            {
                // only dispose TokenSource for step level ExecutionContext
                _cancellationTokenSource?.Dispose();
            }

            _logger.End();

            // Skip if generated context name. Generated context names start with "__". After M271-ish the server will never send an empty context name.
            if (!string.IsNullOrEmpty(ContextName) && !ContextName.StartsWith("__", StringComparison.Ordinal))
            {
                Global.StepsContext.SetOutcome(ScopeName, ContextName, (Outcome ?? Result ?? TaskResult.Succeeded).ToActionResult());
                Global.StepsContext.SetConclusion(ScopeName, ContextName, (Result ?? TaskResult.Succeeded).ToActionResult());
            }

            return Result.Value;
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

            // Skip if generated context name. Generated context names start with "__". After M271-ish the server will never send an empty context name.
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

            // Variables
            Global.Variables = new Variables(HostContext, message.Variables);

            // Environment variables shared across all actions
            Global.EnvironmentVariables = new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer);

            // Job defaults shared across all actions
            Global.JobDefaults = new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

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

            if (!_throttlingReported &&
                _totalThrottlingDelayInMilliseconds > _throttlingDelayReportThreshold)
            {
                this.Warning(string.Format("The job is currently being throttled by the server. You may experience delays in console line output, job status reporting, and action log uploads."));

                _throttlingReported = true;
            }
        }

        private IExecutionContext CreatePostChild(string displayName, Dictionary<string, string> intraActionState)
        {
            if (!_expandedForPostJob)
            {
                Trace.Info($"Reserve record order {_childTimelineRecordOrder + 1} to {_childTimelineRecordOrder * 2} for post job actions.");
                _expandedForPostJob = true;
                _childTimelineRecordOrder = _childTimelineRecordOrder * 2;
            }

            var newGuid = Guid.NewGuid();
            return CreateChild(newGuid, displayName, newGuid.ToString("N"), null, null, intraActionState, _childTimelineRecordOrder - Root.PostJobSteps.Count);
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
            return new PipelineTemplateEvaluator(traceWriter, schema, context.Global.FileTable);
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
        public static readonly string Debug = "##[debug]";
    }
}
