using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
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
            MaxFileSize = copy.MaxFileSize;
            MaxResultSize = copy.MaxResultSize;
        }

        public Int32 MaxDepth => 50;

        /// <summary>
        /// Gets the maximum error message length before the message will be truncated.
        /// </summary>
        public Int32 MaxErrorMessageLength => 500;

        /// <summary>
        /// Gets the maximum number of errors that can be recorded when parsing a pipeline.
        /// </summary>
        public Int32 MaxErrors => 10;

         /// <summary>
        /// Gets or sets the maximum number of files that can be loaded when parsing a pipeline. Zero or less is treated as infinite.
        /// </summary>
        public Int32 MaxFiles { get; set; } = 50;

        public Int32 MaxFileSize { get; set; } = 1024 * 1024; // 1 mb

        public Int32 MaxParseEvents => 1000000; // 1 million

        public Int32 MaxResultSize { get; set; } = 10 * 1024 * 1024; // 10 mb
    }
}
