using GitHub.DistributedTask.WebApi;
using System;

namespace GitHub.Runner.Common.Util
{
    public static class TaskResultUtil
    {
        private static readonly int _returnCodeOffset = 100;

        public static bool IsValidReturnCode(int returnCode)
        {
            int resultInt = returnCode - _returnCodeOffset;
            return Enum.IsDefined(typeof(TaskResult), resultInt);
        }

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

        // Merge 2 TaskResults get the worst result.
        // Succeeded -> Failed/Canceled/Skipped/Abandoned
        // Failed -> Failed/Canceled
        // Canceled -> Canceled
        // Skipped -> Skipped
        // Abandoned -> Abandoned
        public static TaskResult MergeTaskResults(TaskResult? currentResult, TaskResult comingResult)
        {
            if (currentResult == null)
            {
                return comingResult;
            }

            // current result is Canceled/Skip/Abandoned
            if (currentResult > TaskResult.Failed)
            {
                return currentResult.Value;
            }

            // comming result is bad than current result
            if (comingResult >= currentResult)
            {
                return comingResult;
            }

            return currentResult.Value;
        }

        public static ActionResult ToActionResult(this TaskResult result)
        {
            switch (result)
            {
                case TaskResult.Succeeded:
                    return ActionResult.Success;
                case TaskResult.Failed:
                    return ActionResult.Failure;
                case TaskResult.Canceled:
                    return ActionResult.Cancelled;
                case TaskResult.Skipped:
                    return ActionResult.Skipped;
                default:
                    throw new NotSupportedException(result.ToString());
            }
        }
    }
}
