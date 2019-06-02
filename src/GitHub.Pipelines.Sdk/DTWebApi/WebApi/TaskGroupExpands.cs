namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    using System;

    [Flags]
    public enum TaskGroupExpands
    {
        None = 0,
        Tasks = 2,
    }
}