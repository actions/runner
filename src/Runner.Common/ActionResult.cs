using System;

namespace GitHub.Runner.Common
{
    public enum ActionResult
    {
        Success = 0,

        Failure = 1,

        Cancelled = 2,

        Skipped = 3
    }
}