using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Utility class to perform operations on Variable groups.
    /// </summary>
    public static class VariableGroupUtility
    {
        public static VariableValue Clone(this VariableValue value)
        {
            return new VariableValue(value);
        }
    }
}
