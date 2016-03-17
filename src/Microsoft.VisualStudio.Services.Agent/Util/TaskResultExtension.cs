using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class TaskResultUtil
    {
        private static readonly int _returnCodeOffset = 100;

        public static int TranslateToReturnCode(TaskResult result)
        {
            return _returnCodeOffset + (int)result;
        }

        public static TaskResult TranslateFromReturnCode(int returnCode)
        {
            int resultInt = returnCode - _returnCodeOffset;
            if (Enum.IsDefined(typeof(TaskResult), resultInt))
            {
                return (TaskResult)resultInt;
            }
            else
            {
                return TaskResult.Failed;
            }
        }
    }
}
