using System.Collections.Generic;

namespace GitHub.Services.WebApi.Patch
{
    /// <summary>
    /// Event args for the applying patch operation.
    /// </summary>
    public class PatchOperationApplyingEventArgs
    {
        public PatchOperationApplyingEventArgs(IEnumerable<string> path, Operation operation)
        {
            this.Path = path;
            this.Operation = operation;
        }

        /// <summary>
        /// The current path.
        /// </summary>
        public IEnumerable<string> Path { get; private set; }

        /// <summary>
        /// The operation about to be applied.
        /// </summary>
        public Operation Operation { get; private set; }
    }
}
