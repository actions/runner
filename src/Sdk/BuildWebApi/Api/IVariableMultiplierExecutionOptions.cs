using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Build.WebApi
{
    public interface IVariableMultiplierExecutionOptions
    {
        Int32 MaxConcurrency
        {
            get;
        }

        Boolean ContinueOnError
        {
            get;
        }

        List<String> Multipliers
        {
            get;
        }
    }
}
