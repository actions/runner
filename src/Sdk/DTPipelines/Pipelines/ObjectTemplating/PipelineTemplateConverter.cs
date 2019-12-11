using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Expressions2.Sdk.Functions;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.Services.Common;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    internal static class PipelineTemplateConverter
    {
        internal static String ConvertToJobDisplayName(
            TemplateContext context,
            TemplateToken displayName,
            Boolean allowExpressions = false)
        {
            var result = default(String);

            // Expression
            if (allowExpressions && displayName is ExpressionToken)
            {
                return result;
            }

            // String
            var displayNameString = displayName.AssertString($"job {PipelineTemplateConstants.Name}");
            result = displayNameString.Value;
            return result;
        }
        internal static Int32? ConvertToJobTimeout(
            TemplateContext context,
            TemplateToken token,
            Boolean allowExpressions = false)
        {
            if (allowExpressions && token is ExpressionToken)
            {
                return null;
            }

            var numberToken = token.AssertNumber($"job {PipelineTemplateConstants.TimeoutMinutes}");
            return (Int32)numberToken.Value;
        }

        internal static Int32? ConvertToJobCancelTimeout(
            TemplateContext context,
            TemplateToken token,
            Boolean allowExpressions = false)
        {
            if (allowExpressions && token is ExpressionToken)
            {
                return null;
            }

            var numberToken = token.AssertNumber($"job {PipelineTemplateConstants.CancelTimeoutMinutes}");
            return (Int32)numberToken.Value;
        }

        internal static Boolean? ConvertToStepContinueOnError(
            TemplateContext context,
            TemplateToken token,
            Boolean allowExpressions = false)
        {
            if (allowExpressions && token is ExpressionToken)
            {
                return null;
            }

            var booleanToken = token.AssertBoolean($"step {PipelineTemplateConstants.ContinueOnError}");
            return booleanToken.Value;
        }

        internal static String ConvertToStepDisplayName(
            TemplateContext context,
            TemplateToken token,
            Boolean allowExpressions = false)
        {
            if (allowExpressions && token is ExpressionToken)
            {
                return null;
            }

            var stringToken = token.AssertString($"step {PipelineTemplateConstants.Name}");
            return stringToken.Value;
        }

        internal static Dictionary<String, String> ConvertToStepEnvironment(
            TemplateContext context,
            TemplateToken environment,
            StringComparer keyComparer,
            Boolean allowExpressions = false)
        {
            var result = new Dictionary<String, String>(keyComparer);

            // Expression
            if (allowExpressions && environment is ExpressionToken)
            {
                return result;
            }

            // Mapping
            var mapping = environment.AssertMapping("environment");

            foreach (var pair in mapping)
            {
                // Expression key
                if (allowExpressions && pair.Key is ExpressionToken)
                {
                    continue;
                }

                // String key
                var key = pair.Key.AssertString("environment key");

                // Expression value
                if (allowExpressions && pair.Value is ExpressionToken)
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
            Boolean allowExpressions = false)
        {
            var result = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            // Expression
            if (allowExpressions && inputs is ExpressionToken)
            {
                return result;
            }

            // Mapping
            var mapping = inputs.AssertMapping("inputs");

            foreach (var pair in mapping)
            {
                // Expression key
                if (allowExpressions && pair.Key is ExpressionToken)
                {
                    continue;
                }

                // Literal key
                var key = pair.Key.AssertString("inputs key");

                // Expression value
                if (allowExpressions && pair.Value is ExpressionToken)
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
            Boolean allowExpressions = false)
        {
            if (allowExpressions && token is ExpressionToken)
            {
                return null;
            }

            var numberToken = token.AssertNumber($"step {PipelineTemplateConstants.TimeoutMinutes}");
            return (Int32)numberToken.Value;
        }

        internal static JobContainer ConvertToJobContainer(
            TemplateContext context,
            TemplateToken value,
            bool allowExpressions = false)
        {
            var result = new JobContainer();
            if (allowExpressions && value.Traverse().Any(x => x is ExpressionToken))
            {
                return result;
            }

            if (value is StringToken containerLiteral)
            {
                result.Image = containerLiteral.Value;
            }
            else
            {
                var containerMapping = value.AssertMapping($"{PipelineTemplateConstants.Container}");
                foreach (var containerPropertyPair in containerMapping)
                {
                    var propertyName = containerPropertyPair.Key.AssertString($"{PipelineTemplateConstants.Container} key");

                    switch (propertyName.Value)
                    {
                        case PipelineTemplateConstants.Image:
                            result.Image = containerPropertyPair.Value.AssertString($"{PipelineTemplateConstants.Container} {propertyName}").Value;
                            break;
                        case PipelineTemplateConstants.Env:
                            var env = containerPropertyPair.Value.AssertMapping($"{PipelineTemplateConstants.Container} {propertyName}");
                            var envDict = new Dictionary<String, String>(env.Count);
                            foreach (var envPair in env)
                            {
                                var envKey = envPair.Key.ToString();
                                var envValue = envPair.Value.AssertString($"{PipelineTemplateConstants.Container} {propertyName} {envPair.Key.ToString()}").Value;
                                envDict.Add(envKey, envValue);
                            }
                            result.Environment = envDict;
                            break;
                        case PipelineTemplateConstants.Options:
                            result.Options = containerPropertyPair.Value.AssertString($"{PipelineTemplateConstants.Container} {propertyName}").Value;
                            break;
                        case PipelineTemplateConstants.Ports:
                            var ports = containerPropertyPair.Value.AssertSequence($"{PipelineTemplateConstants.Container} {propertyName}");
                            var portList = new List<String>(ports.Count);
                            foreach (var portItem in ports)
                            {
                                var portString = portItem.AssertString($"{PipelineTemplateConstants.Container} {propertyName} {portItem.ToString()}").Value;
                                portList.Add(portString);
                            }
                            result.Ports = portList;
                            break;
                        case PipelineTemplateConstants.Volumes:
                            var volumes = containerPropertyPair.Value.AssertSequence($"{PipelineTemplateConstants.Container} {propertyName}");
                            var volumeList = new List<String>(volumes.Count);
                            foreach (var volumeItem in volumes)
                            {
                                var volumeString = volumeItem.AssertString($"{PipelineTemplateConstants.Container} {propertyName} {volumeItem.ToString()}").Value;
                                volumeList.Add(volumeString);
                            }
                            result.Volumes = volumeList;
                            break;
                        default:
                            propertyName.AssertUnexpectedValue($"{PipelineTemplateConstants.Container} key");
                            break;
                    }
                }
            }

            if (result.Image.StartsWith("docker://", StringComparison.Ordinal))
            {
                result.Image = result.Image.Substring("docker://".Length);
            }

            if (String.IsNullOrEmpty(result.Image))
            {
                context.Error(value, "Container image cannot be empty");
            }

            return result;
        }

        internal static List<KeyValuePair<String, JobContainer>> ConvertToJobServiceContainers(
            TemplateContext context,
            TemplateToken services,
            bool allowExpressions = false)
        {
            var result = new List<KeyValuePair<String, JobContainer>>();

            if (allowExpressions && services.Traverse().Any(x => x is ExpressionToken))
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

        private static IEnumerable<ContextScope> ConvertToScopes(
            TemplateContext context,
            TemplateToken scopes)
        {
            var scopesSequence = scopes.AssertSequence($"job {PipelineTemplateConstants.Scopes}");

            foreach (var scopesItem in scopesSequence)
            {
                var result = new ContextScope();
                var scope = scopesItem.AssertMapping($"{PipelineTemplateConstants.Scopes} item");

                foreach (var scopeProperty in scope)
                {
                    var propertyName = scopeProperty.Key.AssertString($"{PipelineTemplateConstants.Scopes} item key");

                    switch (propertyName.Value)
                    {
                        case PipelineTemplateConstants.Name:
                            var nameLiteral = scopeProperty.Value.AssertString($"{PipelineTemplateConstants.Scopes} item {PipelineTemplateConstants.Name}");
                            result.Name = nameLiteral.Value;
                            break;

                        case PipelineTemplateConstants.Inputs:
                            result.Inputs = scopeProperty.Value.AssertMapping($"{PipelineTemplateConstants.Scopes} item {PipelineTemplateConstants.Inputs}");
                            break;

                        case PipelineTemplateConstants.Outputs:
                            result.Outputs = scopeProperty.Value.AssertMapping($"{PipelineTemplateConstants.Scopes} item {PipelineTemplateConstants.Outputs}");
                            break;
                    }
                }

                yield return result;
            }
        }

        private static readonly INamedValueInfo[] s_jobIfNamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.GitHub),
        };
        private static readonly INamedValueInfo[] s_stepNamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Strategy),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Matrix),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Steps),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.GitHub),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Job),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Runner),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Env),
        };
        private static readonly INamedValueInfo[] s_stepInTemplateNamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Strategy),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Matrix),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Steps),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Inputs),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.GitHub),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Job),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Runner),
            new NamedValueInfo<NoOperationNamedValue>(PipelineTemplateConstants.Env),
        };
        private static readonly IFunctionInfo[] s_stepConditionFunctions = new IFunctionInfo[]
        {
            new FunctionInfo<NoOperation>(PipelineTemplateConstants.Always, 0, 0),
            new FunctionInfo<NoOperation>(PipelineTemplateConstants.Cancelled, 0, 0),
            new FunctionInfo<NoOperation>(PipelineTemplateConstants.Failure, 0, 0),
            new FunctionInfo<NoOperation>(PipelineTemplateConstants.Success, 0, 0),
        };
    }
}
