#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GitHub.Actions.Expressions;
using GitHub.Actions.Expressions.Data;
using GitHub.Actions.Expressions.Sdk;
using GitHub.Actions.Expressions.Sdk.Functions;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    internal static class WorkflowTemplateConverter
    {
        /// <summary>
        /// Constructs the <c ref="WorkflowTemplate" />. Errors are stored to both <c ref="TemplateContext.Errors" /> and <c ref="WorkflowTemplate.Errors" />.
        /// </summary>
        internal static WorkflowTemplate ConvertToWorkflow(
            TemplateContext context,
            TemplateToken workflow)
        {
            var result = new WorkflowTemplate();
            result.FileTable.AddRange(context.GetFileTable());

            // Note, the "finally" block appends context.Errors to result
            try
            {
                if (workflow == null || context.Errors.Count > 0)
                {
                    return result;
                }

                var workflowMapping = workflow.AssertMapping("root");

                foreach (var workflowPair in workflowMapping)
                {
                    var workflowKey = workflowPair.Key.AssertString("root key");

                    switch (workflowKey.Value)
                    {
                        case WorkflowTemplateConstants.On:
                            var inputTypes = ConvertToOnWorkflowDispatchInputTypes(workflowPair.Value);
                            foreach (var item in inputTypes)
                            {
                                result.InputTypes.TryAdd(item.Key, item.Value);
                            }
                            break;

                        case WorkflowTemplateConstants.Description:
                        case WorkflowTemplateConstants.Name:
                        case WorkflowTemplateConstants.RunName:
                            break;

                        case WorkflowTemplateConstants.Defaults:
                            result.Defaults = workflowPair.Value;
                            break;

                        case WorkflowTemplateConstants.Env:
                            result.Env = workflowPair.Value;
                            break;

                        case WorkflowTemplateConstants.Concurrency:
                            ConvertToConcurrency(context, workflowPair.Value, isEarlyValidation: true);
                            result.Concurrency = workflowPair.Value;
                            break;

                        case WorkflowTemplateConstants.Jobs:
                            result.Jobs.AddRange(ConvertToJobs(context, workflowPair.Value));
                            break;

                        case WorkflowTemplateConstants.Permissions:
                            result.Permissions = ConvertToPermissions(context, workflowPair.Value);
                            break;

                        default:
                            workflowKey.AssertUnexpectedValue("root key"); // throws
                            break;
                    }
                }

                // Propagate explicit permissions
                if (result.Permissions != null)
                {
                    foreach (var job in result.Jobs)
                    {
                        if (job.Permissions == null)
                        {
                            job.Permissions = result.Permissions;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                context.Errors.Add(ex);
            }
            finally
            {
                if (context.Errors.Count > 0)
                {
                    foreach (var error in context.Errors)
                    {
                        result.Errors.Add(new WorkflowValidationError(error.Code, error.Message));
                    }
                }
            }

            return result;
        }

        internal static void ConvertToReferencedWorkflow(
            TemplateContext context,
            TemplateToken referencedWorkflow,
            ReusableWorkflowJob workflowJob,
            String permissionsPolicy,
            bool isTrusted)
        {
            // Explicit max permissions for the reusable workflow or reusable workflow chain.
            // Present only when the caller (or higher ancestor) has defined the maximum allowed permissions in YAML.
            var explicitMaxPermissions = workflowJob.Permissions;

            var workflowMapping = referencedWorkflow.AssertMapping("root");
            foreach (var workflowPair in workflowMapping)
            {
                var workflowKey = workflowPair.Key.AssertString("root key");
                switch (workflowKey.Value)
                {
                    case WorkflowTemplateConstants.On:
                        ConvertToOnTrigger(context, workflowPair.Value, workflowJob);
                        break;

                    case WorkflowTemplateConstants.Description:
                    case WorkflowTemplateConstants.Name:
                    case WorkflowTemplateConstants.RunName:
                        break;

                    case WorkflowTemplateConstants.Defaults:
                        workflowJob.Defaults = workflowPair.Value;
                        break;

                    case WorkflowTemplateConstants.Env:
                        workflowJob.Env = workflowPair.Value;
                        break;

                    case WorkflowTemplateConstants.Concurrency:
                        ConvertToConcurrency(context, workflowPair.Value, true);
                        workflowJob.EmbeddedConcurrency = workflowPair.Value;
                        break;
                    case WorkflowTemplateConstants.Jobs:
                        workflowJob.Jobs.AddRange(ConvertToJobs(context, workflowPair.Value));
                        break;

                    case WorkflowTemplateConstants.Permissions:
                        var embeddedRootPermissions = ConvertToPermissions(context, workflowPair.Value);
                        PermissionsHelper.ValidateEmbeddedPermissions(
                            context,
                            workflowJob,
                            embeddedJob: null,
                            requested: embeddedRootPermissions,
                            explicitMax: explicitMaxPermissions,
                            permissionsPolicy,
                            isTrusted);
                        workflowJob.Permissions = embeddedRootPermissions;
                        break;

                    default:
                        workflowKey.AssertUnexpectedValue("root key"); // throws
                        break;
                }
            }

            // Validate requested permissions or propagate explicit permissions
            foreach (var embeddedJob in workflowJob.Jobs)
            {
                if (embeddedJob.Permissions != null)
                {
                    // Validate requested permissions
                    PermissionsHelper.ValidateEmbeddedPermissions(
                        context,
                        workflowJob,
                        embeddedJob,
                        requested: embeddedJob.Permissions,
                        explicitMax: explicitMaxPermissions,
                        permissionsPolicy,
                        isTrusted);
                }
                else if (workflowJob.Permissions != null)
                {
                    // Propagate explicit permissions
                    embeddedJob.Permissions = workflowJob.Permissions;
                }
            }
        }

        internal static IDictionary<String, String> ConvertToOnWorkflowDispatchInputTypes(TemplateToken onToken)
        {
            var result = new Dictionary<String, String>();
            if (onToken.Type != TokenType.Mapping)
            {
                return result;
            }

            var triggerMapping = onToken.AssertMapping($"workflow {WorkflowTemplateConstants.On} value");
            var dispatchTrigger = triggerMapping.FirstOrDefault(x =>
                string.Equals((x.Key as StringToken).Value, WorkflowTemplateConstants.WorkflowDispatch, StringComparison.Ordinal)).Value;
            if (dispatchTrigger == null || dispatchTrigger is NullToken)
            {
                return result;
            }

            var wfDispatchDefinitions = dispatchTrigger.AssertMapping($"workflow {WorkflowTemplateConstants.On} value {WorkflowTemplateConstants.WorkflowDispatch}");
            var inputDefinitionsToken = wfDispatchDefinitions.FirstOrDefault(x =>
                string.Equals((x.Key as StringToken).Value, WorkflowTemplateConstants.Inputs, StringComparison.Ordinal)).Value;
            if (inputDefinitionsToken == null || inputDefinitionsToken is NullToken)
            {
                return result;
            }

            var inputs = inputDefinitionsToken.AssertMapping($"{WorkflowTemplateConstants.On}-{WorkflowTemplateConstants.WorkflowDispatch}-{WorkflowTemplateConstants.Inputs}");
            var inputDefinitions = inputs?
                .ToDictionary(
                    x => x.Key.AssertString("inputs key").Value,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase
                );
            foreach (var definedItem in inputDefinitions)
            {
                string definedKey = definedItem.Key;

                if (definedItem.Value is NullToken)
                {
                    result.Add(definedKey, WorkflowTemplateConstants.TypeString);
                    continue;
                }

                var definedInputSpec = definedItem.Value.AssertMapping($"input {definedKey}").ToDictionary(
                        x => x.Key.AssertString($"input {definedKey} key").Value,
                        x => x.Value,
                        StringComparer.OrdinalIgnoreCase);
                if (definedInputSpec.TryGetValue(WorkflowTemplateConstants.Type, out TemplateToken inputTypeToken) &&
                    inputTypeToken is StringToken inputTypeStringToken)
                {
                    result.Add(definedKey, inputTypeStringToken.Value);
                }
                else
                {
                    result.Add(definedKey, WorkflowTemplateConstants.TypeString);
                }
            }
            return result;
        }

        internal static void ConvertToOnTrigger(
            TemplateContext context,
            TemplateToken onToken,
            ReusableWorkflowJob parentWorkflowJob)
        {
            switch (onToken.Type)
            {
                // check for on: workflow_call
                case TokenType.String:
                    var result = onToken.AssertString($"Reference workflow {WorkflowTemplateConstants.On} value");
                    if (result.Value == WorkflowTemplateConstants.WorkflowCall)
                    {
                        ValidateWorkflowJobTrigger(context, parentWorkflowJob);
                        return;
                    }
                    break;

                // check for on: [push, workflow_call]
                case TokenType.Sequence:
                    var triggers = onToken.AssertSequence($"Reference workflow {WorkflowTemplateConstants.On} value");

                    foreach (var triggerItem in triggers)
                    {
                        var triggerString = triggerItem.AssertString($"Reference workflow {WorkflowTemplateConstants.On} value {triggerItem}").Value;
                        if (triggerString == WorkflowTemplateConstants.WorkflowCall)
                        {
                            ValidateWorkflowJobTrigger(context, parentWorkflowJob);
                            return;
                        }
                    }
                    break;

                // check for on: workflow_call Mapping
                case TokenType.Mapping:
                    var triggerMapping = onToken.AssertMapping($"Reference workflow {WorkflowTemplateConstants.On} value");

                    foreach (var triggerItem in triggerMapping)
                    {
                        var triggerString = triggerItem.Key.AssertString($"Reference workflow {WorkflowTemplateConstants.On} value {triggerItem.Key}").Value;
                        if (triggerString == WorkflowTemplateConstants.WorkflowCall)
                        {
                            if (triggerItem.Value is NullToken)
                            {
                                ValidateWorkflowJobTrigger(context, parentWorkflowJob);
                                return;
                            }

                            var wfCallDefinitions = triggerItem.Value.AssertMapping($"Reference workflow {WorkflowTemplateConstants.On} value {triggerItem.Key}");

                            foreach (var wfCallDefinitionItem in wfCallDefinitions)
                            {
                                var wfCallDefinitionItemKey = wfCallDefinitionItem.Key.AssertString($"{WorkflowTemplateConstants.On}-{WorkflowTemplateConstants.WorkflowCall}-{wfCallDefinitionItem.Key.ToString()}").Value;
                                if (wfCallDefinitionItemKey == WorkflowTemplateConstants.Inputs)
                                {
                                    parentWorkflowJob.InputDefinitions = wfCallDefinitionItem.Value.AssertMapping($"{WorkflowTemplateConstants.On}-{WorkflowTemplateConstants.WorkflowCall}-{wfCallDefinitionItem.Key.ToString()}");
                                }
                                else if (wfCallDefinitionItemKey == WorkflowTemplateConstants.Secrets)
                                {
                                    parentWorkflowJob.SecretDefinitions = wfCallDefinitionItem.Value.AssertMapping($"{WorkflowTemplateConstants.On}-{WorkflowTemplateConstants.WorkflowCall}-{wfCallDefinitionItem.Key.ToString()}");
                                }
                                else if (wfCallDefinitionItemKey == WorkflowTemplateConstants.Outputs)
                                {
                                    parentWorkflowJob.Outputs = wfCallDefinitionItem.Value.AssertMapping($"{WorkflowTemplateConstants.On}-{WorkflowTemplateConstants.WorkflowCall}-{wfCallDefinitionItem.Key.ToString()}");
                                }
                            }

                            ValidateWorkflowJobTrigger(context, parentWorkflowJob);
                            return;
                        }
                    }
                    break;
                default:
                    break;
            }

            context.Error(onToken, $"{WorkflowTemplateConstants.WorkflowCall} key is not defined in the referenced workflow.");
            return;
        }

        internal static Boolean ConvertToIfResult(
            TemplateContext context,
            TemplateToken ifResult)
        {
            var expression = ifResult.Traverse().FirstOrDefault(x => x is ExpressionToken);
            if (expression != null)
            {
                throw new ArgumentException($"Unexpected type '{expression.GetType().Name}' encountered while reading 'if'.");
            }

            var evaluationResult = EvaluationResult.CreateIntermediateResult(null, ifResult);
            return evaluationResult.IsTruthy;
        }

        internal static String ConvertToJobName(
            TemplateContext context,
            TemplateToken name,
            Boolean isEarlyValidation = false)
        {
            var result = default(String);

            // Expression
            if (isEarlyValidation && name is ExpressionToken)
            {
                return result;
            }

            // String
            var nameString = name.AssertString($"job {WorkflowTemplateConstants.Name}");
            result = nameString.Value;
            return result;
        }

        internal static Snapshot ConvertToSnapshot(
            TemplateContext context,
            TemplateToken snapshotToken,
            Boolean isEarlyValidation = false)
        {
            String imageName = null;
            string defaultVersion = "1.*";
            String version = null;
            var condition = new BasicExpressionToken(null, null, null, $"{WorkflowTemplateConstants.Success}()");

            if (isEarlyValidation && snapshotToken is ExpressionToken)
            {
                return default;
            }


            // String
            if (snapshotToken is StringToken snapshotStringToken)
            {
                imageName = snapshotStringToken.Value;
            }
            // Mapping
            else if (snapshotToken is MappingToken snapshotMappingToken)
            {
                foreach (var snapshotProperty in snapshotMappingToken)
                {
                    var propertyName = snapshotProperty.Key.AssertString($"job {WorkflowTemplateConstants.Snapshot} key");
                    var propertyValue = snapshotProperty.Value;

                    switch (propertyName.Value)
                    {
                        case WorkflowTemplateConstants.ImageName:
                            if (isEarlyValidation && propertyValue is ExpressionToken)
                            {
                                return default;
                            }
                            imageName = propertyValue.AssertString($"job {WorkflowTemplateConstants.Snapshot} {WorkflowTemplateConstants.ImageName}").Value;
                            break;
                        case WorkflowTemplateConstants.If:
                            condition = ConvertToIfCondition(context, propertyValue, IfKind.Snapshot);
                            break;
                        case WorkflowTemplateConstants.CustomImageVersion:
                            if (isEarlyValidation && propertyValue is ExpressionToken)
                            {
                                return default;
                            }
                            var versionValue = propertyValue.AssertString($"job {WorkflowTemplateConstants.Snapshot} {WorkflowTemplateConstants.CustomImageVersion}").Value;
                            if (versionValue != null && !IsSnapshotImageVersionValid(versionValue))
                            {
                                context.Error(snapshotToken, "Expected format '{major-version}.*' Actual '" + versionValue + "'");
                                return null;
                            }
                            version = versionValue;
                            break;
                        default:
                            propertyName.AssertUnexpectedValue($"job {WorkflowTemplateConstants.Snapshot} key");
                            break;
                    }
                }
            }

            // ImageName is a required property (schema validation)
            if (imageName == null)
            {
                context.Error(snapshotToken, $"job {WorkflowTemplateConstants.Snapshot} {WorkflowTemplateConstants.ImageName} is required.");
                return null;
            }

            return new Snapshot
            {
                ImageName = imageName,
                If = condition,
                Version = version ?? defaultVersion
            };
        }

        private static bool IsSnapshotImageVersionValid(string versionString)
        {
            var versionSegments = versionString.Split(".");

            if (versionSegments.Length != 2 ||
                !versionSegments[1].Equals("*") ||
                !Int32.TryParse(versionSegments[0], NumberStyles.None, CultureInfo.InvariantCulture, result: out int parsedMajor) ||
                parsedMajor < 0)
            {
                return false;
            }

            return true;
        }

        internal static RunsOn ConvertToRunsOn(
            TemplateContext context,
            TemplateToken runsOn,
            Boolean isEarlyValidation = false)
        {
            var result = new RunsOn();

            ConvertToRunsOnLabels(context, runsOn, result, isEarlyValidation);

            // Mapping
            if (runsOn is MappingToken runsOnMapping)
            {
                foreach (var runsOnToken in runsOnMapping)
                {
                    var propertyName = runsOnToken.Key.AssertString($"job {WorkflowTemplateConstants.RunsOn} property name");

                    switch (propertyName.Value)
                    {
                        case WorkflowTemplateConstants.Group:
                            // Expression
                            if (isEarlyValidation && runsOnToken.Value is ExpressionToken)
                            {
                                continue;
                            }

                            // String
                            var groupName = runsOnToken.Value.AssertString($"job {WorkflowTemplateConstants.RunsOn} {WorkflowTemplateConstants.Group} name").Value;
                            var names = groupName.Split(WorkflowTemplateConstants.Slash);
                            if (names.Length == 2)
                            {
                                if (string.IsNullOrEmpty(names[1]))
                                {
                                    context.Error(runsOnToken.Value, $"Invalid {WorkflowTemplateConstants.RunsOn} {WorkflowTemplateConstants.Group} name '{groupName}'.");
                                }
                                else if (!string.Equals(names[0], WorkflowTemplateConstants.Org) && !string.Equals(names[0], WorkflowTemplateConstants.Organization) &&
                                    !string.Equals(names[0], WorkflowTemplateConstants.Ent) && !string.Equals(names[0], WorkflowTemplateConstants.Enterprise))
                                {

                                    context.Error(runsOnToken.Value, $"Invalid {WorkflowTemplateConstants.RunsOn} {WorkflowTemplateConstants.Group} name '{groupName}'. Please use 'organization/' or 'enterprise/' prefix to target a single runner group.");
                                }
                                else
                                {
                                    result.RunnerGroup = groupName;
                                }
                            }
                            else if (names.Length > 2)
                            {
                                context.Error(runsOnToken.Value, $"Invalid {WorkflowTemplateConstants.RunsOn} {WorkflowTemplateConstants.Group} name '{groupName}'. Please use 'organization/' or 'enterprise/' prefix to target a single runner group.");
                            }
                            else
                            {
                                result.RunnerGroup = groupName;
                            }
                            break;
                        case WorkflowTemplateConstants.Labels:
                            ConvertToRunsOnLabels(context, runsOnToken.Value, result, isEarlyValidation);
                            break;
                    }
                }
            }

            return result;
        }

        internal static void ConvertToRunsOnLabels(
            TemplateContext context,
            TemplateToken runsOnLabelsToken,
            RunsOn runsOn,
            Boolean isEarlyValidation = false)
        {
            // Expression
            if (isEarlyValidation && runsOnLabelsToken is ExpressionToken)
            {
                return;
            }

            // String
            if (runsOnLabelsToken is StringToken runsOnLabelsString)
            {
                runsOn.Labels.Add(runsOnLabelsString.Value);
            }
            // Sequence<String>
            else if (runsOnLabelsToken is SequenceToken runsOnLabelsSequence)
            {
                foreach (var runsOnLabelToken in runsOnLabelsSequence)
                {
                    // Expression
                    if (isEarlyValidation && runsOnLabelToken is ExpressionToken)
                    {
                        continue;
                    }

                    // String
                    var label = runsOnLabelToken.AssertString($"job {WorkflowTemplateConstants.RunsOn} {WorkflowTemplateConstants.Labels} sequence item");
                    runsOn.Labels.Add(label.Value);
                }
            }
        }

        internal static Int32? ConvertToJobTimeout(
            TemplateContext context,
            TemplateToken token,
            Boolean isEarlyValidation = false)
        {
            if (isEarlyValidation && token is ExpressionToken)
            {
                return null;
            }

            var numberToken = token.AssertNumber($"job {WorkflowTemplateConstants.TimeoutMinutes}");
            return (Int32)numberToken.Value;
        }

        internal static Int32? ConvertToJobCancelTimeout(
            TemplateContext context,
            TemplateToken token,
            Boolean isEarlyValidation = false)
        {
            if (isEarlyValidation && token is ExpressionToken)
            {
                return null;
            }

            var numberToken = token.AssertNumber($"job {WorkflowTemplateConstants.CancelTimeoutMinutes}");
            return (Int32)numberToken.Value;
        }

        internal static Boolean? ConvertToJobContinueOnError(
            TemplateContext context,
            TemplateToken token,
            Boolean isEarlyValidation = false)
        {
            if (isEarlyValidation && token is ExpressionToken)
            {
                return null;
            }

            var booleanToken = token.AssertBoolean($"job {WorkflowTemplateConstants.ContinueOnError}");
            return booleanToken.Value;
        }

        internal static Boolean? ConvertToStepContinueOnError(
            TemplateContext context,
            TemplateToken token,
            Boolean isEarlyValidation = false)
        {
            if (isEarlyValidation && token is ExpressionToken)
            {
                return null;
            }

            var booleanToken = token.AssertBoolean($"step {WorkflowTemplateConstants.ContinueOnError}");
            return booleanToken.Value;
        }

        internal static String ConvertToStepName(
            TemplateContext context,
            TemplateToken token,
            Boolean isEarlyValidation = false)
        {
            if (isEarlyValidation && token is ExpressionToken)
            {
                return null;
            }

            var stringToken = token.AssertString($"step {WorkflowTemplateConstants.Name}");
            return stringToken.Value;
        }

        internal static GroupPermitSetting ConvertToConcurrency(
            TemplateContext context,
            TemplateToken concurrency,
            Boolean isEarlyValidation = false)
        {
            var result = new GroupPermitSetting("");

            // Expression
            if (isEarlyValidation && concurrency is ExpressionToken)
            {
                return result;
            }

            // String
            if (concurrency is StringToken concurrencyString)
            {
                result.Group = concurrencyString.Value;
            }
            // Mapping
            else
            {
                var concurrencyMapping = concurrency.AssertMapping($"{WorkflowTemplateConstants.Concurrency}");
                foreach (var concurrencyProperty in concurrencyMapping)
                {
                    var propertyName = concurrencyProperty.Key.AssertString($"{WorkflowTemplateConstants.Concurrency} key");

                    // Expression
                    if (isEarlyValidation && (concurrencyProperty.Value is ExpressionToken || concurrencyProperty.Key is ExpressionToken))
                    {
                        continue;
                    }

                    switch (propertyName.Value)
                    {
                        case WorkflowTemplateConstants.Group:
                            // Literal
                            var group = concurrencyProperty.Value.AssertString($"{WorkflowTemplateConstants.Group} key");
                            result.Group = group.Value;
                            break;
                        case WorkflowTemplateConstants.CancelInProgress:
                            // Literal
                            var cancelInProgress = concurrencyProperty.Value.AssertBoolean($"{WorkflowTemplateConstants.CancelInProgress} key");
                            result.CancelInProgress = cancelInProgress.Value;
                            break;
                        default:
                            propertyName.AssertUnexpectedValue($"{WorkflowTemplateConstants.Concurrency} key"); // throws
                            break;
                    }
                }
            }

            if (!isEarlyValidation && String.IsNullOrEmpty(result.Group))
            {
                context.Error(concurrency, "Concurrency group name cannot be empty");
            }

            if (result.Group?.Length > 400)
            {
                context.Error(concurrency, "Concurrency group name must be less than 400 characters");
            }

            return result;
        }

        internal static ActionsEnvironmentReference ConvertToActionEnvironmentReference(
            TemplateContext context,
            TemplateToken environment,
            bool isEarlyValidation = false)
        {
            // Expression
            if (isEarlyValidation && environment is ExpressionToken)
            {
                return null;
            }
            // String
            else if (environment is StringToken nameString)
            {
                return String.IsNullOrEmpty(nameString.Value) ? null : new ActionsEnvironmentReference(nameString.Value);
            }
            // Mapping
            else
            {
                var environmentMapping = environment.AssertMapping($"job {WorkflowTemplateConstants.Environment}");

                if (isEarlyValidation)
                {
                    // Skip early validation if any expressions other than "url" (expanded by the runner)
                    var urlToken = environmentMapping
                        .Where(x => x.Key is StringToken key && string.Equals(key.Value, WorkflowTemplateConstants.Url, StringComparison.Ordinal))
                        .Select(x => x.Value)
                        .SingleOrDefault();
                    if (isEarlyValidation && environmentMapping.Traverse().Any(x => x is ExpressionToken && x != urlToken))
                    {
                        return null;
                    }
                }

                var name = default(String);
                var url = default(TemplateToken);
                foreach (var environmentProp in environmentMapping)
                {
                    var propertyName = environmentProp.Key.AssertString($"job {WorkflowTemplateConstants.Environment} key");
                    var propertyValue = environmentProp.Value;

                    switch (propertyName.Value)
                    {
                        // Name is a required property (schema validation)
                        case WorkflowTemplateConstants.Name:
                            name = propertyValue.AssertString($"job {WorkflowTemplateConstants.Environment} {WorkflowTemplateConstants.Name}").Value;
                            break;
                        case WorkflowTemplateConstants.Url:
                            url = propertyValue;
                            break;
                        default:
                            propertyName.AssertUnexpectedValue($"job {WorkflowTemplateConstants.Environment} key"); // throws
                            break;
                    }
                }

                if (!String.IsNullOrEmpty(name))
                {
                    return new ActionsEnvironmentReference(name) { Url = url };
                }
                else
                {
                    return null;
                }
            }
        }

        internal static Dictionary<String, String> ConvertToStepEnvironment(
            TemplateContext context,
            TemplateToken environment,
            StringComparer keyComparer,
            Boolean isEarlyValidation = false)
        {
            var result = new Dictionary<String, String>(keyComparer);

            // Expression
            if (isEarlyValidation && environment is ExpressionToken)
            {
                return result;
            }

            // Mapping
            var mapping = environment.AssertMapping("environment");

            foreach (var pair in mapping)
            {
                // Expression key
                if (isEarlyValidation && pair.Key is ExpressionToken)
                {
                    continue;
                }

                // String key
                var key = pair.Key.AssertString("environment key");

                // Expression value
                if (isEarlyValidation && pair.Value is ExpressionToken)
                {
                    continue;
                }

                // String value
                var value = pair.Value.AssertString("environment value");
                result[key.Value] = value.Value;
            }

            return result;
        }

        internal static Dictionary<String, String> ConvertToStepInputs(
            TemplateContext context,
            TemplateToken inputs,
            Boolean isEarlyValidation = false)
        {
            var result = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            // Expression
            if (isEarlyValidation && inputs is ExpressionToken)
            {
                return result;
            }

            // Mapping
            var mapping = inputs.AssertMapping("inputs");

            foreach (var pair in mapping)
            {
                // Expression key
                if (isEarlyValidation && pair.Key is ExpressionToken)
                {
                    continue;
                }

                // Literal key
                var key = pair.Key.AssertString("inputs key");

                // Expression value
                if (isEarlyValidation && pair.Value is ExpressionToken)
                {
                    continue;
                }

                // Literal value
                var value = pair.Value.AssertString("inputs value");
                result[key.Value] = value.Value;
            }

            return result;
        }

        internal static Int32? ConvertToStepTimeout(
            TemplateContext context,
            TemplateToken token,
            Boolean isEarlyValidation = false)
        {
            if (isEarlyValidation && token is ExpressionToken)
            {
                return null;
            }

            var numberToken = token.AssertNumber($"step {WorkflowTemplateConstants.TimeoutMinutes}");
            return (Int32)numberToken.Value;
        }

        internal static Strategy ConvertToStrategy(
            TemplateContext context,
            TemplateToken token,
            String jobFactoryName,
            Boolean isEarlyValidation = false)
        {
            var result = new Strategy();

            // Expression
            if (isEarlyValidation && token is ExpressionToken)
            {
                return result;
            }

            var strategyMapping = token.AssertMapping(WorkflowTemplateConstants.Strategy);
            var matrixBuilder = default(MatrixBuilder);
            var hasExpressions = false;

            foreach (var strategyPair in strategyMapping)
            {
                // Expression key
                if (isEarlyValidation && strategyPair.Key is ExpressionToken)
                {
                    hasExpressions = true;
                    continue;
                }

                // Literal key
                var strategyKey = strategyPair.Key.AssertString("strategy key");

                switch (strategyKey.Value)
                {
                    // Fail-Fast
                    case WorkflowTemplateConstants.FailFast:
                        if (isEarlyValidation && strategyPair.Value is ExpressionToken)
                        {
                            hasExpressions = true;
                            continue;
                        }

                        var failFastBooleanToken = strategyPair.Value.AssertBoolean($"strategy {WorkflowTemplateConstants.FailFast}");
                        result.FailFast = failFastBooleanToken.Value;
                        break;

                    // Max-Parallel
                    case WorkflowTemplateConstants.MaxParallel:
                        if (isEarlyValidation && strategyPair.Value is ExpressionToken)
                        {
                            hasExpressions = true;
                            continue;
                        }

                        var maxParallelNumberToken = strategyPair.Value.AssertNumber($"strategy {WorkflowTemplateConstants.MaxParallel}");
                        result.MaxParallel = (Int32)maxParallelNumberToken.Value;
                        break;

                    // Matrix
                    case WorkflowTemplateConstants.Matrix:

                        // Expression
                        if (isEarlyValidation && strategyPair.Value is ExpressionToken)
                        {
                            hasExpressions = true;
                            continue;
                        }

                        var matrix = strategyPair.Value.AssertMapping("matrix");
                        hasExpressions = hasExpressions || matrix.Traverse().Any(x => x is ExpressionToken);
                        matrixBuilder = new MatrixBuilder(context, jobFactoryName);
                        var hasCrossProductVector = false;
                        var hasIncludeVector = false;

                        foreach (var matrixPair in matrix)
                        {
                            // Expression key
                            if (isEarlyValidation && matrixPair.Key is ExpressionToken)
                            {
                                hasCrossProductVector = true; // For early validation, treat as if a vector is defined
                                continue;
                            }

                            var matrixKey = matrixPair.Key.AssertString("matrix key");
                            switch (matrixKey.Value)
                            {
                                case WorkflowTemplateConstants.Include:
                                    if (isEarlyValidation && matrixPair.Value.Traverse().Any(x => x is ExpressionToken))
                                    {
                                        hasIncludeVector = true; // For early validation, treat as OK
                                        continue;
                                    }

                                    var includeSequence = matrixPair.Value.AssertSequence("matrix includes");
                                    hasIncludeVector = includeSequence.Count > 0;
                                    matrixBuilder.Include(includeSequence);
                                    break;

                                case WorkflowTemplateConstants.Exclude:
                                    if (isEarlyValidation && matrixPair.Value.Traverse().Any(x => x is ExpressionToken))
                                    {
                                        continue;
                                    }

                                    var excludeSequence = matrixPair.Value.AssertSequence("matrix excludes");
                                    matrixBuilder.Exclude(excludeSequence);
                                    break;

                                default:
                                    hasCrossProductVector = true;

                                    if (isEarlyValidation && matrixPair.Value.Traverse().Any(x => x is ExpressionToken))
                                    {
                                        continue;
                                    }

                                    var vectorName = matrixKey.Value;
                                    var vectorSequence = matrixPair.Value.AssertSequence("matrix vector value");
                                    if (vectorSequence.Count == 0)
                                    {
                                        context.Error(vectorSequence, $"Matrix vector '{vectorName}' does not contain any values");
                                    }
                                    else
                                    {
                                        matrixBuilder.AddVector(vectorName, vectorSequence);
                                    }
                                    break;
                            }
                        }

                        if (!hasCrossProductVector && !hasIncludeVector)
                        {
                            context.Error(matrix, $"Matrix must define at least one vector");
                        }

                        break;

                    default:
                        strategyKey.AssertUnexpectedValue("strategy key"); // throws
                        break;
                }
            }

            if (hasExpressions)
            {
                return result;
            }

            if (matrixBuilder != null)
            {
                result.Configurations.AddRange(matrixBuilder.Build());
            }

            for (var i = 0; i < result.Configurations.Count; i++)
            {
                var configuration = result.Configurations[i];

                var strategy = new DictionaryExpressionData()
                {
                    {
                        "fail-fast",
                        new BooleanExpressionData(result.FailFast)
                    },
                    {
                        "job-index",
                        new NumberExpressionData(i)
                    },
                    {
                        "job-total",
                        new NumberExpressionData(result.Configurations.Count)
                    }
                };

                if (result.MaxParallel > 0)
                {
                    strategy.Add(
                        "max-parallel",
                        new NumberExpressionData(result.MaxParallel)
                    );
                }
                else
                {
                    strategy.Add(
                        "max-parallel",
                        new NumberExpressionData(result.Configurations.Count)
                    );
                }

                configuration.ExpressionData.Add(WorkflowTemplateConstants.Strategy, strategy);
                context.Memory.AddBytes(WorkflowTemplateConstants.Strategy);
                context.Memory.AddBytes(strategy, traverse: true);

                if (!configuration.ExpressionData.ContainsKey(WorkflowTemplateConstants.Matrix))
                {
                    configuration.ExpressionData.Add(WorkflowTemplateConstants.Matrix, null);
                    context.Memory.AddBytes(WorkflowTemplateConstants.Matrix);
                }
            }

            return result;
        }

        internal static ContainerRegistryCredentials ConvertToContainerCredentials(TemplateToken token)
        {
            var credentials = token.AssertMapping(WorkflowTemplateConstants.Credentials);
            var result = new ContainerRegistryCredentials();
            foreach (var credentialProperty in credentials)
            {
                var propertyName = credentialProperty.Key.AssertString($"{WorkflowTemplateConstants.Credentials} key");
                switch (propertyName.Value)
                {
                    case WorkflowTemplateConstants.Username:
                        result.Username = credentialProperty.Value.AssertString(WorkflowTemplateConstants.Username).Value;
                        break;
                    case WorkflowTemplateConstants.Password:
                        result.Password = credentialProperty.Value.AssertString(WorkflowTemplateConstants.Password).Value;
                        break;
                    default:
                        propertyName.AssertUnexpectedValue($"{WorkflowTemplateConstants.Credentials} key {propertyName}");
                        break;
                }
            }

            return result;
        }

        internal static JobContainer ConvertToJobContainer(
            TemplateContext context,
            TemplateToken value,
            bool isEarlyValidation = false)
        {
            var result = new JobContainer();
            if (isEarlyValidation && value.Traverse().Any(x => x is ExpressionToken))
            {
                return result;
            }

            if (value is StringToken containerLiteral)
            {
                if (String.IsNullOrEmpty(containerLiteral.Value))
                {
                    return null;
                }

                result.Image = containerLiteral.Value;
            }
            else
            {
                var containerMapping = value.AssertMapping($"{WorkflowTemplateConstants.Container}");
                foreach (var containerPropertyPair in containerMapping)
                {
                    var propertyName = containerPropertyPair.Key.AssertString($"{WorkflowTemplateConstants.Container} key");

                    switch (propertyName.Value)
                    {
                        case WorkflowTemplateConstants.Image:
                            result.Image = containerPropertyPair.Value.AssertString($"{WorkflowTemplateConstants.Container} {propertyName}").Value;
                            break;
                        case WorkflowTemplateConstants.Env:
                            var env = containerPropertyPair.Value.AssertMapping($"{WorkflowTemplateConstants.Container} {propertyName}");
                            var envDict = new Dictionary<String, String>(env.Count);
                            foreach (var envPair in env)
                            {
                                var envKey = envPair.Key.ToString();
                                var envValue = envPair.Value.AssertString($"{WorkflowTemplateConstants.Container} {propertyName} {envPair.Key.ToString()}").Value;
                                envDict.Add(envKey, envValue);
                            }
                            result.Environment = envDict;
                            break;
                        case WorkflowTemplateConstants.Options:
                            result.Options = containerPropertyPair.Value.AssertString($"{WorkflowTemplateConstants.Container} {propertyName}").Value;
                            break;
                        case WorkflowTemplateConstants.Ports:
                            var ports = containerPropertyPair.Value.AssertSequence($"{WorkflowTemplateConstants.Container} {propertyName}");
                            var portList = new List<String>(ports.Count);
                            foreach (var portItem in ports)
                            {
                                var portString = portItem.AssertString($"{WorkflowTemplateConstants.Container} {propertyName} {portItem.ToString()}").Value;
                                portList.Add(portString);
                            }
                            result.Ports = portList;
                            break;
                        case WorkflowTemplateConstants.Volumes:
                            var volumes = containerPropertyPair.Value.AssertSequence($"{WorkflowTemplateConstants.Container} {propertyName}");
                            var volumeList = new List<String>(volumes.Count);
                            foreach (var volumeItem in volumes)
                            {
                                var volumeString = volumeItem.AssertString($"{WorkflowTemplateConstants.Container} {propertyName} {volumeItem.ToString()}").Value;
                                volumeList.Add(volumeString);
                            }
                            result.Volumes = volumeList;
                            break;
                        case WorkflowTemplateConstants.Credentials:
                            result.Credentials = ConvertToContainerCredentials(containerPropertyPair.Value);
                            break;
                        default:
                            propertyName.AssertUnexpectedValue($"{WorkflowTemplateConstants.Container} key");
                            break;
                    }
                }
            }

            if (String.IsNullOrEmpty(result.Image))
            {
                // Only error during early validation (parse time)
                // At runtime (expression evaluation), empty image = no container
                if (isEarlyValidation)
                {
                    context.Error(value, "Container image cannot be empty");
                }
                return null;
            }

            if (result.Image.StartsWith(WorkflowTemplateConstants.DockerUriPrefix, StringComparison.Ordinal))
            {
                result.Image = result.Image.Substring(WorkflowTemplateConstants.DockerUriPrefix.Length);
            }

            return result;
        }

        internal static List<KeyValuePair<String, JobContainer>> ConvertToJobServiceContainers(
            TemplateContext context,
            TemplateToken services,
            bool isEarlyValidation = false)
        {
            var result = new List<KeyValuePair<String, JobContainer>>();

            if (isEarlyValidation && services.Traverse().Any(x => x is ExpressionToken))
            {
                return result;
            }

            var servicesMapping = services.AssertMapping("services");

            foreach (var servicePair in servicesMapping)
            {
                var networkAlias = servicePair.Key.AssertString("services key").Value;
                var container = ConvertToJobContainer(context, servicePair.Value);
                result.Add(new KeyValuePair<String, JobContainer>(networkAlias, container));
            }

            return result;
        }

        private static IList<IJob> ConvertToJobs(
            TemplateContext context,
            TemplateToken workflow)
        {
            var result = new List<IJob>();
            var jobsMapping = workflow.AssertMapping(WorkflowTemplateConstants.Jobs);
            var ready = new Queue<NodeInfo>();
            var allUnsatisfied = new List<NodeInfo>();
            var jobCountValidator = context.GetJobCountValidator();
            foreach (var jobsPair in jobsMapping)
            {
                var jobId = jobsPair.Key.AssertString($"{WorkflowTemplateConstants.Jobs} key");
                jobCountValidator.Increment(jobId);
                var jobDefinition = jobsPair.Value.AssertMapping($"{WorkflowTemplateConstants.Jobs} value");
                var idBuilder = new IdBuilder();
                var job = ConvertToJob(context, jobId, jobDefinition, idBuilder);
                result.Add(job);
                var nodeInfo = new NodeInfo
                {
                    Name = job.Id!.Value,
                    Needs = new List<StringToken>(job.Needs ?? new List<StringToken>()),
                };
                if (nodeInfo.Needs.Count == 0)
                {
                    ready.Enqueue(nodeInfo);
                }
                else
                {
                    allUnsatisfied.Add(nodeInfo);
                }
            }

            if (context.Errors.Count != 0)
            {
                return result;
            }

            if (ready.Count == 0)
            {
                context.Error(jobsMapping, "The workflow must contain at least one job with no dependencies.");
                return result;
            }

            while (ready.Count > 0)
            {
                var current = ready.Dequeue();

                // Figure out which nodes would start after current completes
                for (var i = allUnsatisfied.Count - 1; i >= 0; i--)
                {
                    var unsatisfied = allUnsatisfied[i];
                    for (var j = unsatisfied.Needs.Count - 1; j >= 0; j--)
                    {
                        if (String.Equals(unsatisfied.Needs[j].Value, current.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            unsatisfied.Needs.RemoveAt(j);
                            if (unsatisfied.Needs.Count == 0)
                            {
                                ready.Enqueue(unsatisfied);
                                allUnsatisfied.RemoveAt(i);
                            }
                        }
                    }
                }
            }

            // Check whether some jobs will never execute
            if (allUnsatisfied.Count > 0)
            {
                var names = result.ToHashSet(x => x.Id!.Value, StringComparer.OrdinalIgnoreCase);
                foreach (var unsatisfied in allUnsatisfied)
                {
                    foreach (var need in unsatisfied.Needs)
                    {
                        if (names.Contains(need.Value))
                        {
                            context.Error(need, $"Job '{unsatisfied.Name}' depends on job '{need.Value}' which creates a cycle in the dependency graph.");
                        }
                        else
                        {
                            context.Error(need, $"Job '{unsatisfied.Name}' depends on unknown job '{need.Value}'.");
                        }
                    }
                }
            }

            return result;
        }

        private static IJob ConvertToJob(
            TemplateContext context,
            StringToken jobId,
            MappingToken jobDefinition,
            IdBuilder idBuilder)
        {
            if (!idBuilder.TryAddKnownId(jobId.Value, out var error))
            {
                context.Error(jobId, error);
            }

            var condition = new BasicExpressionToken(null, null, null, $"{WorkflowTemplateConstants.Success}()");
            var continueOnError = default(ScalarToken);
            var env = default(TemplateToken);
            var name = default(ScalarToken);
            var jobTarget = default(TemplateToken);
            var steps = new List<IStep>();
            var strategy = default(TemplateToken);
            var jobContainer = default(TemplateToken);
            var jobServiceContainers = default(TemplateToken);
            var concurrency = default(TemplateToken);
            var actionsEnvironment = default(TemplateToken);
            var defaults = default(TemplateToken);
            var permissions = default(Permissions);
            var outputs = default(TemplateToken);
            var jobTimeout = default(ScalarToken);
            var jobCancelTimeout = default(ScalarToken);
            var snapshot = default(TemplateToken);
            var needs = new List<StringToken>();

            var workflowJobRef = default(StringToken);
            var workflowJobInputs = default(MappingToken);
            var workflowJobSecrets = default(MappingToken);
            var workflowJobSecretsInherited = false;

            foreach (var jobProperty in jobDefinition)
            {
                var propertyName = jobProperty.Key.AssertString($"job property name");

                switch (propertyName.Value)
                {
                    case WorkflowTemplateConstants.ContinueOnError:
                        ConvertToJobContinueOnError(context, jobProperty.Value, isEarlyValidation: true); // Validate early if possible
                        continueOnError = jobProperty.Value.AssertScalar($"job {WorkflowTemplateConstants.ContinueOnError}");
                        break;

                    case WorkflowTemplateConstants.If:
                        condition = ConvertToIfCondition(context, jobProperty.Value, IfKind.Job);
                        break;

                    case WorkflowTemplateConstants.Name:
                        name = jobProperty.Value.AssertScalar($"job {WorkflowTemplateConstants.Name}");
                        ConvertToJobName(context, name, isEarlyValidation: true); // Validate early if possible
                        break;

                    case WorkflowTemplateConstants.Needs:
                        if (jobProperty.Value is StringToken needsLiteral)
                        {
                            needs.Add(needsLiteral);
                        }
                        else
                        {
                            var needsSeq = jobProperty.Value.AssertSequence($"job {WorkflowTemplateConstants.Needs}");
                            foreach (var needsItem in needsSeq)
                            {
                                var need = needsItem.AssertString($"job {WorkflowTemplateConstants.Needs} item");
                                needs.Add(need);
                            }
                        }
                        break;

                    case WorkflowTemplateConstants.RunsOn:
                        ConvertToRunsOn(context, jobProperty.Value, isEarlyValidation: true); // Validate early if possible
                        jobTarget = jobProperty.Value;
                        break;

                    case WorkflowTemplateConstants.Snapshot:
                        if (!context.GetFeatures().Snapshot)
                        {
                            context.Error(jobProperty.Key, $"The key '{WorkflowTemplateConstants.Snapshot}' is not allowed");
                            break;
                        }

                        ConvertToSnapshot(context, jobProperty.Value, isEarlyValidation: true); // Validate early if possible
                        snapshot = jobProperty.Value;
                        break;

                    case WorkflowTemplateConstants.Steps:
                        steps.AddRange(ConvertToSteps(context, jobProperty.Value));
                        break;

                    case WorkflowTemplateConstants.Strategy:
                        ConvertToStrategy(context, jobProperty.Value, null, isEarlyValidation: true); // Validate early if possible
                        strategy = jobProperty.Value;
                        break;

                    case WorkflowTemplateConstants.TimeoutMinutes:
                        ConvertToJobTimeout(context, jobProperty.Value, isEarlyValidation: true); // Validate early if possible
                        jobTimeout = jobProperty.Value as ScalarToken;
                        break;

                    case WorkflowTemplateConstants.CancelTimeoutMinutes:
                        ConvertToJobCancelTimeout(context, jobProperty.Value, isEarlyValidation: true); // Validate early if possible
                        jobCancelTimeout = jobProperty.Value as ScalarToken;
                        break;

                    case WorkflowTemplateConstants.Container:
                        ConvertToJobContainer(context, jobProperty.Value, isEarlyValidation: true); // Validate early if possible
                        jobContainer = jobProperty.Value;
                        break;

                    case WorkflowTemplateConstants.Services:
                        ConvertToJobServiceContainers(context, jobProperty.Value, isEarlyValidation: true); // Validate early if possible
                        jobServiceContainers = jobProperty.Value;
                        break;

                    case WorkflowTemplateConstants.Concurrency:
                        ConvertToConcurrency(context, jobProperty.Value, isEarlyValidation: true); // Validate early if possible
                        concurrency = jobProperty.Value;
                        break;
                    case WorkflowTemplateConstants.Env:
                        env = jobProperty.Value;
                        break;

                    case WorkflowTemplateConstants.Environment:
                        ConvertToActionEnvironmentReference(context, jobProperty.Value, isEarlyValidation: true); // Validate early if possible
                        actionsEnvironment = jobProperty.Value;
                        break;

                    case WorkflowTemplateConstants.Outputs:
                        outputs = jobProperty.Value;
                        break;

                    case WorkflowTemplateConstants.Defaults:
                        defaults = jobProperty.Value;
                        break;

                    case WorkflowTemplateConstants.Permissions:
                        permissions = ConvertToPermissions(context, jobProperty.Value);
                        break;

                    case WorkflowTemplateConstants.Uses:
                        workflowJobRef = jobProperty.Value.AssertString($"job {WorkflowTemplateConstants.Uses} value");
                        break;

                    case WorkflowTemplateConstants.With:
                        workflowJobInputs = jobProperty.Value.AssertMapping($"{WorkflowTemplateConstants.Uses}-{WorkflowTemplateConstants.With} value");
                        break;

                    case WorkflowTemplateConstants.Secrets:
                        // String in case inherit is used
                        if (jobProperty.Value is StringToken sToken
                            && sToken.Value == WorkflowTemplateConstants.Inherit)
                        {
                            workflowJobSecretsInherited = true;
                        }
                        else
                        {
                            workflowJobSecrets = jobProperty.Value.AssertMapping($"{WorkflowTemplateConstants.Uses}-{WorkflowTemplateConstants.Secrets} value");
                        }

                        break;

                    default:
                        propertyName.AssertUnexpectedValue("job key"); // throws
                        break;
                }
            }

            if (workflowJobRef != null)
            {
                var workflowJob = new ReusableWorkflowJob
                {
                    Id = jobId,
                    Name = name,
                    If = condition,
                    Ref = workflowJobRef,
                    InputValues = workflowJobInputs,
                    SecretValues = workflowJobSecrets,
                    InheritSecrets = workflowJobSecretsInherited,
                    Permissions = permissions,
                    Concurrency = concurrency,
                    Strategy = strategy,
                };
                if (workflowJob.Name is null || workflowJob.Name is StringToken nameStr && String.IsNullOrEmpty(nameStr.Value))
                {
                    workflowJob.Name = workflowJob.Id;
                }
                workflowJob.Needs.AddRange(needs);

                return workflowJob;
            }
            else
            {
                var result = new Job
                {
                    Id = jobId,
                    Name = name,
                    ContinueOnError = continueOnError,
                    If = condition,
                    RunsOn = jobTarget,
                    Strategy = strategy,
                    TimeoutMinutes = jobTimeout,
                    CancelTimeoutMinutes = jobCancelTimeout,
                    Container = jobContainer,
                    Services = jobServiceContainers,
                    Concurrency = concurrency,
                    Env = env,
                    Environment = actionsEnvironment,
                    Outputs = outputs,
                    Defaults = defaults,
                    Permissions = permissions,
                    Snapshot = snapshot,
                };
                if (result.Name is null || result.Name is StringToken nameStr && String.IsNullOrEmpty(nameStr.Value))
                {
                    result.Name = result.Id;
                }

                result.Needs.AddRange(needs);
                result.Steps.AddRange(steps);
                return result;
            }
        }

        internal static IDictionary<String, String> ConvertToWorkflowJobSecrets(
            TemplateContext context,
            TemplateToken secrets)
        {
            var result = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            var mapping = secrets.AssertMapping("workflow job secrets");
            foreach (var pair in mapping)
            {
                var key = pair.Key.AssertString("workflow job secret key").Value;
                var value = pair.Value.AssertString("workflow job secret value").Value;
                if (!String.IsNullOrEmpty(value))
                {
                    result[key] = value;
                }
            }

            return result;
        }

        // Public because used by runner for composite actions
        public static List<IStep> ConvertToSteps(
            TemplateContext context,
            TemplateToken steps)
        {
            var stepsSequence = steps.AssertSequence($"job {WorkflowTemplateConstants.Steps}");

            var idBuilder = new IdBuilder();
            var result = stepsSequence.Select(x => ConvertToStep(context, x, idBuilder)).ToList();

            // Generate default IDs when empty
            foreach (IStep step in result)
            {
                if (!string.IsNullOrEmpty(step.Id))
                {
                    continue;
                }

                var id = default(string);
                if (step is ActionStep action)
                {
                    if (action.Uses!.Value.StartsWith(WorkflowTemplateConstants.DockerUriPrefix, StringComparison.Ordinal))
                    {
                        id = action.Uses!.Value.Substring(WorkflowTemplateConstants.DockerUriPrefix.Length);
                    }
                    else if (action.Uses!.Value.StartsWith("./") || action.Uses!.Value.StartsWith(".\\"))
                    {
                        id = WorkflowConstants.SelfAlias;
                    }
                    else
                    {
                        var usesSegments = action.Uses!.Value.Split('@');
                        var pathSegments = usesSegments[0].Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        var gitRef = usesSegments.Length == 2 ? usesSegments[1] : String.Empty;

                        if (usesSegments.Length == 2 &&
                            pathSegments.Length >= 2 &&
                            !string.IsNullOrEmpty(pathSegments[0]) &&
                            !string.IsNullOrEmpty(pathSegments[1]) &&
                            !string.IsNullOrEmpty(gitRef))
                        {
                            id = $"{pathSegments[0]}/{pathSegments[1]}";
                        }
                    }
                }

                if (string.IsNullOrEmpty(id))
                {
                    id = "run";
                }

                idBuilder.AppendSegment($"__{id}");

                // Allow reserved prefix "__" for default IDs
                step.Id = idBuilder.Build(allowReservedPrefix: true);
            }

            return result;
        }

        private static IStep ConvertToStep(
            TemplateContext context,
            TemplateToken stepsItem,
            IdBuilder idBuilder)
        {
            var step = stepsItem.AssertMapping($"{WorkflowTemplateConstants.Steps} item");
            var continueOnError = default(ScalarToken);
            var env = default(TemplateToken);
            var id = default(StringToken);
            var ifCondition = default(BasicExpressionToken);
            var ifToken = default(ScalarToken);
            var name = default(ScalarToken);
            var run = default(ScalarToken);
            var timeoutMinutes = default(ScalarToken);
            var uses = default(StringToken);
            var with = default(TemplateToken);
            var workingDir = default(ScalarToken);
            var shell = default(ScalarToken);

            foreach (var stepProperty in step)
            {
                var propertyName = stepProperty.Key.AssertString($"{WorkflowTemplateConstants.Steps} item key");

                switch (propertyName.Value)
                {
                    case WorkflowTemplateConstants.ContinueOnError:
                        ConvertToStepContinueOnError(context, stepProperty.Value, isEarlyValidation: true); // Validate early if possible
                        continueOnError = stepProperty.Value.AssertScalar($"{WorkflowTemplateConstants.Steps} {WorkflowTemplateConstants.ContinueOnError}");
                        break;

                    case WorkflowTemplateConstants.Env:
                        ConvertToStepEnvironment(context, stepProperty.Value, StringComparer.Ordinal, isEarlyValidation: true); // Validate early if possible
                        env = stepProperty.Value;
                        break;

                    case WorkflowTemplateConstants.Id:
                        id = stepProperty.Value.AssertString($"{WorkflowTemplateConstants.Steps} item {WorkflowTemplateConstants.Id}");
                        if (!String.IsNullOrEmpty(id.Value) &&
                            !idBuilder.TryAddKnownId(id.Value, out var error))
                        {
                            context.Error(id, error);
                        }
                        break;

                    case WorkflowTemplateConstants.If:
                        ifToken = stepProperty.Value.AssertScalar($"{WorkflowTemplateConstants.Steps} item {WorkflowTemplateConstants.If}");
                        break;

                    case WorkflowTemplateConstants.Name:
                        name = stepProperty.Value.AssertScalar($"{WorkflowTemplateConstants.Steps} item {WorkflowTemplateConstants.Name}");
                        break;

                    case WorkflowTemplateConstants.Run:
                        run = stepProperty.Value.AssertScalar($"{WorkflowTemplateConstants.Steps} item {WorkflowTemplateConstants.Run}");
                        break;

                    case WorkflowTemplateConstants.Shell:
                        shell = stepProperty.Value.AssertScalar($"{WorkflowTemplateConstants.Steps} item {WorkflowTemplateConstants.Shell}");
                        break;

                    case WorkflowTemplateConstants.TimeoutMinutes:
                        ConvertToStepTimeout(context, stepProperty.Value, isEarlyValidation: true); // Validate early if possible
                        timeoutMinutes = stepProperty.Value.AssertScalar($"{WorkflowTemplateConstants.Steps} item {WorkflowTemplateConstants.TimeoutMinutes}");
                        break;

                    case WorkflowTemplateConstants.Uses:
                        uses = stepProperty.Value.AssertString($"{WorkflowTemplateConstants.Steps} item {WorkflowTemplateConstants.Uses}");
                        break;

                    case WorkflowTemplateConstants.With:
                        ConvertToStepInputs(context, stepProperty.Value, isEarlyValidation: true); // Validate early if possible
                        with = stepProperty.Value;
                        break;

                    case WorkflowTemplateConstants.WorkingDirectory:
                        workingDir = stepProperty.Value.AssertScalar($"{WorkflowTemplateConstants.Steps} item {WorkflowTemplateConstants.WorkingDirectory}");
                        break;

                    default:
                        propertyName.AssertUnexpectedValue($"{WorkflowTemplateConstants.Steps} item key"); // throws
                        break;
                }
            }

            // Fixup the if-condition
            ifCondition = ConvertToIfCondition(context, ifToken, IfKind.Step);

            if (run != null)
            {
                return new RunStep
                {
                    Id = id?.Value,
                    ContinueOnError = continueOnError,
                    Name = name,
                    If = ifCondition,
                    TimeoutMinutes = timeoutMinutes,
                    Env = env,
                    WorkingDirectory = workingDir,
                    Shell = shell,
                    Run = run,
                };
            }
            else
            {
                uses.AssertString($"{WorkflowTemplateConstants.Steps} item {WorkflowTemplateConstants.Uses}");
                var result = new ActionStep
                {
                    Id = id?.Value,
                    ContinueOnError = continueOnError,
                    Name = name,
                    If = ifCondition,
                    TimeoutMinutes = timeoutMinutes,
                    Env = env,
                    Uses = uses,
                    With = with,
                };

                if (!uses.Value.StartsWith(WorkflowTemplateConstants.DockerUriPrefix, StringComparison.Ordinal) &&
                    !uses.Value.StartsWith("./") &&
                    !uses.Value.StartsWith(".\\"))
                {
                    var usesSegments = uses.Value.Split('@');
                    var pathSegments = usesSegments[0].Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    var gitRef = usesSegments.Length == 2 ? usesSegments[1] : String.Empty;

                    if (usesSegments.Length != 2 ||
                        pathSegments.Length < 2 ||
                        String.IsNullOrEmpty(pathSegments[0]) ||
                        String.IsNullOrEmpty(pathSegments[1]) ||
                        String.IsNullOrEmpty(gitRef))
                    {
                        context.Error(uses, $"Expected format {{org}}/{{repo}}[/path]@ref. Actual '{uses.Value}'");
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// When empty, default to "success()".
        /// When a status function is not referenced, format as "success() &amp;&amp; &lt;CONDITION&gt;".
        /// </summary>
        private static BasicExpressionToken ConvertToIfCondition(
            TemplateContext context,
            TemplateToken token,
            IfKind ifKind)
        {
            String condition;
            if (token is null)
            {
                condition = null;
            }
            else if (token is BasicExpressionToken expressionToken)
            {
                condition = expressionToken.Expression;
            }
            else
            {
                var stringToken = token.AssertString($"{ifKind} {WorkflowTemplateConstants.If}");
                condition = stringToken.Value;
            }

            if (String.IsNullOrWhiteSpace(condition))
            {
                return new BasicExpressionToken(token?.FileId, token?.Line, token?.Column, $"{WorkflowTemplateConstants.Success}()");
            }

            var expressionParser = new ExpressionParser();
            var functions = default(IFunctionInfo[]);
            var namedValues = default(INamedValueInfo[]);
            switch (ifKind)
            {
                case IfKind.Job:
                    namedValues = s_jobIfNamedValues;
                    functions = s_jobConditionFunctions;
                    break;
                case IfKind.Step:
                    namedValues = s_stepNamedValues;
                    functions = s_stepConditionFunctions;
                    break;
                case IfKind.Snapshot:
                    namedValues = s_snapshotIfNamedValues;
                    functions = s_snapshotConditionFunctions;
                    break;
                default:
                    throw new ArgumentException($"Unexpected IfKind Enum value '{ifKind}' encountered while translating the token '{token}' to an IfCondition.");
            }

            var node = default(ExpressionNode);
            try
            {
                node = expressionParser.CreateTree(condition, null, namedValues, functions, allowCaseFunction: context.AllowCaseFunction) as ExpressionNode;
            }
            catch (Exception ex)
            {
                context.Error(token, ex);
                return null;
            }

            if (node == null)
            {
                return new BasicExpressionToken(token?.FileId, token?.Line, token?.Column, $"{WorkflowTemplateConstants.Success}()");
            }

            var hasStatusFunction = node.Traverse().Any(x =>
            {
                if (x is Function function)
                {
                    return String.Equals(function.Name, WorkflowTemplateConstants.Always, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(function.Name, WorkflowTemplateConstants.Cancelled, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(function.Name, WorkflowTemplateConstants.Failure, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(function.Name, WorkflowTemplateConstants.Success, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            });

            var finalCondition = hasStatusFunction ? condition : $"{WorkflowTemplateConstants.Success}() && ({condition})";
            return new BasicExpressionToken(token?.FileId, token?.Line, token?.Column, finalCondition);
        }


        private static Permissions ConvertToPermissions(TemplateContext context, TemplateToken token)
        {
            if (token is StringToken)
            {
                var permissionLevel = PermissionLevel.NoAccess;
                var permissionsStr = token.AssertString("permissions");
                switch (permissionsStr.Value)
                {
                    case "read-all":
                        permissionLevel = PermissionLevel.Read;
                        break;
                    case "write-all":
                        permissionLevel = PermissionLevel.Write;
                        break;
                    default:
                        permissionsStr.AssertUnexpectedValue(permissionsStr.Value);
                        break;
                }
                return new Permissions(permissionLevel, includeIdToken: true, includeAttestations: true, includeModels: context.GetFeatures().AllowModelsPermission);
            }

            var mapping = token.AssertMapping("permissions");
            var permissions = new Permissions();
            foreach (var pair in mapping)
            {
                var key = pair.Key.AssertString("permissions.key");
                var permissionLevel = ConvertToPermissionLevel(context, pair.Value);
                switch (key.Value)
                {
                    case "actions":
                        permissions.Actions = permissionLevel;
                        break;
                    case "artifact-metadata":
                        permissions.ArtifactMetadata = permissionLevel;
                        break;
                    case "attestations":
                        permissions.Attestations = permissionLevel;
                        break;
                    case "checks":
                        permissions.Checks = permissionLevel;
                        break;
                    case "contents":
                        permissions.Contents = permissionLevel;
                        break;
                    case "deployments":
                        permissions.Deployments = permissionLevel;
                        break;
                    case "issues":
                        permissions.Issues = permissionLevel;
                        break;
                    case "discussions":
                        permissions.Discussions = permissionLevel;
                        break;
                    case "packages":
                        permissions.Packages = permissionLevel;
                        break;
                    case "pages":
                        permissions.Pages = permissionLevel;
                        break;
                    case "pull-requests":
                        permissions.PullRequests = permissionLevel;
                        break;
                    case "repository-projects":
                        permissions.RepositoryProjects = permissionLevel;
                        break;
                    case "statuses":
                        permissions.Statuses = permissionLevel;
                        break;
                    case "security-events":
                        permissions.SecurityEvents = permissionLevel;
                        break;
                    case "id-token":
                        if (context.GetFeatures().IdToken)
                        {
                            permissions.IdToken = permissionLevel;
                        }
                        else
                        {
                            context.Error(key, $"The key 'id-token' is not allowed");
                        }
                        break;
                    case "models":
                        if (context.GetFeatures().AllowModelsPermission)
                        {
                            if (permissionLevel == PermissionLevel.Write)
                            {
                                permissions.Models = PermissionLevel.Read;
                            }
                            else
                            {
                                permissions.Models = permissionLevel;
                            }
                        }
                        else
                        {
                            context.Error(key, $"The permission 'models' is not allowed");
                        }
                        break;
                    default:
                        break;
                }
            }

            return permissions;
        }

        private static PermissionLevel ConvertToPermissionLevel(
            TemplateContext context,
            TemplateToken token)
        {
            var level = token.AssertString("permissions.value");
            switch (level.Value)
            {
                case "none":
                    return PermissionLevel.NoAccess;
                case "read":
                    return PermissionLevel.Read;
                case "write":
                    return PermissionLevel.Write;
                default:
                    level.AssertUnexpectedValue(level.Value);
                    return PermissionLevel.NoAccess;
            }
        }

        private static void ValidateWorkflowJobTrigger(
            TemplateContext context,
            ReusableWorkflowJob workflowJob)
        {
            ConvertToWorkflowJobInputs(context, workflowJob.InputDefinitions, workflowJob.InputValues, workflowJob, isEarlyValidation: true);
            ValidateWorkflowJobSecrets(context, workflowJob.SecretDefinitions, workflowJob.SecretValues, workflowJob);
        }

        private static ExpressionData ConvertToInputValueDefinedType(
            TemplateContext context,
            string key,
            StringToken definedType,
            TemplateToken token,
            Boolean isEarlyValidation = false)
        {
            var inputType = default(string);

            switch (definedType.Value)
            {
                case WorkflowTemplateConstants.TypeBoolean:
                    inputType = WorkflowTemplateConstants.BooleanNeedsContext;

                    break;
                case WorkflowTemplateConstants.TypeNumber:
                    inputType = WorkflowTemplateConstants.NumberNeedsContext;

                    break;
                case WorkflowTemplateConstants.TypeString:
                    inputType = WorkflowTemplateConstants.StringNeedsContext;

                    break;
                default:
                    // The schema for workflow_call.inputs only allows boolean, string, or number.
                    // We should have failed earlier if we receive any other type.
                    throw new ArgumentException($"Unexpected defined type '{definedType.Value}' when converting input value for '{key}'");
            }

            // Leverage the templating library to coerce or error
            //
            // During early validation, we're not actually evaluating any expressions with this call.
            // Any allowed contexts (i.e. "github"/"inputs") have not been added yet, so the TemplateEvaluator
            // will not unravel any expressions.
            //
            // During runtime, the expressions have already been expanded.
            var result = TemplateEvaluator.Evaluate(
                context,
                inputType,
                token,
                context.Memory.CalculateBytes(token), // Remove the size of the template token that is being replaced
                token.FileId
            );

            if (isEarlyValidation && token.Traverse().Any(x => x is ExpressionToken))
            {
                return null;
            }

            return result.ToExpressionData();
        }

        internal static IDictionary<String, String> ConvertToWorkflowJobOutputs(TemplateToken workflowJobOutputDefinitions)
        {
            var result = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            var outputs = workflowJobOutputDefinitions
                .AssertMapping("workflow job output definitions")
                .ToDictionary(
                    x => x.Key.AssertString("outputs key").Value,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (var definition in outputs)
            {
                var spec = definition.Value.AssertMapping("workflow job output spec").ToDictionary(
                    x => x.Key.AssertString("outputs spec key").Value,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase
                );

                var value = spec["value"].AssertString("workflow job output value").Value;

                result.Add(definition.Key, value);
            }

            return result;
        }

        internal static DictionaryExpressionData ConvertToWorkflowJobInputs(
            TemplateContext context,
            TemplateToken workflowJobInputDefinitions,
            TemplateToken workflowJobInputValues,
            ReusableWorkflowJob workflowJob,
            Boolean isEarlyValidation = false)
        {
            var result = default(DictionaryExpressionData);
            var inputDefinitions = workflowJobInputDefinitions?
                .AssertMapping("workflow job input definitions")
                .ToDictionary(
                    x => x.Key.AssertString("inputs key").Value,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase
                );

            var inputValues = workflowJobInputValues?
                .AssertMapping("workflow job input values")
                .ToDictionary(
                    x => x.Key.AssertString("with key").Value,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase
                );

            if (inputDefinitions != null)
            {
                result = new DictionaryExpressionData();
                foreach (var definedItem in inputDefinitions)
                {
                    string definedKey = definedItem.Key;
                    var definedInputSpec = definedItem.Value.AssertMapping($"input {definedKey}").ToDictionary(
                            x => x.Key.AssertString($"input {definedKey} key").Value,
                            x => x.Value,
                            StringComparer.OrdinalIgnoreCase);

                    var inputSpecType = definedInputSpec[WorkflowTemplateConstants.Type].AssertString($"inputs {definedKey} type"); // must exist, per schema

                    // if default provided, check with the defined type
                    if (definedInputSpec.TryGetValue(WorkflowTemplateConstants.Default, out TemplateToken defaultValue))
                    {
                        var value = ConvertToInputValueDefinedType(context, definedKey, inputSpecType, defaultValue, isEarlyValidation);
                        if (!isEarlyValidation)
                        {
                            result.Add(definedKey, value);
                        }
                    }
                    else if (!isEarlyValidation)
                    {
                        result.Add(definedKey, GetDefaultValueByType(inputSpecType).ToExpressionData());
                    }

                    // if input provided, check with defined type and continue
                    if (inputValues != null && inputValues.TryGetValue(definedKey, out TemplateToken inputValue))
                    {
                        var value = ConvertToInputValueDefinedType(context, definedKey, inputSpecType, inputValue, isEarlyValidation);
                        if (!isEarlyValidation)
                        {
                            result[definedKey] = value;
                        }
                        continue;
                    }

                    // if input required but not provided, error out
                    if (isEarlyValidation
                        && definedInputSpec.TryGetValue(WorkflowTemplateConstants.Required, out TemplateToken requiredToken)
                        && requiredToken.AssertBoolean(WorkflowTemplateConstants.Required).Value)
                    {
                        context.Error(workflowJob.Ref, $"Input {definedKey} is required, but not provided while calling.");
                        continue;
                    }
                }
            }

            // Validating if any undefined inputs are provided
            ValidateUndefinedParameters(context, inputDefinitions, inputValues, "input");

            return result;
        }

        private static void ValidateWorkflowJobSecrets(
           TemplateContext context,
           MappingToken workflowJobSecretDefinitions,
           MappingToken workflowJobSecretValues,
           ReusableWorkflowJob workflowJob)
        {
            // if the secrets are inherited from the caller, we do not have any workflowJob.SecretValues (i.e. explicit mapping)
            // Inherited org/repo/env secrets will be stored in context variables and will be validated there
            if (workflowJob.InheritSecrets)
            {
                return;
            }

            var secretDefinitions = workflowJobSecretDefinitions?.ToDictionary(
                x => x.Key.AssertString("secrets key").Value,
                x => x.Value, StringComparer.OrdinalIgnoreCase);

            var secretValues = workflowJobSecretValues?.ToDictionary(
                x => x.Key.AssertString("secrets key").Value,
                x => x.Value, StringComparer.OrdinalIgnoreCase);

            if (secretDefinitions != null)
            {
                foreach (var definedItem in secretDefinitions)
                {
                    if (definedItem.Value is NullToken nullToken)
                    {
                        continue;
                    }

                    var definedKey = definedItem.Key.ToString();
                    var definedSecretSpec = definedItem.Value.AssertMapping($"secret {definedKey}").ToDictionary(
                            x => x.Key.AssertString($"secret {definedKey} key").Value,
                            x => x.Value, StringComparer.OrdinalIgnoreCase);

                    // if secret provided, continue
                    if (secretValues != null && secretValues.TryGetValue(definedKey, out TemplateToken secretValue))
                    {
                        continue;
                    }

                    // if secret required but not provided, error out
                    if (definedSecretSpec.TryGetValue(WorkflowTemplateConstants.Required, out TemplateToken requiredToken)
                    && requiredToken.AssertBoolean(WorkflowTemplateConstants.Required).Value)
                    {
                        context.Error(workflowJob.Ref, $"Secret {definedKey} is required, but not provided while calling.");
                    }
                }
            }

            // Validating if any undefined secrets are provided
            ValidateUndefinedParameters(context, secretDefinitions, secretValues, WorkflowTemplateConstants.Secret);
        }

        private static void ValidateUndefinedParameters(
           TemplateContext context,
           Dictionary<string, TemplateToken> definitions,
           Dictionary<string, TemplateToken> providedValues,
           string parameterType)
        {
            if (providedValues != null)
            {
                foreach (var providedValue in providedValues)
                {
                    var providedKey = providedValue.Key;
                    if (definitions == null || !definitions.TryGetValue(providedKey, out TemplateToken value))
                    {
                        context.Error(providedValue.Value, $"Invalid {parameterType}, {providedKey} is not defined in the referenced workflow.");
                    }
                }
            }
        }

        private static TemplateToken GetDefaultValueByType(StringToken type)
        {
            return type.Value switch
            {
                WorkflowTemplateConstants.TypeString => new StringToken(type.FileId, type.Line, type.Column, string.Empty),
                WorkflowTemplateConstants.TypeBoolean => new BooleanToken(type.FileId, type.Line, type.Column, false),
                WorkflowTemplateConstants.TypeNumber => new NumberToken(type.FileId, type.Line, type.Column, 0.0),
                _ => null,
            };
        }

        private sealed class NodeInfo
        {
            public String Name { get; set; }
            public List<StringToken> Needs { get; set; }
        }

        private enum IfKind
        {
            Job = 0,
            Step = 1,
            Snapshot = 2
        }
        private static readonly INamedValueInfo[] s_jobIfNamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.GitHub),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Vars),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Inputs),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Needs),
        };
        private static readonly INamedValueInfo[] s_stepNamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.GitHub),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Vars),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Inputs),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Needs),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Strategy),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Matrix),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Steps),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Job),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Runner),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Env),
        };
        private static readonly INamedValueInfo[] s_snapshotIfNamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.GitHub),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Vars),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Inputs),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Needs),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Strategy),
            new NamedValueInfo<NoOperationNamedValue>(WorkflowTemplateConstants.Matrix),
        };
        private static readonly IFunctionInfo[] s_jobConditionFunctions = new IFunctionInfo[]
        {
            new FunctionInfo<NoOperation>(WorkflowTemplateConstants.Always, 0, 0),
            new FunctionInfo<NoOperation>(WorkflowTemplateConstants.Failure, 0, Int32.MaxValue),
            new FunctionInfo<NoOperation>(WorkflowTemplateConstants.Cancelled, 0, 0),
            new FunctionInfo<NoOperation>(WorkflowTemplateConstants.Success, 0, Int32.MaxValue),
        };
        private static readonly IFunctionInfo[] s_stepConditionFunctions = new IFunctionInfo[]
        {
            new FunctionInfo<NoOperation>(WorkflowTemplateConstants.Always, 0, 0),
            new FunctionInfo<NoOperation>(WorkflowTemplateConstants.Cancelled, 0, 0),
            new FunctionInfo<NoOperation>(WorkflowTemplateConstants.Failure, 0, 0),
            new FunctionInfo<NoOperation>(WorkflowTemplateConstants.Success, 0, 0),
            new FunctionInfo<NoOperation>(WorkflowTemplateConstants.HashFiles, 1, Byte.MaxValue),
        };
        private static readonly IFunctionInfo[] s_snapshotConditionFunctions = null;
    }
}
