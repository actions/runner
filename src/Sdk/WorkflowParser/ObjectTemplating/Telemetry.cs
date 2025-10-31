using System;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating
{
    /// <summary>
    /// Tracks telemetry data during workflow parsing.
    /// </summary>
    public sealed class Telemetry
    {
        /// <summary>
        /// Gets or sets the count of YAML anchors encountered during parsing.
        /// </summary>
        public Int32 YamlAnchors { get; set; }

        /// <summary>
        /// Gets or sets the count of YAML aliases encountered during parsing.
        /// </summary>
        public Int32 YamlAliases { get; set; }
    }
}
