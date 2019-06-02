using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Logging
{
    internal interface ISecret
    {
        /// <summary>
        /// Returns one item (start, length) for each match found in the input string.
        /// </summary>
        IEnumerable<ReplacementPosition> GetPositions(String input);
    }
}
