using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ParallelExecutionOptions
    {
        public ParallelExecutionOptions()
        {
        }

        private ParallelExecutionOptions(ParallelExecutionOptions optionsToCopy)
        {
            this.Matrix = optionsToCopy.Matrix;
            this.MaxConcurrency = optionsToCopy.MaxConcurrency;
        }

        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(ExpressionValueJsonConverter<IDictionary<String, IDictionary<String, String>>>))]
        public ExpressionValue<IDictionary<String, IDictionary<String, String>>> Matrix
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(ExpressionValueJsonConverter<Int32>))]
        public ExpressionValue<Int32> MaxConcurrency
        {
            get;
            set;
        }

        public ParallelExecutionOptions Clone()
        {
            return new ParallelExecutionOptions(this);
        }

        internal JobExecutionContext CreateJobContext(
            PhaseExecutionContext context,
            String jobName,
            Int32 attempt,
            ExpressionValue<String> container,
            IDictionary<String, ExpressionValue<String>> sidecarContainers,
            Boolean continueOnError,
            Int32 timeoutInMinutes,
            Int32 cancelTimeoutInMinutes,
            IJobFactory jobFactory)
        {
            // perform regular expansion with a filter
            var options = new JobExpansionOptions(jobName, attempt);

            return GenerateJobContexts(
                context,
                container,
                sidecarContainers,
                continueOnError,
                timeoutInMinutes,
                cancelTimeoutInMinutes,
                jobFactory,
                options)
                .FirstOrDefault();
        }

        internal ExpandPhaseResult Expand(
            PhaseExecutionContext context,
            ExpressionValue<String> container,
            IDictionary<String, ExpressionValue<String>> sidecarContainers,
            Boolean continueOnError,
            Int32 timeoutInMinutes,
            Int32 cancelTimeoutInMinutes,
            IJobFactory jobFactory,
            JobExpansionOptions options)
        {
            var jobContexts = GenerateJobContexts(
                context,
                container,
                sidecarContainers,
                continueOnError,
                timeoutInMinutes,
                cancelTimeoutInMinutes,
                jobFactory,
                options);

            var result = new ExpandPhaseResult();
            foreach (var c in jobContexts)
            {
                result.Jobs.Add(c.Job);
            }

            // parse MaxConcurrency request
            var numberOfJobs = jobContexts.Count;
            var userProvidedValue = context.Evaluate(
                name: nameof(MaxConcurrency),
                expression: this.MaxConcurrency,
                defaultValue: 0).Value;

            // setting max to 0 or less is shorthand for "unlimited"
            if (userProvidedValue <= 0)
            {
                userProvidedValue = numberOfJobs;
            }

            result.MaxConcurrency = userProvidedValue;
            return result;
        }

        internal IList<JobExecutionContext> GenerateJobContexts(
            PhaseExecutionContext context,
            ExpressionValue<String> container,
            IDictionary<String, ExpressionValue<String>> sidecarContainers,
            Boolean continueOnError,
            Int32 timeoutInMinutes,
            Int32 cancelTimeoutInMinutes,
            IJobFactory jobFactory,
            JobExpansionOptions options)
        {
            // We don't want job variables to be set into the phase context so we create a child context for each unique configuration
            var jobContexts = new List<JobExecutionContext>();
            void GenerateContext(
                String displayName,
                String configuration,
                IDictionary<String, String> configurationVariables = null,
                String parallelExecutionType = null,
                Int32 positionInPhase = 1,
                Int32 totalJobsInPhase = 1)
            {
                // configurations should (eventually) follow configuration naming conventions
                if (String.IsNullOrEmpty(configuration))
                {
                    configuration = PipelineConstants.DefaultJobName;
                }

                // Determine attempt number.
                // if we have a custom value, it wins.
                // if we have previously attempted this configuration,
                //    the new attempt number is one greater than the previous.
                // 1 is the minimum attempt number
                var attemptNumber = options?.GetAttemptNumber(configuration) ?? -1;
                if (attemptNumber < 1)
                {
                    var previousAttempt = context.PreviousAttempt;
                    if (previousAttempt != null)
                    {
                        var jobInstance = context.PreviousAttempt?.Jobs.FirstOrDefault(x => x.Job.Name.Equals(configuration, StringComparison.OrdinalIgnoreCase));
                        if (jobInstance != null)
                        {
                            attemptNumber = jobInstance.Job.Attempt + 1;
                        }
                    }
                }

                if (attemptNumber < 1)
                {
                    attemptNumber = 1;
                }

                var jobContext = context.CreateJobContext(
                    name: configuration,
                    attempt: attemptNumber,
                    positionInPhase,
                    totalJobsInPhase);

                // add parallel execution type
                if (parallelExecutionType != null)
                {
                    jobContext.SetSystemVariables(new List<Variable>
                    {
                        new Variable
                        {
                            Name = WellKnownDistributedTaskVariables.ParallelExecutionType,
                            Value = parallelExecutionType
                        }
                    });
                }

                if (configurationVariables != null)
                {
                    jobContext.SetUserVariables(configurationVariables);
                }

                // create job model from factory
                jobContext.Job.Definition = jobFactory.CreateJob(
                    jobContext,
                    container,
                    sidecarContainers,
                    continueOnError,
                    timeoutInMinutes,
                    cancelTimeoutInMinutes,
                    displayName);

                jobContexts.Add(jobContext);

                if (jobContexts.Count > context.ExecutionOptions.MaxJobExpansion)
                {
                    // Note: this is a little weird: it might be that the max concurrency is greater than the max expansion,
                    //       but we only throw if we actually try to generate more jobs than the max expansion.
                    throw new MaxJobExpansionException(PipelineStrings.PhaseJobSlicingExpansionExceedLimit(jobContexts.Count.ToString(), context.ExecutionOptions.MaxJobExpansion));
                }
            }

            if (this.Matrix != null)
            {
                var matrixValue = context.Evaluate(nameof(Matrix), this.Matrix, null, traceDefault: false).Value;
                var numberOfConfigurations = matrixValue?.Count ?? 0;
                if (numberOfConfigurations > 0)
                {
                    var positionInPhase = 1;
                    foreach (var pair in matrixValue)
                    {
                        // user-provided configuration key
                        var configuration = pair.Key;
                        var refName = configuration;
                        if (!PipelineUtilities.IsLegalNodeName(refName))
                        {
                            var legalNodeName = PipelineConstants.DefaultJobDisplayName + positionInPhase.ToString();
                            context.Trace?.Info($"\"{refName}\" is not a legal node name; node will be named \"{legalNodeName}\".");
                            if (context.ExecutionOptions.EnforceLegalNodeNames)
                            {
                                refName = legalNodeName;
                            }
                        }

                        if (options == null || options.IsIncluded(refName))
                        {
                            GenerateContext(
                                displayName: Phase.GenerateDisplayName(context.Phase.Definition, configuration),
                                configuration: refName,
                                configurationVariables: pair.Value,
                                parallelExecutionType: "MultiConfiguration",
                                positionInPhase: positionInPhase,
                                totalJobsInPhase: numberOfConfigurations);
                        }

                        ++positionInPhase;
                    }
                }
            }
            else if (this.MaxConcurrency is var maxConcurrencyPipelineValue && maxConcurrencyPipelineValue != null)
            {
                var maxConcurrency = context.Evaluate(nameof(maxConcurrencyPipelineValue), maxConcurrencyPipelineValue, 1).Value;

                //If the value of context.ExecutionOptions.MaxParallelism is set, we will enforce MaxConcurrency value to be not more than context.ExecutionOptions.MaxParallelism.
                //context.ExecutionOptions.MaxParallelism is currently set if the current context is hosted and public, especially to avoid abuse of services.
                if (maxConcurrency > context.ExecutionOptions.MaxParallelism)
                {
                    maxConcurrency = context.ExecutionOptions.MaxParallelism.Value;
                }

                if (maxConcurrency > 1)
                {
                    if (options == null || options.Configurations == null || options.Configurations.Count == 0)
                    {
                        // generate all slices
                        for (var positionInPhase = 1; positionInPhase <= maxConcurrency; ++positionInPhase)
                        {
                            // NOTE: for historical reasons, the reference name of a slice is "Job" plus the slice number: "Job1"
                            var positionInPhaseString = positionInPhase.ToString();
                            GenerateContext(
                                displayName: Phase.GenerateDisplayName(context.Phase.Definition, positionInPhaseString),
                                configuration: PipelineConstants.DefaultJobDisplayName + positionInPhaseString,
                                configurationVariables: null,
                                parallelExecutionType: "MultiMachine",
                                positionInPhase: positionInPhase,
                                totalJobsInPhase: maxConcurrency);
                        }
                    }
                    else
                    {
                        // generate only the requested slices
                        foreach (var configuration in options.Configurations.Keys)
                        {
                            // determine which slices are required by parsing the configuration name (see generation code above)
                            var prefix = PipelineConstants.DefaultJobDisplayName;
                            if (!configuration.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                             || !int.TryParse(configuration.Substring(prefix.Length), out var positionInPhase))
                                throw new PipelineValidationException(PipelineStrings.PipelineNotValid());

                            GenerateContext(
                                displayName: Phase.GenerateDisplayName(context.Phase.Definition, positionInPhase.ToString()),
                                configuration: configuration,
                                configurationVariables: null,
                                parallelExecutionType: "MultiMachine",
                                positionInPhase: positionInPhase,
                                totalJobsInPhase: maxConcurrency);
                        }
                    }
                }
            }

            // if no contexts are produced otherwise, create a default context.
            if (jobContexts.Count == 0)
            {
                var configuration = PipelineConstants.DefaultJobName;
                if (options == null || options.IsIncluded(configuration))
                {
                    // the default display name is just the JobFactory display name
                    GenerateContext(
                        displayName: Phase.GenerateDisplayName(context.Phase.Definition),
                        configuration: configuration);
                }
            }

            return jobContexts;
        }
    }
}
