namespace GitHub.Actions.WorkflowParser
{
    public sealed class ParseOptions
    {
        public ParseOptions()
        {
        }

        internal ParseOptions(ParseOptions copy)
        {
            AllowAnchors = copy.AllowAnchors;
            MaxDepth = copy.MaxDepth;
            MaxFiles = copy.MaxFiles;
            MaxFileSize = copy.MaxFileSize;
            MaxJobLimit = copy.MaxJobLimit;
            MaxNestedReusableWorkflowsDepth = copy.MaxNestedReusableWorkflowsDepth;
            MaxResultSize = copy.MaxResultSize;
            SkipReusableWorkflows = copy.SkipReusableWorkflows;
        }

        /// <summary>
        /// Gets or sets a value indicating whether YAML anchors are allowed.
        /// </summary>
        public bool AllowAnchors { get; set; }

        /// <summary>
        /// Gets or sets the maximum element depth when parsing a workflow.
        /// </summary>
        public int MaxDepth { get; set; } = 50;

        /// <summary>
        /// Gets the maximum error message length before the message will be truncated.
        /// </summary>
        public int MaxErrorMessageLength => 500;

        /// <summary>
        /// Gets the maximum number of errors that can be recorded when parsing a workflow.
        /// </summary>
        public int MaxErrors => 10;

        /// <summary>
        /// Gets or sets the maximum number of files that can be loaded when parsing a workflow. Zero or less is treated as infinite.
        /// </summary>
        public int MaxFiles { get; set; } = 51; // 1 initial caller + max 50 reusable workflow references

        /// <summary>
        /// Gets or set the maximum number of characters a file can contain when parsing a workflow.
        /// </summary>
        public int MaxFileSize { get; set; } = 1024 * 1024;

        /// <summary>
        /// Gets or sets the maximum number of internal parsing events. This concept was initially
        /// introduced to prevent infinite loops from user-controlled looping constructs. However,
        /// we no longer have looping constructs.
        ///
        /// This concept can be removed.
        /// </summary>
        public int MaxParseEvents => 1000000; // 1 million

        /// <summary>
        /// Gets or sets the maximum number of jobs that can be defined in a workflow (includes nested workflows).
        /// Zero or less is treated as infinite.
        /// </summary>
        public int MaxJobLimit { get; set; }

        /// <summary>
        /// Gets or sets the maximum workflow nest depth. Zero indicates reusable workflows are not allowed.
        /// </summary>
        public int MaxNestedReusableWorkflowsDepth { get; set; }

        /// <summary>
        /// Gets or sets the maximum size of the result in bytes.
        /// </summary>
        public int MaxResultSize { get; set; } = 10 * 1024 * 1024; // 10 mb

        /// <summary>
        /// Gets or sets a value indicating whether to skip loading reusable workflows.
        /// </summary>
        public bool SkipReusableWorkflows { get; set; }
    }
}
