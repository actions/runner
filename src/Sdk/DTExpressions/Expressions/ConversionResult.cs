using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct ConversionResult
    {
        /// <summary>
        /// Result object after the conversion
        /// </summary>
        public Object Result;

        /// <summary>
        /// Memory overhead for the result object
        /// </summary>
        public ResultMemory ResultMemory;
    }
}
