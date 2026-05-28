using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Runner.Worker.Dap
{
    public enum DapCommand
    {
        Continue,
        Next,
        StepIn,
        StepOut,
        Disconnect
    }

    /// <summary>
    /// Base class of requests, responses, and events per DAP specification.
    /// </summary>
    public class ProtocolMessage
    {
        /// <summary>
        /// Sequence number of the message (also known as message ID).
        /// The seq for the first message sent by a client or debug adapter is 1,
        /// and for each subsequent message is 1 greater than the previous message.
        /// </summary>
        [JsonProperty("seq")]
        public int Seq { get; set; }

        /// <summary>
        /// Message type: 'request', 'response', 'event'
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    /// <summary>
    /// A client or debug adapter initiated request.
    /// </summary>
    public class Request : ProtocolMessage
    {
        /// <summary>
        /// The command to execute.
        /// </summary>
        [JsonProperty("command")]
        public string Command { get; set; }

        /// <summary>
        /// Object containing arguments for the command.
        /// Using JObject for flexibility with different argument types.
        /// </summary>
        [JsonProperty("arguments")]
        public JObject Arguments { get; set; }
    }

    /// <summary>
    /// Response for a request.
    /// </summary>
    public class Response : ProtocolMessage
    {
        /// <summary>
        /// Sequence number of the corresponding request.
        /// </summary>
        [JsonProperty("request_seq")]
        public int RequestSeq { get; set; }

        /// <summary>
        /// Outcome of the request. If true, the request was successful.
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// The command requested.
        /// </summary>
        [JsonProperty("command")]
        public string Command { get; set; }

        /// <summary>
        /// Contains the raw error in short form if success is false.
        /// </summary>
        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        /// <summary>
        /// Contains request result if success is true and error details if success is false.
        /// </summary>
        [JsonProperty("body", NullValueHandling = NullValueHandling.Ignore)]
        public object Body { get; set; }
    }

    /// <summary>
    /// A debug adapter initiated event.
    /// </summary>
    public class Event : ProtocolMessage
    {
        public Event()
        {
            Type = "event";
        }

        /// <summary>
        /// Type of event.
        /// </summary>
        [JsonProperty("event")]
        public string EventType { get; set; }

        /// <summary>
        /// Event-specific information.
        /// </summary>
        [JsonProperty("body", NullValueHandling = NullValueHandling.Ignore)]
        public object Body { get; set; }
    }

    #region Initialize Request/Response

    /// <summary>
    /// Arguments for 'initialize' request.
    /// </summary>
    public class InitializeRequestArguments
    {
        /// <summary>
        /// The ID of the client using this adapter.
        /// </summary>
        [JsonProperty("clientID")]
        public string ClientId { get; set; }

        /// <summary>
        /// The human-readable name of the client using this adapter.
        /// </summary>
        [JsonProperty("clientName")]
        public string ClientName { get; set; }

        /// <summary>
        /// The ID of the debug adapter.
        /// </summary>
        [JsonProperty("adapterID")]
        public string AdapterId { get; set; }

        /// <summary>
        /// The ISO-639 locale of the client using this adapter, e.g. en-US or de-CH.
        /// </summary>
        [JsonProperty("locale")]
        public string Locale { get; set; }

        /// <summary>
        /// If true all line numbers are 1-based (default).
        /// </summary>
        [JsonProperty("linesStartAt1")]
        public bool LinesStartAt1 { get; set; } = true;

        /// <summary>
        /// If true all column numbers are 1-based (default).
        /// </summary>
        [JsonProperty("columnsStartAt1")]
        public bool ColumnsStartAt1 { get; set; } = true;

        /// <summary>
        /// Determines in what format paths are specified. The default is 'path'.
        /// </summary>
        [JsonProperty("pathFormat")]
        public string PathFormat { get; set; } = "path";

        /// <summary>
        /// Client supports the type attribute for variables.
        /// </summary>
        [JsonProperty("supportsVariableType")]
        public bool SupportsVariableType { get; set; }

        /// <summary>
        /// Client supports the paging of variables.
        /// </summary>
        [JsonProperty("supportsVariablePaging")]
        public bool SupportsVariablePaging { get; set; }

        /// <summary>
        /// Client supports the runInTerminal request.
        /// </summary>
        [JsonProperty("supportsRunInTerminalRequest")]
        public bool SupportsRunInTerminalRequest { get; set; }

        /// <summary>
        /// Client supports memory references.
        /// </summary>
        [JsonProperty("supportsMemoryReferences")]
        public bool SupportsMemoryReferences { get; set; }

        /// <summary>
        /// Client supports progress reporting.
        /// </summary>
        [JsonProperty("supportsProgressReporting")]
        public bool SupportsProgressReporting { get; set; }
    }

    /// <summary>
    /// Debug adapter capabilities returned in InitializeResponse.
    /// </summary>
    public class Capabilities
    {
        /// <summary>
        /// The debug adapter supports the configurationDone request.
        /// </summary>
        [JsonProperty("supportsConfigurationDoneRequest")]
        public bool SupportsConfigurationDoneRequest { get; set; }

        /// <summary>
        /// The debug adapter supports function breakpoints.
        /// </summary>
        [JsonProperty("supportsFunctionBreakpoints")]
        public bool SupportsFunctionBreakpoints { get; set; }

        /// <summary>
        /// The debug adapter supports conditional breakpoints.
        /// </summary>
        [JsonProperty("supportsConditionalBreakpoints")]
        public bool SupportsConditionalBreakpoints { get; set; }

        /// <summary>
        /// The debug adapter supports a (side effect free) evaluate request for data hovers.
        /// </summary>
        [JsonProperty("supportsEvaluateForHovers")]
        public bool SupportsEvaluateForHovers { get; set; }

        /// <summary>
        /// The debug adapter supports stepping back via the stepBack and reverseContinue requests.
        /// </summary>
        [JsonProperty("supportsStepBack")]
        public bool SupportsStepBack { get; set; }

        /// <summary>
        /// The debug adapter supports setting a variable to a value.
        /// </summary>
        [JsonProperty("supportsSetVariable")]
        public bool SupportsSetVariable { get; set; }

        /// <summary>
        /// The debug adapter supports restarting a frame.
        /// </summary>
        [JsonProperty("supportsRestartFrame")]
        public bool SupportsRestartFrame { get; set; }

        /// <summary>
        /// The debug adapter supports the gotoTargets request.
        /// </summary>
        [JsonProperty("supportsGotoTargetsRequest")]
        public bool SupportsGotoTargetsRequest { get; set; }

        /// <summary>
        /// The debug adapter supports the stepInTargets request.
        /// </summary>
        [JsonProperty("supportsStepInTargetsRequest")]
        public bool SupportsStepInTargetsRequest { get; set; }

        /// <summary>
        /// The debug adapter supports the completions request.
        /// </summary>
        [JsonProperty("supportsCompletionsRequest")]
        public bool SupportsCompletionsRequest { get; set; }

        /// <summary>
        /// The debug adapter supports the modules request.
        /// </summary>
        [JsonProperty("supportsModulesRequest")]
        public bool SupportsModulesRequest { get; set; }

        /// <summary>
        /// The debug adapter supports the terminate request.
        /// </summary>
        [JsonProperty("supportsTerminateRequest")]
        public bool SupportsTerminateRequest { get; set; }

        /// <summary>
        /// The debug adapter supports the terminateDebuggee attribute on the disconnect request.
        /// </summary>
        [JsonProperty("supportTerminateDebuggee")]
        public bool SupportTerminateDebuggee { get; set; }

        /// <summary>
        /// The debug adapter supports the delayed loading of parts of the stack.
        /// </summary>
        [JsonProperty("supportsDelayedStackTraceLoading")]
        public bool SupportsDelayedStackTraceLoading { get; set; }

        /// <summary>
        /// The debug adapter supports the loadedSources request.
        /// </summary>
        [JsonProperty("supportsLoadedSourcesRequest")]
        public bool SupportsLoadedSourcesRequest { get; set; }

        /// <summary>
        /// The debug adapter supports sending progress reporting events.
        /// </summary>
        [JsonProperty("supportsProgressReporting")]
        public bool SupportsProgressReporting { get; set; }

        /// <summary>
        /// The debug adapter supports the runInTerminal request.
        /// </summary>
        [JsonProperty("supportsRunInTerminalRequest")]
        public bool SupportsRunInTerminalRequest { get; set; }

        /// <summary>
        /// The debug adapter supports the cancel request.
        /// </summary>
        [JsonProperty("supportsCancelRequest")]
        public bool SupportsCancelRequest { get; set; }

        /// <summary>
        /// The debug adapter supports exception options.
        /// </summary>
        [JsonProperty("supportsExceptionOptions")]
        public bool SupportsExceptionOptions { get; set; }

        /// <summary>
        /// The debug adapter supports value formatting options.
        /// </summary>
        [JsonProperty("supportsValueFormattingOptions")]
        public bool SupportsValueFormattingOptions { get; set; }

        /// <summary>
        /// The debug adapter supports exception info request.
        /// </summary>
        [JsonProperty("supportsExceptionInfoRequest")]
        public bool SupportsExceptionInfoRequest { get; set; }
    }

    #endregion

    #region Attach Request

    /// <summary>
    /// Arguments for 'attach' request. Additional attributes are implementation specific.
    /// </summary>
    public class AttachRequestArguments
    {
        /// <summary>
        /// Arbitrary data from the previous, restarted session.
        /// </summary>
        [JsonProperty("__restart", NullValueHandling = NullValueHandling.Ignore)]
        public object Restart { get; set; }
    }

    #endregion

    #region Disconnect Request

    /// <summary>
    /// Arguments for 'disconnect' request.
    /// </summary>
    public class DisconnectRequestArguments
    {
        /// <summary>
        /// A value of true indicates that this disconnect request is part of a restart sequence.
        /// </summary>
        [JsonProperty("restart")]
        public bool Restart { get; set; }

        /// <summary>
        /// Indicates whether the debuggee should be terminated when the debugger is disconnected.
        /// </summary>
        [JsonProperty("terminateDebuggee")]
        public bool TerminateDebuggee { get; set; }

        /// <summary>
        /// Indicates whether the debuggee should stay suspended when the debugger is disconnected.
        /// </summary>
        [JsonProperty("suspendDebuggee")]
        public bool SuspendDebuggee { get; set; }
    }

    #endregion

    #region Threads Request/Response

    /// <summary>
    /// A Thread in DAP represents a unit of execution.
    /// For Actions runner, we have a single thread representing the job.
    /// </summary>
    public class Thread
    {
        /// <summary>
        /// Unique identifier for the thread.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// The name of the thread.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Response body for 'threads' request.
    /// </summary>
    public class ThreadsResponseBody
    {
        /// <summary>
        /// All threads.
        /// </summary>
        [JsonProperty("threads")]
        public List<Thread> Threads { get; set; } = new List<Thread>();
    }

    #endregion

    #region StackTrace Request/Response

    /// <summary>
    /// Arguments for 'stackTrace' request.
    /// </summary>
    public class StackTraceArguments
    {
        /// <summary>
        /// Retrieve the stacktrace for this thread.
        /// </summary>
        [JsonProperty("threadId")]
        public int ThreadId { get; set; }

        /// <summary>
        /// The index of the first frame to return.
        /// </summary>
        [JsonProperty("startFrame")]
        public int? StartFrame { get; set; }

        /// <summary>
        /// The maximum number of frames to return.
        /// </summary>
        [JsonProperty("levels")]
        public int? Levels { get; set; }
    }

    /// <summary>
    /// A Stackframe contains the source location.
    /// For Actions runner, each step is a stack frame.
    /// </summary>
    public class StackFrame
    {
        /// <summary>
        /// An identifier for the stack frame.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// The name of the stack frame, typically a method name.
        /// For Actions, this is the step display name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The source of the frame.
        /// </summary>
        [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
        public Source Source { get; set; }

        /// <summary>
        /// The line within the source of the frame.
        /// </summary>
        [JsonProperty("line")]
        public int Line { get; set; }

        /// <summary>
        /// Start position of the range covered by the stack frame.
        /// </summary>
        [JsonProperty("column")]
        public int Column { get; set; }

        /// <summary>
        /// The end line of the range covered by the stack frame.
        /// </summary>
        [JsonProperty("endLine", NullValueHandling = NullValueHandling.Ignore)]
        public int? EndLine { get; set; }

        /// <summary>
        /// End position of the range covered by the stack frame.
        /// </summary>
        [JsonProperty("endColumn", NullValueHandling = NullValueHandling.Ignore)]
        public int? EndColumn { get; set; }

        /// <summary>
        /// A hint for how to present this frame in the UI.
        /// </summary>
        [JsonProperty("presentationHint", NullValueHandling = NullValueHandling.Ignore)]
        public string PresentationHint { get; set; }
    }

    /// <summary>
    /// A Source is a descriptor for source code.
    /// </summary>
    public class Source
    {
        /// <summary>
        /// The short name of the source.
        /// </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// The path of the source to be shown in the UI.
        /// </summary>
        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }

        /// <summary>
        /// If the value > 0 the contents of the source must be retrieved through
        /// the 'source' request (even if a path is specified).
        /// </summary>
        [JsonProperty("sourceReference", NullValueHandling = NullValueHandling.Ignore)]
        public int? SourceReference { get; set; }

        /// <summary>
        /// A hint for how to present the source in the UI.
        /// </summary>
        [JsonProperty("presentationHint", NullValueHandling = NullValueHandling.Ignore)]
        public string PresentationHint { get; set; }
    }

    /// <summary>
    /// Response body for 'stackTrace' request.
    /// </summary>
    public class StackTraceResponseBody
    {
        /// <summary>
        /// The frames of the stack frame.
        /// </summary>
        [JsonProperty("stackFrames")]
        public List<StackFrame> StackFrames { get; set; } = new List<StackFrame>();

        /// <summary>
        /// The total number of frames available in the stack.
        /// </summary>
        [JsonProperty("totalFrames", NullValueHandling = NullValueHandling.Ignore)]
        public int? TotalFrames { get; set; }
    }

    #endregion

    #region Scopes Request/Response

    /// <summary>
    /// Arguments for 'scopes' request.
    /// </summary>
    public class ScopesArguments
    {
        /// <summary>
        /// Retrieve the scopes for the stack frame identified by frameId.
        /// </summary>
        [JsonProperty("frameId")]
        public int FrameId { get; set; }
    }

    /// <summary>
    /// A Scope is a named container for variables.
    /// For Actions runner, scopes are: github, env, inputs, steps, secrets, runner, job
    /// </summary>
    public class Scope
    {
        /// <summary>
        /// Name of the scope such as 'Arguments', 'Locals', or 'Registers'.
        /// For Actions: 'github', 'env', 'inputs', 'steps', 'secrets', 'runner', 'job'
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// A hint for how to present this scope in the UI.
        /// </summary>
        [JsonProperty("presentationHint", NullValueHandling = NullValueHandling.Ignore)]
        public string PresentationHint { get; set; }

        /// <summary>
        /// The variables of this scope can be retrieved by passing the value of
        /// variablesReference to the variables request.
        /// </summary>
        [JsonProperty("variablesReference")]
        public int VariablesReference { get; set; }

        /// <summary>
        /// The number of named variables in this scope.
        /// </summary>
        [JsonProperty("namedVariables", NullValueHandling = NullValueHandling.Ignore)]
        public int? NamedVariables { get; set; }

        /// <summary>
        /// The number of indexed variables in this scope.
        /// </summary>
        [JsonProperty("indexedVariables", NullValueHandling = NullValueHandling.Ignore)]
        public int? IndexedVariables { get; set; }

        /// <summary>
        /// If true, the number of variables in this scope is large or expensive to retrieve.
        /// </summary>
        [JsonProperty("expensive")]
        public bool Expensive { get; set; }

        /// <summary>
        /// The source for this scope.
        /// </summary>
        [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
        public Source Source { get; set; }

        /// <summary>
        /// The start line of the range covered by this scope.
        /// </summary>
        [JsonProperty("line", NullValueHandling = NullValueHandling.Ignore)]
        public int? Line { get; set; }

        /// <summary>
        /// Start position of the range covered by this scope.
        /// </summary>
        [JsonProperty("column", NullValueHandling = NullValueHandling.Ignore)]
        public int? Column { get; set; }

        /// <summary>
        /// The end line of the range covered by this scope.
        /// </summary>
        [JsonProperty("endLine", NullValueHandling = NullValueHandling.Ignore)]
        public int? EndLine { get; set; }

        /// <summary>
        /// End position of the range covered by this scope.
        /// </summary>
        [JsonProperty("endColumn", NullValueHandling = NullValueHandling.Ignore)]
        public int? EndColumn { get; set; }
    }

    /// <summary>
    /// Response body for 'scopes' request.
    /// </summary>
    public class ScopesResponseBody
    {
        /// <summary>
        /// The scopes of the stack frame.
        /// </summary>
        [JsonProperty("scopes")]
        public List<Scope> Scopes { get; set; } = new List<Scope>();
    }

    #endregion

    #region Variables Request/Response

    /// <summary>
    /// Arguments for 'variables' request.
    /// </summary>
    public class VariablesArguments
    {
        /// <summary>
        /// The variable for which to retrieve its children.
        /// </summary>
        [JsonProperty("variablesReference")]
        public int VariablesReference { get; set; }

        /// <summary>
        /// Filter to limit the child variables to either named or indexed.
        /// </summary>
        [JsonProperty("filter", NullValueHandling = NullValueHandling.Ignore)]
        public string Filter { get; set; }

        /// <summary>
        /// The index of the first variable to return.
        /// </summary>
        [JsonProperty("start", NullValueHandling = NullValueHandling.Ignore)]
        public int? Start { get; set; }

        /// <summary>
        /// The number of variables to return.
        /// </summary>
        [JsonProperty("count", NullValueHandling = NullValueHandling.Ignore)]
        public int? Count { get; set; }
    }

    /// <summary>
    /// A Variable is a name/value pair.
    /// </summary>
    public class Variable
    {
        /// <summary>
        /// The variable's name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The variable's value.
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }

        /// <summary>
        /// The type of the variable's value.
        /// </summary>
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary>
        /// If variablesReference is > 0, the variable is structured and its children
        /// can be retrieved by passing variablesReference to the variables request.
        /// </summary>
        [JsonProperty("variablesReference")]
        public int VariablesReference { get; set; }

        /// <summary>
        /// The number of named child variables.
        /// </summary>
        [JsonProperty("namedVariables", NullValueHandling = NullValueHandling.Ignore)]
        public int? NamedVariables { get; set; }

        /// <summary>
        /// The number of indexed child variables.
        /// </summary>
        [JsonProperty("indexedVariables", NullValueHandling = NullValueHandling.Ignore)]
        public int? IndexedVariables { get; set; }

        /// <summary>
        /// A memory reference to a location appropriate for this result.
        /// </summary>
        [JsonProperty("memoryReference", NullValueHandling = NullValueHandling.Ignore)]
        public string MemoryReference { get; set; }

        /// <summary>
        /// A reference that allows the client to request the location where the
        /// variable's value is declared.
        /// </summary>
        [JsonProperty("declarationLocationReference", NullValueHandling = NullValueHandling.Ignore)]
        public int? DeclarationLocationReference { get; set; }

        /// <summary>
        /// The evaluatable name of this variable which can be passed to the evaluate
        /// request to fetch the variable's value.
        /// </summary>
        [JsonProperty("evaluateName", NullValueHandling = NullValueHandling.Ignore)]
        public string EvaluateName { get; set; }
    }

    /// <summary>
    /// Response body for 'variables' request.
    /// </summary>
    public class VariablesResponseBody
    {
        /// <summary>
        /// All (or a range) of variables for the given variable reference.
        /// </summary>
        [JsonProperty("variables")]
        public List<Variable> Variables { get; set; } = new List<Variable>();
    }

    #endregion

    #region Continue Request/Response

    /// <summary>
    /// Arguments for 'continue' request.
    /// </summary>
    public class ContinueArguments
    {
        /// <summary>
        /// Specifies the active thread. If the debug adapter supports single thread
        /// execution, setting this will resume only the specified thread.
        /// </summary>
        [JsonProperty("threadId")]
        public int ThreadId { get; set; }

        /// <summary>
        /// If this flag is true, execution is resumed only for the thread with given
        /// threadId. If false, all threads are resumed.
        /// </summary>
        [JsonProperty("singleThread")]
        public bool SingleThread { get; set; }
    }

    /// <summary>
    /// Response body for 'continue' request.
    /// </summary>
    public class ContinueResponseBody
    {
        /// <summary>
        /// If true, all threads are resumed. If false, only the thread with the given
        /// threadId is resumed.
        /// </summary>
        [JsonProperty("allThreadsContinued")]
        public bool AllThreadsContinued { get; set; } = true;
    }

    #endregion

    #region Next Request

    /// <summary>
    /// Arguments for 'next' request.
    /// </summary>
    public class NextArguments
    {
        /// <summary>
        /// Specifies the thread for which to resume execution for one step.
        /// </summary>
        [JsonProperty("threadId")]
        public int ThreadId { get; set; }

        /// <summary>
        /// Stepping granularity.
        /// </summary>
        [JsonProperty("granularity", NullValueHandling = NullValueHandling.Ignore)]
        public string Granularity { get; set; }

        /// <summary>
        /// If this flag is true, all other suspended threads are not resumed.
        /// </summary>
        [JsonProperty("singleThread")]
        public bool SingleThread { get; set; }
    }

    #endregion

    #region Evaluate Request/Response

    /// <summary>
    /// Arguments for 'evaluate' request.
    /// </summary>
    public class EvaluateArguments
    {
        /// <summary>
        /// The expression to evaluate.
        /// </summary>
        [JsonProperty("expression")]
        public string Expression { get; set; }

        /// <summary>
        /// Evaluate the expression in the scope of this stack frame.
        /// </summary>
        [JsonProperty("frameId", NullValueHandling = NullValueHandling.Ignore)]
        public int? FrameId { get; set; }

        /// <summary>
        /// The context in which the evaluate request is used.
        /// Values: 'watch', 'repl', 'hover', 'clipboard', 'variables'
        /// </summary>
        [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
        public string Context { get; set; }
    }

    /// <summary>
    /// Response body for 'evaluate' request.
    /// </summary>
    public class EvaluateResponseBody
    {
        /// <summary>
        /// The result of the evaluate request.
        /// </summary>
        [JsonProperty("result")]
        public string Result { get; set; }

        /// <summary>
        /// The type of the evaluate result.
        /// </summary>
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary>
        /// If variablesReference is > 0, the evaluate result is structured.
        /// </summary>
        [JsonProperty("variablesReference")]
        public int VariablesReference { get; set; }

        /// <summary>
        /// The number of named child variables.
        /// </summary>
        [JsonProperty("namedVariables", NullValueHandling = NullValueHandling.Ignore)]
        public int? NamedVariables { get; set; }

        /// <summary>
        /// The number of indexed child variables.
        /// </summary>
        [JsonProperty("indexedVariables", NullValueHandling = NullValueHandling.Ignore)]
        public int? IndexedVariables { get; set; }

        /// <summary>
        /// A memory reference to a location appropriate for this result.
        /// </summary>
        [JsonProperty("memoryReference", NullValueHandling = NullValueHandling.Ignore)]
        public string MemoryReference { get; set; }
    }

    #endregion

    #region Completions Request/Response

    /// <summary>
    /// Arguments for 'completions' request.
    /// </summary>
    public class CompletionsArguments
    {
        /// <summary>
        /// Returns completions in the scope of this stack frame.
        /// </summary>
        [JsonProperty("frameId", NullValueHandling = NullValueHandling.Ignore)]
        public int? FrameId { get; set; }

        /// <summary>
        /// One or more source lines. Typically this is the text users have typed
        /// in the debug console (REPL).
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// The position within 'text' for which to determine the completion proposals.
        /// It is measured in UTF-16 code units.
        /// </summary>
        [JsonProperty("column")]
        public int Column { get; set; }

        /// <summary>
        /// A line for which to determine the completion proposals.
        /// If missing the first line of the text is assumed.
        /// </summary>
        [JsonProperty("line", NullValueHandling = NullValueHandling.Ignore)]
        public int? Line { get; set; }
    }

    /// <summary>
    /// A completion item in the debug console.
    /// </summary>
    public class CompletionItem
    {
        /// <summary>
        /// The label of this completion item.
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// If text is returned and not an empty string, then it is inserted instead
        /// of the label.
        /// </summary>
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        /// <summary>
        /// A human-readable string with additional information about this item.
        /// </summary>
        [JsonProperty("detail", NullValueHandling = NullValueHandling.Ignore)]
        public string Detail { get; set; }

        /// <summary>
        /// The item's type. Typically the client uses this information to render the item
        /// in the UI with an icon.
        /// Values: 'method', 'function', 'constructor', 'field', 'variable', 'class',
        /// 'interface', 'module', 'property', 'unit', 'value', 'enum', 'keyword',
        /// 'snippet', 'text', 'color', 'file', 'reference', 'customcolor'
        /// </summary>
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary>
        /// Start position (0-based) within 'text' that should be replaced
        /// by the completion text.
        /// </summary>
        [JsonProperty("start", NullValueHandling = NullValueHandling.Ignore)]
        public int? Start { get; set; }

        /// <summary>
        /// Length of the text that should be replaced by the completion text.
        /// </summary>
        [JsonProperty("length", NullValueHandling = NullValueHandling.Ignore)]
        public int? Length { get; set; }
    }

    /// <summary>
    /// Response body for 'completions' request.
    /// </summary>
    public class CompletionsResponseBody
    {
        /// <summary>
        /// The possible completions.
        /// </summary>
        [JsonProperty("targets")]
        public List<CompletionItem> Targets { get; set; } = new List<CompletionItem>();
    }

    #endregion

    #region Events

    /// <summary>
    /// Body for 'stopped' event.
    /// The event indicates that the execution of the debuggee has stopped.
    /// </summary>
    public class StoppedEventBody
    {
        /// <summary>
        /// The reason for the event. For backward compatibility this string is shown
        /// in the UI if the description attribute is missing.
        /// Values: 'step', 'breakpoint', 'exception', 'pause', 'entry', 'goto',
        /// 'function breakpoint', 'data breakpoint', 'instruction breakpoint'
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }

        /// <summary>
        /// The full reason for the event, e.g. 'Paused on exception'.
        /// This string is shown in the UI as is and can be translated.
        /// </summary>
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        /// <summary>
        /// The thread which was stopped.
        /// </summary>
        [JsonProperty("threadId", NullValueHandling = NullValueHandling.Ignore)]
        public int? ThreadId { get; set; }

        /// <summary>
        /// A value of true hints to the client that this event should not change the focus.
        /// </summary>
        [JsonProperty("preserveFocusHint", NullValueHandling = NullValueHandling.Ignore)]
        public bool? PreserveFocusHint { get; set; }

        /// <summary>
        /// Additional information. E.g. if reason is 'exception', text contains the
        /// exception name.
        /// </summary>
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        /// <summary>
        /// If allThreadsStopped is true, a debug adapter can announce that all threads
        /// have stopped.
        /// </summary>
        [JsonProperty("allThreadsStopped", NullValueHandling = NullValueHandling.Ignore)]
        public bool? AllThreadsStopped { get; set; }

        /// <summary>
        /// Ids of the breakpoints that triggered the event.
        /// </summary>
        [JsonProperty("hitBreakpointIds", NullValueHandling = NullValueHandling.Ignore)]
        public List<int> HitBreakpointIds { get; set; }
    }

    /// <summary>
    /// Body for 'continued' event.
    /// The event indicates that the execution of the debuggee has continued.
    /// </summary>
    public class ContinuedEventBody
    {
        /// <summary>
        /// The thread which was continued.
        /// </summary>
        [JsonProperty("threadId")]
        public int ThreadId { get; set; }

        /// <summary>
        /// If true, all threads have been resumed.
        /// </summary>
        [JsonProperty("allThreadsContinued", NullValueHandling = NullValueHandling.Ignore)]
        public bool? AllThreadsContinued { get; set; }
    }

    /// <summary>
    /// Body for 'terminated' event.
    /// The event indicates that debugging of the debuggee has terminated.
    /// </summary>
    public class TerminatedEventBody
    {
        /// <summary>
        /// A debug adapter may set restart to true to request that the client
        /// restarts the session.
        /// </summary>
        [JsonProperty("restart", NullValueHandling = NullValueHandling.Ignore)]
        public object Restart { get; set; }
    }

    /// <summary>
    /// Body for 'output' event.
    /// The event indicates that the target has produced some output.
    /// </summary>
    public class OutputEventBody
    {
        /// <summary>
        /// The output category. If not specified, 'console' is assumed.
        /// Values: 'console', 'important', 'stdout', 'stderr', 'telemetry'
        /// </summary>
        [JsonProperty("category", NullValueHandling = NullValueHandling.Ignore)]
        public string Category { get; set; }

        /// <summary>
        /// The output to report.
        /// </summary>
        [JsonProperty("output")]
        public string Output { get; set; }

        /// <summary>
        /// Support for keeping an output log organized by grouping related messages.
        /// Values: 'start', 'startCollapsed', 'end'
        /// </summary>
        [JsonProperty("group", NullValueHandling = NullValueHandling.Ignore)]
        public string Group { get; set; }

        /// <summary>
        /// If variablesReference is > 0, the output contains objects which can be
        /// retrieved by passing variablesReference to the variables request.
        /// </summary>
        [JsonProperty("variablesReference", NullValueHandling = NullValueHandling.Ignore)]
        public int? VariablesReference { get; set; }

        /// <summary>
        /// The source location where the output was produced.
        /// </summary>
        [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
        public Source Source { get; set; }

        /// <summary>
        /// The source location's line where the output was produced.
        /// </summary>
        [JsonProperty("line", NullValueHandling = NullValueHandling.Ignore)]
        public int? Line { get; set; }

        /// <summary>
        /// The position in line where the output was produced.
        /// </summary>
        [JsonProperty("column", NullValueHandling = NullValueHandling.Ignore)]
        public int? Column { get; set; }

        /// <summary>
        /// Additional data to report.
        /// </summary>
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }
    }

    /// <summary>
    /// Body for 'thread' event.
    /// The event indicates that a thread has started or exited.
    /// </summary>
    public class ThreadEventBody
    {
        /// <summary>
        /// The reason for the event.
        /// Values: 'started', 'exited'
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }

        /// <summary>
        /// The identifier of the thread.
        /// </summary>
        [JsonProperty("threadId")]
        public int ThreadId { get; set; }
    }

    /// <summary>
    /// Body for 'exited' event.
    /// The event indicates that the debuggee has exited and returns its exit code.
    /// </summary>
    public class ExitedEventBody
    {
        /// <summary>
        /// The exit code returned from the debuggee.
        /// </summary>
        [JsonProperty("exitCode")]
        public int ExitCode { get; set; }
    }

    #endregion

    #region Error Response

    /// <summary>
    /// A structured error message.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Unique identifier for the message.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// A format string for the message.
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// An object used as a dictionary for looking up the variables in the format string.
        /// </summary>
        [JsonProperty("variables", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Variables { get; set; }

        /// <summary>
        /// If true send to telemetry.
        /// </summary>
        [JsonProperty("sendTelemetry", NullValueHandling = NullValueHandling.Ignore)]
        public bool? SendTelemetry { get; set; }

        /// <summary>
        /// If true show user.
        /// </summary>
        [JsonProperty("showUser", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ShowUser { get; set; }

        /// <summary>
        /// A url where additional information about this message can be found.
        /// </summary>
        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }

        /// <summary>
        /// A label that is presented to the user as the UI for opening the url.
        /// </summary>
        [JsonProperty("urlLabel", NullValueHandling = NullValueHandling.Ignore)]
        public string UrlLabel { get; set; }
    }

    /// <summary>
    /// Body for error responses.
    /// </summary>
    public class ErrorResponseBody
    {
        /// <summary>
        /// A structured error message.
        /// </summary>
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public Message Error { get; set; }
    }

    #endregion
}
