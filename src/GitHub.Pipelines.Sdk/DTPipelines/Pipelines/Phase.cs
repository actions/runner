using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Phase : PhaseNode, IJobFactory
    {
        public Phase()
        {
        }

        private Phase(Phase phaseToCopy)
            : base(phaseToCopy)
        {
            if (phaseToCopy.m_steps != null && phaseToCopy.m_steps.Count > 0)
            {
                m_steps = new List<Step>(phaseToCopy.m_steps.Select(x => x.Clone()));
            }
        }

        /// <summary>
        /// Gets the phase type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public override PhaseType Type => PhaseType.Phase;

        /// <summary>
        /// Gets the list of steps associated with this phase. At runtime the steps will be used as a template for
        /// the execution of a job.
        /// </summary>
        public IList<Step> Steps
        {
            get
            {
                if (m_steps == null)
                {
                    m_steps = new List<Step>();
                }
                return m_steps;
            }
        }

        /// <summary>
        /// Creates the specified job using the provided execution context and name. A new execution context is
        /// returned which includes new variables set by the job.
        /// </summary>
        /// <param name="context">The execution context</param>
        /// <param name="name">The name of the job which should be created</param>
        /// <returns>A job and execution context if the specified name exists; otherwise, null</returns>
        public JobExecutionContext CreateJobContext(
            PhaseExecutionContext context,
            String name,
            Int32 attempt)
        {
            ArgumentUtility.CheckForNull(this.Target, nameof(this.Target));

            // Create a copy of the context so the same root context may be used to create multiple jobs
            // without impacting the input context.
            return this.Target.CreateJobContext(context, name, attempt, this);
        }

        /// <summary>
        /// Creates a job context using the provided phase context and existing job instance. A new context is
        /// returned which includes new variables set by the job.
        /// </summary>
        /// <param name="context">The execution context</param>
        /// <param name="jobInstance">The existing job instance</param>
        /// <returns>A job execution context</returns>
        public JobExecutionContext CreateJobContext(
            PhaseExecutionContext context,
            JobInstance jobInstance)
        {
            var jobContext = context.CreateJobContext(jobInstance);
            jobContext.Job.Definition.Id = jobContext.GetInstanceId();

            var options = new BuildOptions();
            var builder = new PipelineBuilder(context);
            var result = builder.GetReferenceResources(jobInstance.Definition.Steps.OfType<Step>().ToList(), jobInstance.Definition.Target);
            jobContext.ReferencedResources.MergeWith(result);

            // Update the execution context with referenced job containers
            var containerAlias = jobInstance.Definition.Container;
            if (!String.IsNullOrEmpty(containerAlias))
            {
                UpdateJobContextReferencedContainers(jobContext, containerAlias);
            }
            var sidecarContainers = jobInstance.Definition.SidecarContainers;
            if (sidecarContainers != null)
            {
                foreach (var sidecar in sidecarContainers)
                {
                    // Sidecar is serviceName -> containerAlias, e.g. ngnix: containerAlias
                    UpdateJobContextReferencedContainers(jobContext, sidecar.Value);
                }
            }
            // Update the execution context with the job-specific system variables
            UpdateJobContextVariablesFromJob(jobContext, jobInstance.Definition);

            return jobContext;
        }

        /// <summary>
        /// Expands the template using the provided execution context and returns the list of jobs.
        /// </summary>
        /// <param name="context">The execution context</param>
        /// <param name="options">The expansion options to use</param>
        /// <returns>A list of jobs which should be executed for this phase</returns>
        public ExpandPhaseResult Expand(
            PhaseExecutionContext context,
            JobExpansionOptions options = null)
        {
            ArgumentUtility.CheckForNull(this.Target, nameof(this.Target));

            var result = this.Target.Expand(context, this, options);
            if (result != null)
            {
                var runtimeValue = this.ContinueOnError?.GetValue(context);
                result.ContinueOnError = runtimeValue?.Value ?? false;
            }

            return result;
        }

        internal static String GetErrorMessage(
            String code,
            params Object[] values)
        {
            var stageName = (String)values[0];
            if (String.IsNullOrEmpty(stageName) ||
                stageName.Equals(PipelineConstants.DefaultJobName, StringComparison.OrdinalIgnoreCase))
            {
                switch (code)
                {
                    case PipelineConstants.NameInvalid:
                        return PipelineStrings.PhaseNameInvalid(values[1]);

                    case PipelineConstants.NameNotUnique:
                        return PipelineStrings.PhaseNamesMustBeUnique(values[1]);

                    case PipelineConstants.StartingPointNotFound:
                        return PipelineStrings.PipelineNotValidNoStartingPhase();

                    case PipelineConstants.DependencyNotFound:
                        return PipelineStrings.PhaseDependencyNotFound(values[1], values[2]);

                    case PipelineConstants.GraphContainsCycle:
                        return PipelineStrings.PhaseGraphCycleDetected(values[1], values[2]);
                }
            }
            else
            {
                switch (code)
                {
                    case PipelineConstants.NameInvalid:
                        return PipelineStrings.StagePhaseNameInvalid(values[0], values[1]);

                    case PipelineConstants.NameNotUnique:
                        return PipelineStrings.StagePhaseNamesMustBeUnique(values[0], values[1]);

                    case PipelineConstants.StartingPointNotFound:
                        return PipelineStrings.StageNotValidNoStartingPhase(values[0]);

                    case PipelineConstants.DependencyNotFound:
                        return PipelineStrings.StagePhaseDependencyNotFound(values[0], values[1], values[2]);

                    case PipelineConstants.GraphContainsCycle:
                        return PipelineStrings.StagePhaseGraphCycleDetected(values[0], values[1], values[2]);
                }
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// Resolves external references and ensures the steps are compatible with the selected target.
        /// </summary>
        /// <param name="context">The validation context</param>
        public override void Validate(
            PipelineBuildContext context,
            ValidationResult result)
        {
            base.Validate(context, result);

            StepValidationResult phaseStepValidationResult = new StepValidationResult();
            // Require the latest agent version.
            if (context.BuildOptions.DemandLatestAgent)
            {
                var latestPackageVersion = context.PackageStore?.GetLatestVersion(WellKnownPackageTypes.Agent);
                if (latestPackageVersion == null)
                {
                    throw new NotSupportedException("Unable to determine the latest agent package version");
                }

                phaseStepValidationResult.MinAgentVersion = latestPackageVersion.ToString();
            }

            if (context.EnvironmentVersion < 2)
            {
                // environment version 1 should has at most 1 checkout step, the position of the checkout task might not be the fisrt one of there is an Azure keyvault task
                var checkoutStep = this.Steps.SingleOrDefault(x => x.IsCheckoutTask());
                if (checkoutStep != null)
                {
                    if ((checkoutStep as TaskStep).Inputs[PipelineConstants.CheckoutTaskInputs.Repository] == PipelineConstants.NoneAlias)
                    {
                        this.Variables.Add(new Variable() { Name = "agent.source.skip", Value = Boolean.TrueString });
                    }

                    this.Steps.Remove(checkoutStep);
                }
            }

            ValidateSteps(context, this, Target, result, this.Steps, phaseStepValidationResult);

            // Resolve the target to ensure we have stable identifiers for the orchestration engine
            // phase targets with expressions need to be evaluated against resolved job contexts.
            bool validateTarget = false;
            if (this.Target.Type == PhaseTargetType.Pool || this.Target.Type == PhaseTargetType.Server)
            {
                validateTarget = true;
            }
            else if (this.Target is AgentQueueTarget agentQueueTarget && agentQueueTarget.IsLiteral())
            {
                validateTarget = true;
            }

            if (validateTarget)
            {
                this.Target.Validate(
                    context,
                    context.BuildOptions,
                    result,
                    this.Steps,
                    phaseStepValidationResult.TaskDemands);
            }
        }

        // todo: merge JobFactory.cs and Phase.cs and then make this private
        internal static void ValidateSteps(
            PipelineBuildContext context,
            PhaseNode phase,
            PhaseTarget phaseTarget,
            ValidationResult result,
            IList<Step> steps,
            StepValidationResult phaseStepValidationResult)
        {
            var stepsCopy = new List<Step>();
            foreach (var step in steps)
            {
                // Skip if not enabled on the definition.
                if (!step.Enabled)
                {
                    continue;
                }

                if (step.Type == StepType.Task)
                {
                    var taskErrors = ValidateTaskStep(context, phase, phaseTarget, result.ReferencedResources, result.UnauthorizedResources, (step as TaskStep), phaseStepValidationResult);
                    if (taskErrors.Count == 0)
                    {
                        stepsCopy.Add(step);
                    }
                    else
                    {
                        result.Errors.AddRange(taskErrors);
                    }
                }
                else if (step.Type == StepType.Group)
                {
                    var groupErrors = ValidateGroupStep(context, phase, phaseTarget, result.ReferencedResources, result.UnauthorizedResources, (step as GroupStep), phaseStepValidationResult);
                    if (groupErrors.Count == 0)
                    {
                        stepsCopy.Add(step);
                    }
                    else
                    {
                        result.Errors.AddRange(groupErrors);
                    }
                }
                else if (step.Type == StepType.Action)
                {
                    var actionErrors = ValidateActionStep(context, phase, step as ActionStep, phaseStepValidationResult);
                    if (actionErrors.Count == 0)
                    {
                        stepsCopy.Add(step);
                    }
                    else
                    {
                        result.Errors.AddRange(actionErrors);
                    }
                }
                else
                {
                    result.Errors.Add(new PipelineValidationError(PipelineStrings.StepNotSupported()));
                }
            }

            // Now replace the steps list with our updated list based on disabled/missing tasks
            steps.Clear();
            steps.AddRange(stepsCopy);

            // Now go through any tasks which did not have a name specified and name them according to how many
            // of that specific task is present.
            if (phaseStepValidationResult.UnnamedSteps.Count > 0)
            {
                GenerateDefaultTaskNames(phaseStepValidationResult.KnownNames, phaseStepValidationResult.UnnamedSteps);
            }

            // Make sure our computed minimum agent version is included with the task demands
            if (phaseStepValidationResult.MinAgentVersion != null)
            {
                phaseStepValidationResult.TaskDemands.Add(new DemandMinimumVersion(PipelineConstants.AgentVersionDemandName, phaseStepValidationResult.MinAgentVersion));
            }
        }

        private static List<PipelineValidationError> ValidateActionStep(
            PipelineBuildContext context,
            PhaseNode phase,
            ActionStep actionStep,
            StepValidationResult stepValidationResult)
        {
            List<PipelineValidationError> actionErrors = new List<PipelineValidationError>();

            // We need an action reference to a contianer image or repository
            if (actionStep.Reference == null)
            {
                // Stop checking further since we can't even find an action definition
                actionErrors.Add(new PipelineValidationError(PipelineStrings.StepActionReferenceInvalid(phase.Name, actionStep.Name)));
                return actionErrors;
            }

            string defaultActionName = "";
            if (actionStep.Reference.Type == ActionSourceType.ContainerRegistry)
            {
                // action is reference to an image from container registry
                var containerAction = actionStep.Reference as ContainerRegistryReference;
                defaultActionName = NameValidation.Sanitize(containerAction.Image, context.BuildOptions.AllowHyphenNames);
            }
            else if (actionStep.Reference.Type == ActionSourceType.Repository)
            {
                // action is reference to dockerfile or action.js from a git repository
                var repoAction = actionStep.Reference as RepositoryPathReference;
                defaultActionName = NameValidation.Sanitize(repoAction.Name ?? PipelineConstants.SelfAlias, context.BuildOptions.AllowHyphenNames);
            }
            else if (actionStep.Reference.Type == ActionSourceType.AgentPlugin)
            {
                var pluginAction = actionStep.Reference as PluginReference;
                defaultActionName = NameValidation.Sanitize(pluginAction.Plugin);
            }
            else if (actionStep.Reference.Type == ActionSourceType.Script)
            {
                defaultActionName = "run";
            }
            else
            {
                actionErrors.Add(new PipelineValidationError(PipelineStrings.TaskStepReferenceInvalid(phase.Name, actionStep.Name, actionStep.Reference.Type)));
            }

            // Validate task name
            var stepNameError = ValidateStepName(context, phase, stepValidationResult, actionStep, defaultActionName);
            if (stepNameError != null)
            {
                actionErrors.Add(stepNameError);
            }

            return actionErrors;
        }

        private static List<PipelineValidationError> ValidateTaskStep(
            PipelineBuildContext context,
            PhaseNode phase,
            PhaseTarget phaseTarget,
            PipelineResources referencedResources,
            PipelineResources unauthorizedResources,
            TaskStep taskStep,
            StepValidationResult stepValidationResult)
        {
            List<PipelineValidationError> taskErrors = new List<PipelineValidationError>();

            // We need either a task name or an identifier and a version.
            if (taskStep.Reference == null ||
                taskStep.Reference.Version == null ||
                (taskStep.Reference.Id == Guid.Empty && String.IsNullOrEmpty(taskStep.Reference.Name)))
            {
                // Stop checking further since we can't even resolve task definition
                taskErrors.Add(new PipelineValidationError(PipelineStrings.StepTaskReferenceInvalid(phase.Name, taskStep.Name)));
                return taskErrors;
            }

            // Try to resolve by the identifier first, then by name
            TaskDefinition resolvedTask = null;
            try
            {
                if (taskStep.Reference.Id != Guid.Empty)
                {
                    resolvedTask = context.TaskStore?.ResolveTask(taskStep.Reference.Id, taskStep.Reference.Version);
                }
                else if (!String.IsNullOrEmpty(taskStep.Reference.Name))
                {
                    resolvedTask = context.TaskStore?.ResolveTask(taskStep.Reference.Name, taskStep.Reference.Version);
                }
            }
            catch (AmbiguousTaskSpecificationException ex)
            {
                // Stop checking further since we can't even resolve task definition
                taskErrors.Add(new PipelineValidationError(PipelineStrings.TaskStepReferenceInvalid(phase.Name, taskStep.Name, ex.Message)));
                return taskErrors;
            }

            // Make sure we were able to find the task with the provided reference data
            if (resolvedTask == null || resolvedTask.Disabled)
            {
                // Stop checking further since we can't even resolve task definition
                String name = taskStep.Reference.Id != Guid.Empty ? taskStep.Reference.Id.ToString() : taskStep.Reference.Name;
                taskErrors.Add(new PipelineValidationError(PipelineStrings.TaskMissing(phase.Name, taskStep.Name, name, taskStep.Reference.Version)));
                return taskErrors;
            }

            // Make sure this step is compatible with the target used by this phase
            if (phaseTarget.IsValid(resolvedTask) == false)
            {
                // Stop checking further since the task is not for valid for the target
                taskErrors.Add(new PipelineValidationError(PipelineStrings.TaskInvalidForGivenTarget(phase.Name, taskStep.Name, resolvedTask.Name, resolvedTask.Version)));
                return taskErrors;
            }

            // Resolve the task version to pin a given task for the duration of the plan
            taskStep.Reference.Id = resolvedTask.Id;
            taskStep.Reference.Name = resolvedTask.Name;
            taskStep.Reference.Version = resolvedTask.Version;

            // Make sure that we have valid syntax for a condition statement
            var conditionError = ValidateStepCondition(context, phase, taskStep.Name, taskStep.Condition);
            if (conditionError != null)
            {
                taskErrors.Add(conditionError);
            }

            // Resolves values from inputs based on the provided validation options
            var inputErrors = ResolveInputs(context, phase, referencedResources, unauthorizedResources, taskStep, resolvedTask);
            if (inputErrors.Count > 0)
            {
                taskErrors.AddRange(inputErrors);
            }

            // Task names do not have to correspond to the same rules as reference names, so we need to remove
            // any characters which are considered invalid for a reference name from the task definition name.
            var defaultTaskName = NameValidation.Sanitize(taskStep.Reference.Name, context.BuildOptions.AllowHyphenNames);

            // Validate task name
            var stepNameError = ValidateStepName(context, phase, stepValidationResult, taskStep, defaultTaskName);
            if (stepNameError != null)
            {
                taskErrors.Add(stepNameError);
            }

            // Now union any demand which are satisifed by tasks within the job
            stepValidationResult.TasksSatisfy.UnionWith(resolvedTask.Satisfies);

            stepValidationResult.MinAgentVersion = resolvedTask.GetMinimumAgentVersion(stepValidationResult.MinAgentVersion);

            // Add demands from task
            var unsatisfiedDemands = resolvedTask.Demands.Where(d => !stepValidationResult.TasksSatisfy.Contains(d.Name));
            if (unsatisfiedDemands.Any())
            {
                stepValidationResult.TaskDemands.UnionWith(unsatisfiedDemands);
            }

            return taskErrors;
        }

        private static List<PipelineValidationError> ValidateGroupStep(
            PipelineBuildContext context,
            PhaseNode phase,
            PhaseTarget phaseTarget,
            PipelineResources referencedResources,
            PipelineResources unauthorizedResources,
            GroupStep groupStep,
            StepValidationResult stepValidationResult)
        {
            List<PipelineValidationError> groupErrors = new List<PipelineValidationError>();

            // Make sure that we have valid syntax for a condition statement
            var conditionError = ValidateStepCondition(context, phase, groupStep.Name, groupStep.Condition);
            if (conditionError != null)
            {
                groupErrors.Add(conditionError);
            }

            // ValidationResult for steps within group, since only steps within a group need to have unique task.name
            StepValidationResult groupStepsValidationResult = new StepValidationResult();

            var stepsCopy = new List<TaskStep>();
            foreach (var step in groupStep.Steps)
            {
                // Skip if not enabled on the definition.
                if (!step.Enabled)
                {
                    continue;
                }

                var taskErrors = ValidateTaskStep(context, phase, phaseTarget, referencedResources, unauthorizedResources, step, groupStepsValidationResult);
                if (taskErrors.Count == 0)
                {
                    stepsCopy.Add(step);
                }
                else
                {
                    groupErrors.AddRange(taskErrors);
                }
            }

            // Now replace the steps list with our updated list based on disabled/missing tasks
            groupStep.Steps.Clear();
            groupStep.Steps.AddRange(stepsCopy);

            // Merge group steps validation result
            if (groupStepsValidationResult.UnnamedSteps.Count > 0)
            {
                // Now go through any tasks within a group which did not have a name specified and name them according to how many
                // of that specific task is present.
                GenerateDefaultTaskNames(groupStepsValidationResult.KnownNames, groupStepsValidationResult.UnnamedSteps);
            }

            // If group min agent version > current min agent version
            if (DemandMinimumVersion.CompareVersion(groupStepsValidationResult.MinAgentVersion, stepValidationResult.MinAgentVersion) > 0)
            {
                stepValidationResult.MinAgentVersion = groupStepsValidationResult.MinAgentVersion;
            }

            // Add tasks satisfies provided by the group
            stepValidationResult.TasksSatisfy.UnionWith(groupStepsValidationResult.TasksSatisfy);

            // Add demands come from tasks within the group
            var unsatisfiedDemands = groupStepsValidationResult.TaskDemands.Where(d => !stepValidationResult.TasksSatisfy.Contains(d.Name));
            if (unsatisfiedDemands.Any())
            {
                stepValidationResult.TaskDemands.UnionWith(unsatisfiedDemands);
            }

            // Validate group name
            var stepNameError = ValidateStepName(context, phase, stepValidationResult, groupStep, "group");
            if (stepNameError != null)
            {
                groupErrors.Add(stepNameError);
            }

            return groupErrors;
        }

        private static PipelineValidationError ValidateStepName(
            PipelineBuildContext context,
            PhaseNode phase,
            StepValidationResult stepValidationResult,
            JobStep step,
            String defaultName)
        {
            if (String.IsNullOrEmpty(step.Name))
            {
                List<Step> stepsToName;
                if (!stepValidationResult.UnnamedSteps.TryGetValue(defaultName, out stepsToName))
                {
                    stepsToName = new List<Step>();
                    stepValidationResult.UnnamedSteps.Add(defaultName, stepsToName);
                }

                stepsToName.Add(step);

                if (String.IsNullOrEmpty(step.DisplayName))
                {
                    step.DisplayName = defaultName;
                }
            }
            else
            {
                bool nameIsValid = NameValidation.IsValid(step.Name, context.BuildOptions.AllowHyphenNames);
                if (!nameIsValid)
                {
                    if (context.BuildOptions.ValidateStepNames)
                    {
                        return new PipelineValidationError(PipelineStrings.StepNameInvalid(phase.Name, step.Name));
                    }
                    else
                    {
                        var sanitizedName = NameValidation.Sanitize(step.Name, context.BuildOptions.AllowHyphenNames);
                        if (String.IsNullOrEmpty(sanitizedName))
                        {
                            sanitizedName = defaultName;
                        }

                        step.Name = sanitizedName;
                        nameIsValid = true;
                    }
                }

                if (nameIsValid && !stepValidationResult.KnownNames.Add(step.Name))
                {
                    if (context.BuildOptions.ValidateStepNames)
                    {
                        return new PipelineValidationError(PipelineStrings.StepNamesMustBeUnique(phase.Name, step.Name));
                    }
                    else
                    {
                        List<Step> stepsToName;
                        if (!stepValidationResult.UnnamedSteps.TryGetValue(step.Name, out stepsToName))
                        {
                            stepsToName = new List<Step>();
                            stepValidationResult.UnnamedSteps.Add(step.Name, stepsToName);
                        }

                        stepsToName.Add(step);
                    }
                }

                // If the name was specified but the display name is empty, default the display name to the name
                if (String.IsNullOrEmpty(step.DisplayName))
                {
                    step.DisplayName = step.Name;
                }
            }

            return null;
        }

        private static PipelineValidationError ValidateStepCondition(
            PipelineBuildContext context,
            PhaseNode phase,
            String stepName,
            String stepCondition)
        {
            if (!String.IsNullOrEmpty(stepCondition))
            {
                try
                {
                    var parser = new ExpressionParser();
                    parser.ValidateSyntax(stepCondition, context.Trace);
                }
                catch (ParseException ex)
                {
                    return new PipelineValidationError(PipelineStrings.StepConditionIsNotValid(phase.Name, stepName, stepCondition, ex.Message));
                }
            }

            return null;
        }

        private static void GenerateDefaultTaskNames(
            ISet<String> knownNames,
            IDictionary<String, List<Step>> unnamedTasks)
        {
            foreach (var unnamedTasksByName in unnamedTasks)
            {
                if (unnamedTasksByName.Value.Count == 1 && knownNames.Add(unnamedTasksByName.Key))
                {
                    unnamedTasksByName.Value[0].Name = unnamedTasksByName.Key;
                }
                else
                {
                    Int32 taskCounter = 1;
                    foreach (var unnamedTask in unnamedTasksByName.Value)
                    {
                        var candidateName = $"{unnamedTasksByName.Key}{taskCounter}";
                        while (!knownNames.Add(candidateName))
                        {
                            taskCounter++;
                            candidateName = $"{unnamedTasksByName.Key}{taskCounter}";
                        }

                        taskCounter++;
                        unnamedTask.Name = candidateName;
                    }
                }
            }
        }

        private static IList<PipelineValidationError> ResolveInputs(
            PipelineBuildContext context,
            PhaseNode phase,
            PipelineResources referencedResources,
            PipelineResources unauthorizedResources,
            TaskStep step,
            TaskDefinition taskDefinition)
        {
            IList<PipelineValidationError> errors = new List<PipelineValidationError>();
            foreach (var input in taskDefinition.Inputs)
            {
                // Resolve by alias
                var inputAlias = ResolveAlias(context, step, input);

                // If the input isn't set then there is nothing else to do here
                if (!step.Inputs.TryGetValue(input.Name, out String inputValue))
                {
                    continue;
                }

                // If the caller requested input validation and the input provides a validation section then we
                // should do a best-effort validation based on what is available in the environment.
                errors.AddRange(ValidateInput(context, phase, step, input, inputAlias, inputValue));

                // Now resolve any resources referenced by task inputs
                errors.AddRange(ResolveResources(context, phase, context.BuildOptions, referencedResources, unauthorizedResources, step, input, inputAlias, inputValue, throwOnFailure: false));
            }

            return errors;
        }

        private static String ResolveAlias(
            PipelineBuildContext context,
            TaskStep step,
            TaskInputDefinition input)
        {
            var specifiedName = input.Name;
            if (context.BuildOptions.ResolveTaskInputAliases && !step.Inputs.ContainsKey(input.Name))
            {
                foreach (String alias in input.Aliases)
                {
                    if (step.Inputs.TryGetValue(alias, out String aliasValue))
                    {
                        specifiedName = alias;
                        step.Inputs.Remove(alias);
                        step.Inputs.Add(input.Name, aliasValue);
                        break;
                    }
                }
            }
            return specifiedName;
        }

        private static IEnumerable<PipelineValidationError> ResolveResources(
            IPipelineContext context,
            PhaseNode phase,
            BuildOptions options,
            PipelineResources referencedResources,
            PipelineResources unauthorizedResources,
            TaskStep step,
            TaskInputDefinition input,
            String inputAlias,
            String inputValue,
            Boolean throwOnFailure = false)
        {
            if (String.IsNullOrEmpty(inputValue))
            {
                return Enumerable.Empty<PipelineValidationError>();
            }

            var errors = new List<PipelineValidationError>();
            if (input.InputType.StartsWith(c_endpointInputTypePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var resolvedEndpoints = new List<String>();
                var endpointType = input.InputType.Remove(0, c_endpointInputTypePrefix.Length);
                var resolvedInputValues = inputValue.Split(',').Select(x => x.Trim()).Where(x => !String.IsNullOrEmpty(x));
                foreach (var value in resolvedInputValues)
                {
                    var replacedValue = context.ExpandVariables(value);
                    referencedResources.AddEndpointReference(replacedValue);

                    // Validate the resource using the provided store if desired
                    if (options.ValidateResources)
                    {
                        var endpoint = context.ResourceStore.GetEndpoint(replacedValue);
                        if (endpoint == null)
                        {
                            if (throwOnFailure)
                            {
                                throw new ResourceNotFoundException(PipelineStrings.ServiceEndpointNotFoundForInput(phase.Name, step.Name, inputAlias, replacedValue));
                            }
                            else
                            {
                                resolvedEndpoints.Add(replacedValue);
                                unauthorizedResources?.AddEndpointReference(replacedValue);
                                errors.Add(new PipelineValidationError(PipelineStrings.ServiceEndpointNotFoundForInput(phase.Name, step.Name, inputAlias, replacedValue)));
                            }
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(endpointType))
                            {
                                var endpointTypeSegments = endpointType.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                                if (endpointTypeSegments.Count >= 1)
                                {
                                    var endpointTypeName = endpointTypeSegments[0];
                                    if (!endpointTypeName.Equals(endpoint.Type, StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (throwOnFailure)
                                        {
                                            throw new PipelineValidationException(PipelineStrings.StepInputEndpointTypeMismatch(phase.Name, step.Name, inputAlias, endpointTypeName, endpoint.Name, endpoint.Type));
                                        }
                                        else
                                        {
                                            errors.Add(new PipelineValidationError(PipelineStrings.StepInputEndpointTypeMismatch(phase.Name, step.Name, inputAlias, endpointTypeName, endpoint.Name, endpoint.Type)));
                                        }
                                    }
                                    else if (endpointTypeSegments.Count > 1 && !String.IsNullOrEmpty(endpoint.Authorization?.Scheme))
                                    {
                                        var supportedAuthSchemes = endpointTypeSegments[1]?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                                        if (supportedAuthSchemes?.Count > 0 && !supportedAuthSchemes.Any(x => x.Equals(endpoint.Authorization.Scheme, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            if (throwOnFailure)
                                            {
                                                throw new PipelineValidationException(PipelineStrings.StepInputEndpointAuthSchemeMismatch(phase.Name, step.Name, inputAlias, endpointTypeName, endpointTypeSegments[1], endpoint.Name, endpoint.Type, endpoint.Authorization.Scheme));
                                            }
                                            else
                                            {
                                                errors.Add(new PipelineValidationError(PipelineStrings.StepInputEndpointAuthSchemeMismatch(phase.Name, step.Name, inputAlias, endpointTypeName, endpointTypeSegments[1], endpoint.Name, endpoint.Type, endpoint.Authorization.Scheme)));
                                            }
                                        }
                                    }
                                }
                            }

                            resolvedEndpoints.Add(endpoint.Id.ToString("D"));
                        }
                    }
                    else
                    {
                        // Always add the value back so we can update the input below.
                        resolvedEndpoints.Add(replacedValue);
                    }
                }

                step.Inputs[input.Name] = String.Join(",", resolvedEndpoints);
            }
            else if (input.InputType.Equals(c_secureFileInputType, StringComparison.OrdinalIgnoreCase))
            {
                var resolvedFiles = new List<String>();
                var resolvedInputValues = inputValue.Split(',').Select(x => x.Trim()).Where(x => !String.IsNullOrEmpty(x));
                foreach (var value in resolvedInputValues)
                {
                    var replacedValue = context.ExpandVariables(value);
                    referencedResources.AddSecureFileReference(replacedValue);

                    // Validate the resource using the provided store if desired
                    if (options.ValidateResources)
                    {
                        var secureFile = context.ResourceStore.GetFile(replacedValue);
                        if (secureFile == null)
                        {
                            if (throwOnFailure)
                            {
                                throw new ResourceNotFoundException(PipelineStrings.SecureFileNotFoundForInput(phase.Name, step.Name, inputAlias, replacedValue));
                            }
                            else
                            {
                                resolvedFiles.Add(replacedValue);
                                unauthorizedResources?.AddSecureFileReference(replacedValue);
                                errors.Add(new PipelineValidationError(PipelineStrings.SecureFileNotFoundForInput(phase.Name, step.Name, inputAlias, replacedValue)));
                            }
                        }
                        else
                        {
                            resolvedFiles.Add(secureFile.Id.ToString("D"));
                        }
                    }
                    else
                    {
                        // Always add the value back so we can update the input below.
                        resolvedFiles.Add(replacedValue);
                    }
                }

                step.Inputs[input.Name] = String.Join(",", resolvedFiles);
            }
            else if (input.InputType.Equals(TaskInputType.Repository, StringComparison.OrdinalIgnoreCase))
            {
                // Ignore repository alias None
                if (!String.Equals(inputValue, PipelineConstants.NoneAlias))
                {
                    var repository = context.ResourceStore.Repositories.Get(inputValue);
                    if (repository == null)
                    {
                        if (options.ValidateResources)
                        {
                            // repository should always be there as full object
                            if (throwOnFailure)
                            {
                                throw new ResourceNotFoundException(PipelineStrings.RepositoryResourceNotFound(inputValue));
                            }
                            else
                            {
                                errors.Add(new PipelineValidationError(PipelineStrings.RepositoryResourceNotFound(inputValue)));
                            }
                        }
                    }
                    else
                    {
                        referencedResources.Repositories.Add(repository);

                        // Add the endpoint
                        if (repository.Endpoint != null)
                        {
                            referencedResources.AddEndpointReference(repository.Endpoint);

                            if (options.ValidateResources)
                            {
                                var repositoryEndpoint = context.ResourceStore.GetEndpoint(repository.Endpoint);
                                if (repositoryEndpoint == null)
                                {
                                    if (throwOnFailure)
                                    {
                                        throw new ResourceNotFoundException(PipelineStrings.ServiceEndpointNotFound(repository.Endpoint));
                                    }
                                    else
                                    {
                                        unauthorizedResources?.AddEndpointReference(repository.Endpoint);
                                        errors.Add(new PipelineValidationError(PipelineStrings.ServiceEndpointNotFound(repository.Endpoint)));
                                    }
                                }
                                else
                                {
                                    repository.Endpoint = new ServiceEndpointReference() { Id = repositoryEndpoint.Id };
                                }
                            }
                        }
                    }
                }
                else
                {
                    // always add self repo with checkout: none
                    var selfRepository = context.ResourceStore.Repositories.Get(PipelineConstants.SelfAlias);
                    if (selfRepository != null)
                    {
                        referencedResources.Repositories.Add(selfRepository);
                        if (selfRepository.Endpoint != null)
                        {
                            referencedResources.AddEndpointReference(selfRepository.Endpoint);
                            if (options.ValidateResources)
                            {
                                var repositoryEndpoint = context.ResourceStore.GetEndpoint(selfRepository.Endpoint);
                                if (repositoryEndpoint == null)
                                {
                                    if (throwOnFailure)
                                    {
                                        throw new ResourceNotFoundException(PipelineStrings.ServiceEndpointNotFound(selfRepository.Endpoint));
                                    }
                                    else
                                    {
                                        unauthorizedResources?.AddEndpointReference(selfRepository.Endpoint);
                                        errors.Add(new PipelineValidationError(PipelineStrings.ServiceEndpointNotFound(selfRepository.Endpoint)));
                                    }
                                }
                                else
                                {
                                    selfRepository.Endpoint = new ServiceEndpointReference() { Id = repositoryEndpoint.Id };
                                }
                            }
                        }
                    }
                }
            }

            return errors;
        }

        private String ResolveContainerResource(JobExecutionContext context, String inputAlias)
        {
            var outputAlias = inputAlias;
            // Check if container is an image spec, not an alias
            if (inputAlias.Contains(":"))
            {
                var resource = context.ResourceStore?.Containers.GetAll().FirstOrDefault(x =>
                    x.Endpoint == null &&
                    x.Properties.Count == 1 &&
                    String.Equals(x.Image, inputAlias, StringComparison.Ordinal));
                if (resource == null)
                {
                    resource = new ContainerResource
                    {
                        Alias = Guid.NewGuid().ToString("N"),
                        Image = inputAlias,
                    };
                    context.ResourceStore?.Containers.Add(resource);
                }
                outputAlias = resource.Alias;
            }

            return outputAlias;
        }

        private void UpdateJobContextReferencedContainers(JobExecutionContext context, string containerAlias)
        {
            // Look up the container by alias, and add dereferenced container to ReferencedResources
            var containerResource = context.ResourceStore?.Containers.Get(containerAlias);
            if (containerResource == null)
            {
                throw new ResourceNotFoundException(PipelineStrings.ContainerResourceNotFound(containerAlias));
            }
            context.ReferencedResources.Containers.Add(containerResource);
            if (containerResource.Endpoint != null)
            {
                context.ReferencedResources.AddEndpointReference(containerResource.Endpoint);
                var serviceEndpoint = context.ResourceStore?.GetEndpoint(containerResource.Endpoint);
                if (serviceEndpoint == null)
                {
                    throw new ResourceNotFoundException(PipelineStrings.ContainerEndpointNotFound(containerAlias, containerResource.Endpoint));
                }
            }
        }

        private static IEnumerable<PipelineValidationError> ValidateInput(
            PipelineBuildContext context,
            PhaseNode phase,
            TaskStep step,
            TaskInputDefinition input,
            String inputAlias,
            String value)
        {
            if (!context.BuildOptions.ValidateTaskInputs || input.Validation == null)
            {
                return Enumerable.Empty<PipelineValidationError>();
            }

            // We cannot perform useful validation if the value didn't expand, it may not be populated until it
            // executes on the target. If we still have variables we just let it go through optimistically.
            var expandedInputValue = context.ExpandVariables(value);
            if (VariableUtility.IsVariable(expandedInputValue))
            {
                return Enumerable.Empty<PipelineValidationError>();
            }

            var inputContext = new InputValidationContext
            {
                Evaluate = true,
                EvaluationOptions = context.ExpressionOptions,
                Expression = input.Validation.Expression,
                SecretMasker = context.SecretMasker,
                TraceWriter = context.Trace,
                Value = expandedInputValue,
            };

            // Make sure to track any input validation errors encountered
            var validationResult = context.InputValidator.Validate(inputContext);
            if (validationResult.IsValid)
            {
                return Enumerable.Empty<PipelineValidationError>();
            }
            else
            {
                // Make sure we do not expose secrets when logging errors about expanded input values
                var maskedValue = context.SecretMasker.MaskSecrets(expandedInputValue);
                var reason = validationResult.Reason ?? input.Validation.Message;
                return new[] { new PipelineValidationError(PipelineStrings.StepTaskInputInvalid(phase.Name, step.Name, inputAlias, maskedValue, inputContext.Expression, reason)) };
            }
        }

        /// <summary>
        /// Produces the official display name for this job.
        /// Optionally include any of the path components you want to consider.
        /// </summary>
        internal static String GenerateDisplayName(
            Stage stage = null,
            PhaseNode phase = null,
            Job job = null)
        {
            var stageName = default(string);
            if (stage != null)
            {
                stageName = stage.DisplayName ?? stage.Name;
            }

            var factoryName = default(string);
            if (phase != null)
            {
                factoryName = phase.DisplayName ?? phase.Name;
            }

            var jobName = default(string);
            if (job != null)
            {
                jobName = job.DisplayName ?? job.Name;
            }

            return GenerateDisplayName(stageName, factoryName, jobName);
        }

        /// <summary>
        /// Produces the official display name for this job.
        /// Optionally include any of the path components you want to consider.
        /// </summary>
        internal static String GenerateDisplayName(
            PhaseNode factory,
            String configuration = null)
        {
            var factoryDisplayName = factory == null
                ? String.Empty
                : factory.DisplayName ?? factory.Name;
            return GenerateDisplayName(factoryDisplayName, configuration);
        }

        /// <summary>
        /// Produces the official display name for this job.
        /// Optionally include any of the path components you want to consider.
        /// Removes any occurrence of the default node name (the reference name used for default nodes,
        ///   or when users do not specify any name)
        /// </summary>
        internal static String GenerateDisplayName(params string[] tokens)
        {
            if (tokens == null)
            {
                return string.Empty;
            }

            var defaultNodeName = PipelineConstants.DefaultJobName;
            var l = defaultNodeName.Length;
            var formattedTokens = tokens
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => (x.StartsWith(defaultNodeName) ? x.Substring(l) : x).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x));
            var result = string.Join(" ", formattedTokens);

            return string.IsNullOrWhiteSpace(result)
                ? PipelineConstants.DefaultJobDisplayName
                : result;
        }

        public Job CreateJob(
            JobExecutionContext context,
            ExpressionValue<String> container,
            IDictionary<String, ExpressionValue<String>> sidecarContainers,
            Boolean continueOnError,
            Int32 timeoutInMinutes,
            Int32 cancelTimeoutInMinutes,
            String displayName = null)
        {
            // default display name is based on the phase.
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = Phase.GenerateDisplayName(context.Phase.Definition);
            }

            var job = new Job
            {
                Id = context.GetInstanceId(),
                Name = context.Job.Name,
                DisplayName = displayName,
                ContinueOnError = continueOnError,
                TimeoutInMinutes = timeoutInMinutes,
                CancelTimeoutInMinutes = cancelTimeoutInMinutes
            };

            if (context.ExecutionOptions.EnableResourceExpressions)
            {
                job.Target = GenerateJobSpecificTarget(context);
            }

            if (job.Target == null)
            {
                ArgumentUtility.CheckForNull(this.Target, nameof(this.Target));
                job.Target = this.Target.Clone();
            }

            if (context.EnvironmentVersion > 1)
            {
                // always add self or designer repo to repository list
                RepositoryResource defaultRepo = null;
                var selfRepo = context.ResourceStore?.Repositories.Get(PipelineConstants.SelfAlias);
                if (selfRepo == null)
                {
                    var designerRepo = context.ResourceStore?.Repositories.Get(PipelineConstants.DesignerRepo);
                    if (designerRepo != null)
                    {
                        defaultRepo = designerRepo;
                    }
                    else
                    {
                        Debug.Fail("Repositories are not filled in.");
                    }
                }
                else
                {
                    defaultRepo = selfRepo;
                }

                if (defaultRepo != null)
                {
                    context.ReferencedResources.Repositories.Add(defaultRepo);

                    // Add the endpoint
                    if (defaultRepo.Endpoint != null)
                    {
                        context.ReferencedResources.AddEndpointReference(defaultRepo.Endpoint);
                        var repositoryEndpoint = context.ResourceStore?.GetEndpoint(defaultRepo.Endpoint);
                        if (repositoryEndpoint == null)
                        {
                            throw new ResourceNotFoundException(PipelineStrings.ServiceEndpointNotFound(defaultRepo.Endpoint));
                        }
                    }
                }
            }

            // Expand short-syntax inline-containers, resolve resource references and add to new job and context 
            if (container != null)
            {
                var containerAlias = container.GetValue(context).Value;
                var outputAlias = ResolveContainerResource(context, containerAlias);
                job.Container = outputAlias;
                UpdateJobContextReferencedContainers(context, outputAlias);
            }
            if (sidecarContainers != null)
            {
                foreach (var sidecar in sidecarContainers)
                {
                    var sidecarContainerAlias = sidecar.Value.GetValue(context).Value;
                    var outputAlias = ResolveContainerResource(context, sidecarContainerAlias);
                    job.SidecarContainers.Add(sidecar.Key, outputAlias);
                    UpdateJobContextReferencedContainers(context, outputAlias);
                }
            }

            // Update the execution context with the job-specific system variables
            UpdateJobContextVariablesFromJob(context, job);

            var steps = new List<JobStep>();
            var identifier = context.GetInstanceName();
            foreach (var step in this.Steps)
            {
                if (step.Type == StepType.Task)
                {
                    // We don't need to add to demands here since they are already part of the plan.
                    steps.Add(CreateJobTaskStep(context, this, identifier, step as TaskStep));
                }
                else if (step.Type == StepType.Group)
                {
                    steps.Add(CreateJobStepGroup(context, this, identifier, step as GroupStep));
                }
                else if (step.Type == StepType.Action)
                {
                    steps.Add(CreateJobActionStep(context, identifier, step as ActionStep));
                }
                else
                {
                    // Should never happen.
                    Debug.Fail(step.Type.ToString());
                }
            }

            // TODO: remove the whole concept of step providers. 
            //       this work should happen during compilation. -zacox.
            //
            // This is not ideal but we need to set the job on the context before calling the step providers below.
            // This method returns the job and it gets set after. We need to clean this up when we do refactoring.
            context.Job.Definition = job;

            var stepProviderDemands = new HashSet<Demand>();

            // Add the system-injected tasks before inserting the user tasks. This currently does not handle injecting
            // min agent version demands if appropriate.
            var systemSteps = new List<TaskStep>();
            if (context.StepProviders != null)
            {
                var jobSteps = new ReadOnlyCollection<JobStep>(steps);

                foreach (IStepProvider stepProvider in context.StepProviders)
                {
                    systemSteps.AddRange(stepProvider.GetPreSteps(context, jobSteps));
                }
            }

            if (systemSteps?.Count > 0)
            {
                for (Int32 i = 0; i < systemSteps.Count; i++)
                {
                    systemSteps[i].Name = $"__system_{i + 1}";

                    IList<JobStep> resolvedSteps = new List<JobStep>();
                    if (ResolveTaskStep(context, this, identifier, systemSteps[i], out resolvedSteps, stepProviderDemands))
                    {
                        job.Steps.AddRange(resolvedSteps);
                    }
                    else
                    {
                        job.Steps.Add(CreateJobTaskStep(context, this, identifier, systemSteps[i], stepProviderDemands));
                    }
                }
            }

            // Resolving user steps
            foreach (var step in steps)
            {
                IList<JobStep> resolvedSteps = new List<JobStep>();
                if (ResolveTaskStep(context, this, identifier, step, out resolvedSteps))
                {
                    job.Steps.AddRange(resolvedSteps);
                }
                else
                {
                    job.Steps.Add(step);
                }
            }

            // Add post job steps, if there are any.
            // These are added after the user tasks.
            var postJobSteps = new List<TaskStep>();
            if (context.StepProviders != null)
            {
                var jobSteps = new ReadOnlyCollection<JobStep>(job.Steps);

                foreach (IStepProvider stepProvider in context.StepProviders)
                {
                    postJobSteps.AddRange(stepProvider.GetPostSteps(context, jobSteps));
                }
            }

            if (postJobSteps?.Count > 0)
            {
                for (Int32 i = 0; i < postJobSteps.Count; i++)
                {
                    postJobSteps[i].Name = $"__system_post_{i + 1}";
                    job.Steps.Add(CreateJobTaskStep(context, this, identifier, postJobSteps[i], stepProviderDemands));
                }
            }

            // create unique set of job demands
            AddDemands(context, job, stepProviderDemands);
            AddDemands(context, job, this.Target?.Demands);

            // Copy context variables into job, since job will be saved and read back later before agent job message is sent
            foreach (var variable in context.Variables)
            {
                context.Job.Definition.Variables.Add(new Variable
                {
                    Name = variable.Key,
                    Value = variable.Value.IsSecret ? null : variable.Value.Value,
                    Secret = variable.Value.IsSecret
                });
            }

            return job;
        }

        private void AddDemands(
            JobExecutionContext context,
            Job job,
            ISet<Demand> demands)
        {
            if (context == null || job == null || demands == null)
            {
                return;
            }

            var mergedDemands = job.Demands;
            foreach (var d in demands)
            {
                if (d == null)
                {
                    continue;
                }

                var demandValue = d.Value;
                if (String.IsNullOrEmpty(demandValue))
                {
                    // if a demand has no value, add it
                    mergedDemands.Add(d.Clone());
                }
                else
                {
                    // if a demand has a non-empty value, any problems encountered while evaluating
                    //   macros should be promoted into PipelineValidationExceptions
                    var expandedValue = context.ExpandVariables(demandValue, maskSecrets: true);
                    try
                    {
                        var resolvedDemand = d.Clone();
                        resolvedDemand.Update(expandedValue);
                        mergedDemands.Add(resolvedDemand);
                    }
                    catch (Exception e)
                    {
                        throw new PipelineValidationException(PipelineStrings.DemandExpansionInvalid(d.ToString(), d.Value, expandedValue), e);
                    }
                }
            }
        }

        private Boolean ResolveTaskStep(
            JobExecutionContext context,
            PhaseNode phase,
            String identifier,
            JobStep step,
            out IList<JobStep> resolvedSteps,
            HashSet<Demand> resolvedDemands = null)
        {
            Boolean handled = false;
            IList<TaskStep> resultSteps = new List<TaskStep>();
            resolvedSteps = new List<JobStep>();

            if (context.ResourceStore?.ResolveStep(context, step, out resultSteps) ?? false)
            {
                foreach (TaskStep resultStep in resultSteps)
                {
                    resolvedSteps.Add(CreateJobTaskStep(context, phase, identifier, resultStep, resolvedDemands));
                }

                handled = true;
            }

            return handled;
        }

        /// <summary>
        /// Evaluate runtime expressions
        /// Queue targets are allowed use runtime expressions.
        /// Resolve all expressions and produce a literal QueueTarget for execution.
        /// </summary>
        private AgentQueueTarget GenerateJobSpecificTarget(JobExecutionContext context)
        {
            var phase = context?.Phase?.Definition as Phase;
            if (phase == null)
            {
                return null;
            }

            if (!(phase.Target is AgentQueueTarget agentQueueTarget))
            {
                return null;
            }

            if (agentQueueTarget.IsLiteral())
            {
                return null;
            }

            // create a clone containing only literals and validate referenced resources
            var validationResult = new Validation.ValidationResult();
            var literalTarget = agentQueueTarget.Evaluate(context, validationResult);
            literalTarget?.Validate(
                context: context,
                result: validationResult,
                buildOptions: new BuildOptions
                {
                    EnableResourceExpressions = true,
                    ValidateResources = true,
                    ValidateExpressions = true, // all expressions must resolve
                    AllowEmptyQueueTarget = false
                },
                steps: new List<Step>(),
                taskDemands: new HashSet<Demand>());

            if (validationResult.Errors.Count > 0)
            {
                throw new PipelineValidationException(validationResult.Errors);
            }

            return literalTarget;
        }

        // todo: merge JobFactory.cs and Phase.cs and then make this private
        internal static ActionStep CreateJobActionStep(
            JobExecutionContext context,
            String jobIdentifier,
            ActionStep action)
        {
            var jobStep = (ActionStep)action.Clone();

            // Setup the identifier based on our current context
            var actionIdentifier = context.IdGenerator.GetInstanceName(jobIdentifier, action.Name);
            jobStep.Id = context.IdGenerator.GetInstanceId(actionIdentifier);

            // Update the display name of task steps
            jobStep.DisplayName = jobStep.DisplayName;

            return jobStep;
        }

        // todo: merge JobFactory.cs and Phase.cs and then make this private
        internal static TaskStep CreateJobTaskStep(
            JobExecutionContext context,
            PhaseNode phase,
            String jobIdentifier,
            TaskStep task,
            HashSet<Demand> resolvedDemands = null)
        {
            var jobStep = (TaskStep)task.Clone();

            // Setup the identifier based on our current context
            var taskIdentifier = context.IdGenerator.GetInstanceName(jobIdentifier, task.Name);
            jobStep.Id = context.IdGenerator.GetInstanceId(taskIdentifier);

            // Update the display name of task steps
            jobStep.DisplayName = context.ExpandVariables(jobStep.DisplayName, maskSecrets: true);

            // Now resolve any resources referenced by inputs
            var taskDefinition = context.TaskStore.ResolveTask(jobStep.Reference.Id, jobStep.Reference.Version);
            if (taskDefinition != null)
            {
                foreach (var input in taskDefinition.Inputs)
                {
                    if (task.Inputs.TryGetValue(input.Name, out String value))
                    {
                        ResolveResources(context, phase, BuildOptions.None, context.ReferencedResources, null, jobStep, input, input.Name, value, throwOnFailure: true);
                    }
                }

                // Add demands
                if (resolvedDemands != null)
                {
                    resolvedDemands.AddRange(taskDefinition.Demands);

                    if (!String.IsNullOrEmpty(taskDefinition.MinimumAgentVersion))
                    {
                        resolvedDemands.Add(new DemandMinimumVersion(PipelineConstants.AgentVersionDemandName, taskDefinition.MinimumAgentVersion));
                    }
                }
            }
            else
            {
                throw new TaskDefinitionNotFoundException(PipelineStrings.TaskMissing(phase.Name, jobStep.Name, jobStep.Reference.Id, jobStep.Reference.Version));
            }

            return jobStep;
        }

        // todo: merge JobFactory.cs and Phase.cs and then make this private
        internal static GroupStep CreateJobStepGroup(
            JobExecutionContext context,
            PhaseNode phase,
            String jobIdentifier,
            GroupStep group)
        {
            var groupStep = (GroupStep)group.Clone();

            var groupIdentifier = context.IdGenerator.GetInstanceName(jobIdentifier, group.Name);
            groupStep.Id = context.IdGenerator.GetInstanceId(groupIdentifier);

            // Update the display name of step group
            groupStep.DisplayName = context.ExpandVariables(groupStep.DisplayName, maskSecrets: true);

            // Now resolve every task steps within step group
            var stepsCopy = new List<TaskStep>();
            foreach (var task in groupStep.Steps)
            {
                stepsCopy.Add(CreateJobTaskStep(context, phase, groupIdentifier, task));
            }

            groupStep.Steps.Clear();
            groupStep.Steps.AddRange(stepsCopy);

            return groupStep;
        }

        private void UpdateJobContextVariablesFromJob(JobExecutionContext jobContext, Job job)
        {
            jobContext.Variables[WellKnownDistributedTaskVariables.JobDisplayName] = job.DisplayName;
            jobContext.Variables[WellKnownDistributedTaskVariables.JobId] = job.Id.ToString("D");
            jobContext.Variables[WellKnownDistributedTaskVariables.JobName] = job.Name;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_steps?.Count == 0)
            {
                m_steps = null;
            }
        }

        [DataMember(Name = "Steps", EmitDefaultValue = false)]
        private IList<Step> m_steps;

        private const String c_secureFileInputType = "secureFile";
        private const String c_endpointInputTypePrefix = "connectedService:";

        // todo: merge JobFactory.cs and Phase.cs and then make this private
        internal class StepValidationResult
        {
            public String MinAgentVersion { get; set; }

            public HashSet<Demand> TaskDemands
            {
                get
                {
                    if (m_taskDemands == null)
                    {
                        m_taskDemands = new HashSet<Demand>();
                    }

                    return m_taskDemands;
                }
            }

            public HashSet<String> KnownNames
            {
                get
                {
                    if (m_knownNames == null)
                    {
                        m_knownNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                    }

                    return m_knownNames;
                }
            }

            public HashSet<String> TasksSatisfy
            {
                get
                {
                    if (m_tasksSatisfy == null)
                    {
                        m_tasksSatisfy = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                    }

                    return m_tasksSatisfy;
                }
            }

            public Dictionary<String, List<Step>> UnnamedSteps
            {
                get
                {
                    if (m_unnamedSteps == null)
                    {
                        m_unnamedSteps = new Dictionary<String, List<Step>>(StringComparer.OrdinalIgnoreCase);
                    }

                    return m_unnamedSteps;
                }
            }

            HashSet<Demand> m_taskDemands;
            HashSet<String> m_knownNames;
            HashSet<String> m_tasksSatisfy;
            Dictionary<String, List<Step>> m_unnamedSteps;
        }
    }
}
