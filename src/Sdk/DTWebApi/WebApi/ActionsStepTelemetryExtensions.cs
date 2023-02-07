using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    public static class ActionsStepTelemetryExtensions
    {
        public static IList<StepResult> ToStepResults(
            this IList<ActionsStepTelemetry> telemetries) 
        {
            var stepResults = new List<StepResult>();
            foreach (var telemetry in telemetries)
            {
                stepResults.Add(telemetry.ToStepResult());
            }

            return stepResults;
        }

        public static StepResult ToStepResult(
            this ActionsStepTelemetry telemetry) 
        {
            var stepResult = new StepResult
            {
                Name = telemetry.DisplayName,
                ExternalID = telemetry.StepId,
                StartedAt = telemetry.StartTime,
                CompletedAt = telemetry.FinishTime,
                Conclusion = telemetry.Result,
                Status = telemetry.State,
                Number = telemetry.Order,
            };
            return stepResult;
        }
    }
}