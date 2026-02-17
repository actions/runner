#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    /// <summary>
    /// Index and depth while replaying a YAML anchor
    /// </summary>
    sealed class YamlReplayState
    {
        /// <summary>
        /// Gets or sets the current node event index that is being replayed.
        /// </summary>
        public Int32 Index { get; set; }

        /// <summary>
        /// Gets or sets the depth within the current anchor that is being replayed.
        /// When the depth reaches zero, the anchor replay is complete.
        /// </summary>
        public Int32 Depth { get; set; }
    }
}
