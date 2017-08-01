using System;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ParseOptions
    {
        public ParseOptions()
        {
        }

        internal ParseOptions(ParseOptions copy)
        {
            MaxFiles = copy.MaxFiles;
            MustacheEvaluationMaxResultLength = copy.MustacheEvaluationMaxResultLength;
            MustacheEvaluationTimeout = copy.MustacheEvaluationTimeout;
            MustacheMaxDepth = copy.MustacheMaxDepth;
        }

        /// <summary>
        /// Gets or sets the maximum number files that can be loaded when parsing a pipeline. Zero or less is treated as infinite.
        /// </summary>
        public Int32 MaxFiles { get; set; }

        /// <summary>
        /// Gets or sets the evaluation max result bytes for each mustache template. Zero or less is treated as unlimited.
        /// </summary>
        public Int32 MustacheEvaluationMaxResultLength { get; set; }

        /// <summary>
        /// Gets or sets the evaluation timeout for each mustache template. Zero or less is treated as infinite.
        /// </summary>
        public TimeSpan MustacheEvaluationTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum depth for each mustache template. This number limits the maximum nest level. Any number less
        /// than 1 is treated as Int32.MaxValue. An exception will be thrown when the threshold is exceeded.
        /// </summary>
        public Int32 MustacheMaxDepth { get; set; }
    }
}
