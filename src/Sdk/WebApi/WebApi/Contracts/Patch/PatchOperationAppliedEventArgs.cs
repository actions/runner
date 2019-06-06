using System.Collections.Generic;

namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// Event args for the applied patch operation.
    /// </summary>
    public class PatchOperationAppliedEventArgs
    {
        public PatchOperationAppliedEventArgs(IEnumerable<string> path, Operation operation)
        {
            this.Path = path;
            this.Operation = operation;
        }

        /// <summary>
        /// The current path.
        /// </summary>
        public IEnumerable<string> Path { get; private set; }

        /// <summary>
        /// The operation being applied.
        /// </summary>
        public Operation Operation { get; private set; }
    }
}
