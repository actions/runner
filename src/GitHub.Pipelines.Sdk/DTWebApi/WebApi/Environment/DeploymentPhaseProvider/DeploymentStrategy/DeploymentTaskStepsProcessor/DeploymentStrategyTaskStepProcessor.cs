using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.Orchestration.Server.Artifacts;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.Validation;

namespace GitHub.DistributedTask.WebApi
{
    internal static class DeploymentStrategyTaskStepProcessor
    {
        public static void GetReferencedResources(
            PipelineBuildContext context,
            ProviderPhase phase,
            IList<Step> steps,
            ValidationResult result)
        {
            if (steps != null)
            {
                foreach (var step in steps)
                {
                    if (step.IsDownloadStepDisabled())
                    {
                        continue;
                    }

                    // todo: enable this once ArtifactResolver is added to PipelineBuildContext
                    //if (context.ArtifactResolver != null)
                    //{
                    //    // Note: we aren't adding any pre steps here as the changes made here are not persisted. This only ensures the validation is done for the Yaml.
                    //    String error = String.Empty;
                    //    if (!context.ArtifactResolver.ResolveStep(context.ResourceStore, step as TaskStep, out error))
                    //    {
                    //        result.Errors.Add(new PipelineValidationError(error));
                    //        return;
                    //    }
                    //}

                    GetReferencedResourcesForEachTaskStep(context, phase, (TaskStep) step, result);
                }
            }
        }

        private static void GetReferencedResourcesForEachTaskStep(
            PipelineBuildContext context,
            ProviderPhase phase,
            TaskStep taskStep,
            ValidationResult result)
        {
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
                // Can't even resolve task definition
                result.Errors.Add(new PipelineValidationError(PipelineStrings.TaskStepReferenceInvalid(phase.Name, taskStep.Name, ex.Message)));
                return;
            }

            // Ensure we were able to find the task with the provided reference data
            if (resolvedTask == null || resolvedTask.Disabled)
            {
                // Stop checking further since we can't even resolve task definition
                String name = taskStep.Reference.Id != Guid.Empty ? taskStep.Reference.Id.ToString() : taskStep.Reference.Name;
                result.Errors.Add(new PipelineValidationError(PipelineStrings.TaskMissing(phase.Name, taskStep.Name, name, taskStep.Reference.Version)));

                return;
            }

            // Make sure this step is compatible with the target used by this phase
            if (phase.Target.IsValid(resolvedTask) == false)
            {
                // Stop checking further since the task is not for valid for the target
                result.Errors.Add(new PipelineValidationError(PipelineStrings.TaskInvalidForGivenTarget(phase.Name, taskStep.Name, resolvedTask.Name, resolvedTask.Version)));
                return;
            }

            // Resolve the task version to pin a given task for the duration of the plan
            taskStep.Reference.Id = resolvedTask.Id;
            taskStep.Reference.Name = resolvedTask.Name;
            taskStep.Reference.Version = resolvedTask.Version;

            // Make sure that we have valid syntax for a condition statement
            var conditionError = ValidateStepCondition(context, phase.Name, taskStep.Name, taskStep.Condition);
            if (conditionError != null)
            {
                result.Errors.Add(conditionError);
            }

            // Resolves values from inputs based on the provided validation options
            ResolveInputs(context, taskStep, resolvedTask, phase.Name, result);
        }

