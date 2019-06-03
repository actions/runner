using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.Artifacts;
using GitHub.DistributedTask.Pipelines.Validation;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.WebApi
{
    internal static class DeploymentStrategyTaskSteps
    {
        public static IList<Step> Build(
            JEnumerable<JToken> stepsJTokens,
            ValidationResult validationResult)
        {
            var steps = new List<Step>();
            var stepNameCounterMap = new Dictionary<string, int>();

            foreach (var eachStepJToken in stepsJTokens)
            {
                var stepProp = eachStepJToken.Children<JProperty>().FirstOrDefault();
                TaskStep step = null;
                switch (stepProp.Name)
                {
                    case ScriptStep:
                        step = BuildCommandlineStep(eachStepJToken, validationResult);
                        break;

                    case PowershellStep:
                        step = BuildPowershellStep(eachStepJToken, validationResult);
                        break;

                    case PwshStep:
                        step = BuildPowershellStep(eachStepJToken, validationResult);
                        step.Inputs["pwsh"] = "true";
                        break;

                    case BashStep:
                        step = BuildBashStep(eachStepJToken, validationResult);
                        break;

                    case DownloadStep:
                        step = BuildDownloadStep(eachStepJToken, validationResult);
                        break;

                    case DownloadBuildStep:
                        step = BuildDownloadBuildStep(eachStepJToken, validationResult);
                        break;

                    case UploadStep:
                        step = BuildUploadStep(eachStepJToken, validationResult);
                        break;

                    case CheckoutStep:
                        step = BuildCheckoutStep(eachStepJToken, validationResult);
                        break;

                    default:
                        step = BuildTaskStep(stepProp.Name, eachStepJToken, validationResult);
                        break;
                }

                // Skip the step if it is disabled.
                if (step != null && step.Enabled != false)
                {
                    // Set Display Name if not provided
                    if (string.IsNullOrWhiteSpace(step.DisplayName))
                    {
                        step.DisplayName = step.Reference.Name;
                    }

                    // Set the task name if not provided
                    if (string.IsNullOrWhiteSpace(step.Name))
                    {
                        step.Name = step.Reference.Name;
                    }

                    ValidateAndUpdateStepNameIfRequired(stepNameCounterMap, step);

                    steps.Add(step);

                }
            }

            return steps;
        }

        private static TaskStep BuildCommandlineStep(
            JToken commandlineJProperty,
            ValidationResult validationResult)
        {
            var step = new TaskStep();

            step.Reference = new TaskStepDefinitionReference
            {
                Name = "CmdLine",
                Version = "2"
            };

            step = BuildBaseStep(commandlineJProperty, step, validationResult);

            foreach (var eachCmdLineChild in commandlineJProperty.Children())
            {
                var cmdLineProp = eachCmdLineChild.Value<JProperty>();

                switch (cmdLineProp.Name)
                {
                    case "script":
                        step.Inputs["script"] = cmdLineProp.Value.ToString();
                        break;

                    case "failOnStderr":
                        step.Inputs["failOnStderr"] = cmdLineProp.Value.ToString();
                        break;

                    case "workingDirectory":
                        step.Inputs["workingDirectory"] = cmdLineProp.Value.ToString();
                        break;
                }
            }

            return step;
        }

        // Powershell task transformations
        private static TaskStep BuildPowershellStep(
            JToken powershellJToken,
            ValidationResult validationResult)
        {
            var step = new TaskStep();

            step.Reference = new TaskStepDefinitionReference
            {
                Name = "PowerShell",
                Version = "2"
            };

            step = BuildBaseStep(powershellJToken, step, validationResult);
            step.Inputs["targetType"] = "inline";
            step.Inputs["script"] = string.Empty;

            var eachPowershellStepProps = powershellJToken.Children<JProperty>();
            foreach (var eachPowershellStepProp in eachPowershellStepProps)
            {
                switch (eachPowershellStepProp.Name)
                {
                    case "powershell":
                    case "pwsh":
                        if (step.Inputs["script"] == string.Empty)
                        {
                            step.Inputs["script"] = eachPowershellStepProp.Value.ToString();
                        }
                        else
                        {
                            validationResult.Errors.Add(new PipelineValidationError("Should not define both inputs - 'powershell' and 'pwsh'. Only one is allowed at a time"));
                        }
                        break;

                    case "errorActionPreference":
                        step.Inputs["errorActionPreference"] = eachPowershellStepProp.Value.ToString();
                        break;

                    case "failOnStderr":
                        step.Inputs["failOnStderr"] = eachPowershellStepProp.Value.ToString();
                        break;

                    case "ignoreLASTEXITCODE":
                        step.Inputs["ignoreLASTEXITCODE"] = eachPowershellStepProp.Value.ToString();
                        break;

                    case "workingDirectory":
                        step.Inputs["workingDirectory"] = eachPowershellStepProp.Value.ToString();
                        break;
                }
            }

            return step;
        }

        private static TaskStep BuildBashStep(
            JToken bashJToken,
            ValidationResult validationResult)
        {
            var step = new TaskStep();

            step.Reference = new TaskStepDefinitionReference
            {
                Name = "Bash",
                Version = "3"
            };

            step = BuildBaseStep(bashJToken, step, validationResult);
            step.Inputs["targetType"] = "inline";

            foreach (var eachBashStepProp in bashJToken.Children<JProperty>())
            {
                switch (eachBashStepProp.Name)
                {
                    case "bash":
                        step.Inputs["script"] = eachBashStepProp.Value.ToString();
                        break;

                    case "failOnStderr":
                        step.Inputs["failOnStderr"] = eachBashStepProp.Value.ToString();
                        break;

                    case "workingDirectory":
                        step.Inputs["workingDirectory"] = eachBashStepProp.Value.ToString();
                        break;
                }
            }

            return step;
        }

        private static TaskStep BuildDownloadStep(
            JToken downloadJToken,
            ValidationResult validationResult)
        {
            var step = new TaskStep();

            step.Reference = new TaskStepDefinitionReference
            {
                Name = PipelineArtifactConstants.DownloadTask.Name,
                Id = PipelineArtifactConstants.DownloadTask.Id,
                Version = PipelineArtifactConstants.DownloadTask.Version.ToString()
            };

            step = BuildBaseStep(downloadJToken, step, validationResult);

            foreach (var downloadStepProp in downloadJToken.Children<JProperty>())
            {
                switch (downloadStepProp.Name)
                {
                    case DownloadStep:
                        if (String.IsNullOrWhiteSpace(downloadStepProp.Value.ToString()))
                        {
                            validationResult.Errors.Add(new PipelineValidationError("Download step must have an alias."));
                            break;
                        }

                        step.Inputs[PipelineArtifactConstants.DownloadTaskInputs.Alias] = downloadStepProp.Value.ToString();
                        break;

                    case PipelineArtifactConstants.DownloadTaskInputs.Artifact:
                        step.Inputs[PipelineArtifactConstants.DownloadTaskInputs.Artifact] = downloadStepProp.Value.ToString();
                        break;

                    case PipelineArtifactConstants.DownloadTaskInputs.Path:
                        step.Inputs[PipelineArtifactConstants.DownloadTaskInputs.Path] = downloadStepProp.Value.ToString();
                        break;

                    case PipelineArtifactConstants.DownloadTaskInputs.Patterns:
                        step.Inputs[PipelineArtifactConstants.DownloadTaskInputs.Patterns] = downloadStepProp.Value.ToString();
                        break;
                }
            }

            return step;
        }

        private static TaskStep BuildDownloadBuildStep(
            JToken downloadJToken,
            ValidationResult validationResult)
        {
            var step = new TaskStep();

            step.Reference = new TaskStepDefinitionReference
            {
                Name = DownloadBuildStep,
            };

            step = BuildBaseStep(downloadJToken, step, validationResult);

            foreach (var downloadBuildStepProp in downloadJToken.Children<JProperty>())
            {
                switch (downloadBuildStepProp.Name)
                {
                    case DownloadBuildStep:
                        if (String.IsNullOrWhiteSpace(downloadBuildStepProp.Value.ToString()))
                        {
                            validationResult.Errors.Add(new PipelineValidationError("downloadBuild step must have an alias."));
                            break;
                        }

                        step.Inputs[PipelineArtifactConstants.DownloadTaskInputs.Alias] = downloadBuildStepProp.Value.ToString();
                        break;

                    case PipelineArtifactConstants.DownloadTaskInputs.Artifact:
                        step.Inputs[PipelineArtifactConstants.DownloadTaskInputs.Artifact] = downloadBuildStepProp.Value.ToString();
                        break;

                    case PipelineArtifactConstants.DownloadTaskInputs.Path:
                        step.Inputs[PipelineArtifactConstants.DownloadTaskInputs.Path] = downloadBuildStepProp.Value.ToString();
                        break;

                    case PipelineArtifactConstants.DownloadTaskInputs.Patterns:
                        step.Inputs[PipelineArtifactConstants.DownloadTaskInputs.Patterns] = downloadBuildStepProp.Value.ToString();
                        break;
                }
            }

            return step;
        }

        private static TaskStep BuildUploadStep(
            JToken uploadJToken,
            ValidationResult validationResult)
        {
            var step = new TaskStep();

            step.Reference = new TaskStepDefinitionReference
            {
                Name = "PublishPipelineArtifact",
                Id = new Guid("ecdc45f6-832d-4ad9-b52b-ee49e94659be"),
                Version = "0"
            };

            foreach (var uploadStepProp in uploadJToken.Children<JProperty>())
            {
                switch (uploadStepProp.Name)
                {
                    case UploadStep:
                        step.Inputs["targetPath"] = uploadStepProp.Value.ToString();
                        break;

                    case "artifact":
                        step.Inputs["artifactName"] = uploadStepProp.Value.ToString();
                        break;
                }
            }

            return step;
        }

        private static TaskStep BuildCheckoutStep(
            JToken checkoutJToken,
            ValidationResult validationResult)
        {
            var step = new TaskStep();

            step.Reference = new TaskStepDefinitionReference
            {
                Name = PipelineConstants.CheckoutTask.Name,
                Id = PipelineConstants.CheckoutTask.Id,
                Version = PipelineConstants.CheckoutTask.Version.ToString()
            };

            step = BuildBaseStep(checkoutJToken, step, validationResult);

            foreach (var checkoutStepProp in checkoutJToken.Children<JProperty>())
            {
                switch (checkoutStepProp.Name)
                {
                    case CheckoutStep:
                        // TODO: Validate deployment job steps at pipeline queue time.
                        var repository = checkoutStepProp.Value.ToString();
                        if (String.IsNullOrWhiteSpace(repository) &&
                            !String.Equals(repository, PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase) &&
                            !String.Equals(repository, PipelineConstants.NoneAlias, StringComparison.OrdinalIgnoreCase) &&
                            !String.Equals(repository, PipelineConstants.DesignerRepo, StringComparison.OrdinalIgnoreCase))
                        {
                            validationResult.Errors.Add(new PipelineValidationError(PipelineStrings.CheckoutStepRepositoryNotSupported(repository)));
                        }

                        step.Inputs[PipelineConstants.CheckoutTaskInputs.Repository] = checkoutStepProp.Value.ToString();

                        // exclude the checkout task in repository is set to none.
                        if (String.Equals(repository, PipelineConstants.NoneAlias, StringComparison.OrdinalIgnoreCase))
                        {
                            step.Condition = "false";
                        }
                        break;

                    case "clean":
                        step.Inputs[PipelineConstants.CheckoutTaskInputs.Clean] = checkoutStepProp.Value.ToString();
                        break;

                    case "submodules":
                        step.Inputs[PipelineConstants.CheckoutTaskInputs.Submodules] = checkoutStepProp.Value.ToString();
                        break;

                    case "lfs":
                        step.Inputs[PipelineConstants.CheckoutTaskInputs.Lfs] = checkoutStepProp.Value.ToString();
                        break;

                    case "fetchDepth":
                        step.Inputs[PipelineConstants.CheckoutTaskInputs.FetchDepth] = checkoutStepProp.Value.ToString();
                        break;

                    case "persistCredentials":
                        step.Inputs[PipelineConstants.CheckoutTaskInputs.PersistCredentials] = checkoutStepProp.Value.ToString();
                        break;

                    case "path":
                        step.Inputs["path"] = checkoutStepProp.Value.ToString();
                        break;
                }
            }

            return step;
        }

        private static TaskStep BuildTaskStep(
            String stepName,
            JToken stepJToken,
            ValidationResult validationResult)
        {
            var step = stepJToken.ToObject<TaskStep>();

            var taskProp = stepJToken.Children<JProperty>().FirstOrDefault(x => x.Name == StepTaskPropertyName);

            if (taskProp == null || !taskProp.HasValues)
            {
                validationResult.Errors.Add(new PipelineValidationError($"Step {stepName} does not have valid 'task' property"));
                return null;
            }

            if (!TryParseTaskReference(taskProp.Value.ToString(), out String taskName, out String taskVersion))
            {
                validationResult.Errors.Add(new PipelineValidationError($"Invalid step task reference {taskProp.Value.ToString()}"));
                return null;
            }

            step.Reference = new TaskStepDefinitionReference
            {
                Name = taskName,
                Version = taskVersion,
            };

            return step;
        }

        private static void ValidateAndUpdateStepNameIfRequired(
            Dictionary<String, Int32> stepNameCounterMap,
            TaskStep step)
        {
            while (stepNameCounterMap.TryGetValue(step.Name, out var taskCounter))
            {
                step.Name = $"{step.Name}{++stepNameCounterMap[step.Name]}";
            }

            stepNameCounterMap.Add(step.Name, 1);
        }

        private static Boolean TryParseTaskReference(
            String value,
            out String name,
            out String version)
        {
            Boolean result;
            if (!String.IsNullOrEmpty(value))
            {
                String[] refComponents = value.Split('@');
                if (refComponents.Length == 2 &&
                    !String.IsNullOrEmpty(refComponents[0]) &&
                    !String.IsNullOrEmpty(refComponents[1]) &&
                    Int32.TryParse(refComponents[1], NumberStyles.None, CultureInfo.InvariantCulture, out _))
                {
                    result = true;
                    name = refComponents[0];
                    version = refComponents[1];
                }
                else
                {
                    result = false;
                    name = null;
                    version = null;
                }
            }
            else
            {
                result = false;
                name = null;
                version = null;
            }

            return result;
        }

        private static TaskStep BuildBaseStep(
            JToken stepJToken,
            TaskStep step,
            ValidationResult validationResult)
        {
            foreach (var stepProp in stepJToken.Children<JProperty>())
            {
                switch (stepProp.Name)
                {
                    case "condition":
                        step.Condition = stepProp.Value.ToString();
                        break;

                    case "name":
                        step.Name = stepProp.Value.ToString();
                        break;

                    case "continueOnError":
                        step.ContinueOnError = ConvertToBoolean(stepProp.Value.ToString(), validationResult);
                        break;

                    case "displayName":
                        step.DisplayName = stepProp.Value.ToString();
                        break;

                    case "enabled":
                        step.Enabled = ConvertToBoolean(stepProp.Value.ToString(), validationResult);
                        break;

                    case "timeoutInMinutes":
                        step.TimeoutInMinutes = ConvertToInt32(stepProp.Value.ToString(), validationResult);
                        break;

                    case "env":
                        var environmentVariables = stepProp.Value.Children<JProperty>();
                        foreach (var envVarProperty in environmentVariables)
                        {
                            step.Environment.Add(envVarProperty.Name, envVarProperty.Value.ToString());
                        }
                        break;
                }
            }

            return step;
        }


        private static Boolean ConvertToBoolean(
            String literal,
            ValidationResult validationResult)
        {
            if (TryParseBoolean(literal, out Boolean result))
            {
                return result;
            }

            validationResult.Errors.Add(new PipelineValidationError(String.Format(CultureInfo.InvariantCulture, "Expected a Boolean value. Actual value: {0}.", literal)));
            return default;
        }

        private static Int32 ConvertToInt32(
            String literal,
            ValidationResult validationResult)
        {
            if (TryParseInt32(literal, out Int32 result))
            {
                return result;
            }

            validationResult.Errors.Add(new PipelineValidationError(String.Format(CultureInfo.InvariantCulture, "Expected an integer value. Actual value: {0}.", literal)));
            return default;
        }

        private static Boolean TryParseBoolean(
            String value,
            out Boolean result)
        {
            if (!String.IsNullOrEmpty(value))
            {
                if (String.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    return true;
                }
                else if (String.Equals(value, "FALSE", StringComparison.OrdinalIgnoreCase))
                {
                    result = false;
                    return true;
                }
            }

            result = default;
            return false;
        }

        private static Boolean TryParseInt32(
            String value,
            out Int32 result)
        {
            if (!String.IsNullOrEmpty(value) &&
                Int32.TryParse(
                    value,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return true;
            }

            result = default;
            return false;
        }

        private const String ScriptStep = "script";
        private const String PowershellStep = "powershell";
        private const String PwshStep = "pwsh";
        private const String BashStep = "bash";
        private const String DownloadStep = "download";
        private const String DownloadBuildStep = "downloadBuild";
        private const String UploadStep = "upload";
        private const String CheckoutStep = "checkout";
        private const String StepTaskPropertyName = "task";
    }
}