        private static PipelineValidationError ValidateStepCondition(
            PipelineBuildContext context,
            String phaseName,
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
                    return new PipelineValidationError(PipelineStrings.StepConditionIsNotValid(phaseName, stepName, stepCondition, ex.Message));
                }
            }

            return null;
        }

        private static void ResolveInputs(
            PipelineBuildContext context,
            TaskStep step,
            TaskDefinition taskDefinition,
            String phaseName,
            ValidationResult result)
        {
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
                ValidateInput(context, step, input, phaseName, inputAlias, inputValue, result);

                // Now resolve any resources referenced by task inputs
                ResolveResources(context, context.BuildOptions, result.ReferencedResources, result.UnauthorizedResources, step, input, phaseName, inputAlias, inputValue, result);
            }
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

        private static void ValidateInput(
            PipelineBuildContext context,
            TaskStep step,
            TaskInputDefinition input,
            String phaseName,
            String inputAlias,
            String value,
            ValidationResult result)
        {
            if (!context.BuildOptions.ValidateTaskInputs || input.Validation == null)
            {
                return;
            }

            // We cannot perform useful validation if the value didn't expand, it may not be populated until it
            // executes on the target. If we still have variables we just let it go through optimistically.
            var expandedInputValue = context.ExpandVariables(value);
            if (VariableUtility.IsVariable(expandedInputValue))
            {
                return;
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

            // Make sure to track any input validaton errors encountered
            var validationResult = context.InputValidator.Validate(inputContext);
            if (validationResult.IsValid)
            {
                return;
            }
            else
            {
                // Make sure we do not expose secrets when logging errors about expanded input values
                var maskedValue = context.SecretMasker.MaskSecrets(expandedInputValue);
                var reason = validationResult.Reason ?? input.Validation.Message;
                result.Errors.Add(new PipelineValidationError(PipelineStrings.StepTaskInputInvalid(phaseName, step.Name, inputAlias, maskedValue, inputContext.Expression, reason)));
            }
        }

        private static void ResolveResources(
            IPipelineContext context,
            BuildOptions options,
            PipelineResources referencedResources,
            PipelineResources unauthorizedResources,
            TaskStep step,
            TaskInputDefinition input,
            String phaseName,
            String inputAlias,
            String inputValue,
            ValidationResult result)
        {
            if (String.IsNullOrEmpty(inputValue))
            {
                return;
            }

            if (input.InputType.StartsWith(c_endpointInputTypePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var resolvedEndpoints = new List<String>();
                var endpointType = input.InputType.Remove(0, c_endpointInputTypePrefix.Length);
                var resolvedInputValues = inputValue.Split(',').Select(x => x.Trim()).Where(x => !String.IsNullOrEmpty(x));
                foreach (var value in resolvedInputValues)
                {
                    var replacedValue = context.ExpandVariables(value);
                    var endpoint = context.ResourceStore.GetEndpoint(replacedValue);
                    if (endpoint != null)
                    {
                        replacedValue = endpoint.Id.ToString();
                    }

                    referencedResources.AddEndpointReference(replacedValue);

                    // Validate the resource using the provided store if desired
                    if (options.ValidateResources)
                    {
                        if (endpoint == null)
                        {
                            resolvedEndpoints.Add(replacedValue);
                            unauthorizedResources?.AddEndpointReference(replacedValue);
                            result.Errors.Add(new PipelineValidationError(PipelineStrings.ServiceEndpointNotFoundForInput(phaseName, step.Name, inputAlias, replacedValue)));
                     
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
                                        result.Errors.Add(new PipelineValidationError(PipelineStrings.StepInputEndpointTypeMismatch(phaseName, step.Name, inputAlias, endpointTypeName, endpoint.Name, endpoint.Type)));
                                    }
                                    else if (endpointTypeSegments.Count > 1 && !String.IsNullOrEmpty(endpoint.Authorization?.Scheme))
                                    {
                                        var supportedAuthSchemes = endpointTypeSegments[1]?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                                        if (supportedAuthSchemes?.Count > 0 && !supportedAuthSchemes.Any(x => x.Equals(endpoint.Authorization.Scheme, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            result.Errors.Add(new PipelineValidationError(PipelineStrings.StepInputEndpointAuthSchemeMismatch(phaseName, step.Name, inputAlias, endpointTypeName, endpointTypeSegments[1], endpoint.Name, endpoint.Type, endpoint.Authorization.Scheme)));
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
                    var secureFile = context.ResourceStore.GetFile(replacedValue);
                    if (secureFile != null)
                    {
                        replacedValue = secureFile.Id.ToString();
                    }

                    referencedResources.AddSecureFileReference(replacedValue);

                    // Validate the resource using the provided store if desired
                    if (options.ValidateResources)
                    {
                        if (secureFile == null)
                        {
                           resolvedFiles.Add(replacedValue);
                           unauthorizedResources?.AddSecureFileReference(replacedValue);
                            result.Errors.Add(new PipelineValidationError(PipelineStrings.SecureFileNotFoundForInput(phaseName, step.Name, inputAlias, replacedValue)));
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
                            result.Errors.Add(new PipelineValidationError(PipelineStrings.RepositoryResourceNotFound(inputValue)));
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
                                    unauthorizedResources?.AddEndpointReference(repository.Endpoint);
                                    result.Errors.Add(new PipelineValidationError(PipelineStrings.ServiceEndpointNotFound(repository.Endpoint)));
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
                                   unauthorizedResources?.AddEndpointReference(selfRepository.Endpoint);
                                   result.Errors.Add(new PipelineValidationError(PipelineStrings.ServiceEndpointNotFound(selfRepository.Endpoint)));
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
        }

        private const String c_endpointInputTypePrefix = "connectedService:";
        private const String c_secureFileInputType = "secureFile";
    }
}
