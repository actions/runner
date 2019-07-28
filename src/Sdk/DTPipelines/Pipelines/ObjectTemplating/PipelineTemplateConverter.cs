using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
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
        internal static PipelineTemplate ConvertToPipeline(
            TemplateContext context,
            RepositoryResource self,
            TemplateToken pipeline)
        {
            var result = new PipelineTemplate();
            result.Resources.Repositories.Add(self);
            var defaultStage = new Stage
            {
                Name = PipelineConstants.DefaultJobName,
            };
            result.Stages.Add(defaultStage);

            try
            {
                if (pipeline == null || context.Errors.Count > 0)
                {
                    return result;
                }

                var pipelineMapping = pipeline.AssertMapping("root");

                foreach (var pipelinePair in pipelineMapping)
                {
                    var pipelineKey = pipelinePair.Key.AssertString("root key");

                    switch (pipelineKey.Value)
                    {
                        case PipelineTemplateConstants.On:
                            break;

                        case PipelineTemplateConstants.Name:
                            break;

                        // todo: remove support for "workflow" in master during M154
                        case PipelineTemplateConstants.Workflow:
                            defaultStage.Phases.AddRange(ConvertToJobFactories(context, result.Resources, pipelinePair.Value));
                            break;

                        case PipelineTemplateConstants.Jobs:
                            defaultStage.Phases.AddRange(ConvertToJobFactories(context, result.Resources, pipelinePair.Value));
                            break;

                        default:
                            pipelineKey.AssertUnexpectedValue("root key"); // throws
                            break;
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
                        result.Errors.Add(new PipelineValidationError(error.Code, error.Message));
                    }
                }
            }

            return result;
        }

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

        internal static PhaseTarget ConvertToJobTarget(
            TemplateContext context,
            TemplateToken runsOn,
            Boolean allowExpressions = false)
        {
            var result = new AgentQueueTarget();

            // Expression
            if (allowExpressions && runsOn is ExpressionToken)
            {
                return result;
            }

            // String
            if (runsOn is StringToken runsOnString)
            {
                result.Queue = new AgentQueueReference { Name = "GitHub Actions" };
                result.AgentSpecification = new JObject
                {
                    { PipelineTemplateConstants.VmImage, runsOnString.Value }
                };
            }
            // Mapping
            else
            {
                var runsOnMapping = runsOn.AssertMapping($"job {PipelineTemplateConstants.RunsOn}");
                foreach (var runsOnProperty in runsOnMapping)
                {
                    // Expression
                    if (allowExpressions && runsOnProperty.Key is ExpressionToken)
                    {
                        continue;
                    }

                    // String
                    var propertyName = runsOnProperty.Key.AssertString($"job {PipelineTemplateConstants.RunsOn} key");

                    switch (propertyName.Value)
                    {
                        case PipelineTemplateConstants.Pool:
                            // Expression
                            if (allowExpressions && runsOnProperty.Value is ExpressionToken)
                            {
                                continue;
                            }

                            // Literal
                            var pool = runsOnProperty.Value.AssertString($"job {PipelineTemplateConstants.RunsOn} key");
                            result.Queue = new AgentQueueReference { Name = pool.Value };
                            break;

                        default:
                            propertyName.AssertUnexpectedValue($"job {PipelineTemplateConstants.RunsOn} key"); // throws
                            break;
                    }
                }
            }

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

        internal static StrategyResult ConvertToStrategy(
            TemplateContext context,
            TemplateToken token,
            String jobFactoryDisplayName,
            Boolean allowExpressions = false)
        {
            var result = new StrategyResult();

            // Expression
            if (allowExpressions && token is ExpressionToken)
            {
                return result;
            }

            var strategyMapping = token.AssertMapping(PipelineTemplateConstants.Strategy);
            var matrixBuilder = default(MatrixBuilder);
            var hasExpressions = false;

            foreach (var strategyPair in strategyMapping)
            {
                // Expression key
                if (allowExpressions && strategyPair.Key is ExpressionToken)
                {
                    hasExpressions = true;
                    continue;
                }

                // Literal key
                var strategyKey = strategyPair.Key.AssertString("strategy key");

                switch (strategyKey.Value)
                {
                    // Fail-Fast
                    case PipelineTemplateConstants.FailFast:
                        if (allowExpressions && strategyPair.Value is ExpressionToken)
                        {
                            hasExpressions = true;
                            continue;
                        }

                        var failFastBooleanToken = strategyPair.Value.AssertBoolean($"strategy {PipelineTemplateConstants.FailFast}");
                        result.FailFast = failFastBooleanToken.Value;
                        break;

                    // Max-Parallel
                    case PipelineTemplateConstants.MaxParallel:
                        if (allowExpressions && strategyPair.Value is ExpressionToken)
                        {
                            hasExpressions = true;
                            continue;
                        }

                        var maxParallelNumberToken = strategyPair.Value.AssertNumber($"strategy {PipelineTemplateConstants.MaxParallel}");
                        result.MaxParallel = (Int32)maxParallelNumberToken.Value;
                        break;

                    // Matrix
                    case PipelineTemplateConstants.Matrix:

                        // Expression
                        if (allowExpressions && strategyPair.Value is ExpressionToken)
                        {
                            hasExpressions = true;
                            continue;
                        }

                        var matrix = strategyPair.Value.AssertMapping("matrix");
                        hasExpressions = hasExpressions || TemplateUtil.GetTokens(matrix).Any(x => x is ExpressionToken);
                        matrixBuilder = new MatrixBuilder(context, jobFactoryDisplayName);
                        var hasVector = false;

                        foreach (var matrixPair in matrix)
                        {
                            // Expression key
                            if (allowExpressions && matrixPair.Key is ExpressionToken)
                            {
                                hasVector = true; // For validation, treat as if a vector is defined
                                continue;
                            }

                            var matrixKey = matrixPair.Key.AssertString("matrix key");
                            switch (matrixKey.Value)
                            {
                                case PipelineTemplateConstants.Include:
                                    if (allowExpressions && matrixPair.Value is ExpressionToken)
                                    {
                                        continue;
                                    }

                                    var includeSequence = matrixPair.Value.AssertSequence("matrix includes");
                                    matrixBuilder.Include(includeSequence);
                                    break;

                                case PipelineTemplateConstants.Exclude:
                                    if (allowExpressions && matrixPair.Value is ExpressionToken)
                                    {
                                        continue;
                                    }

                                    var excludeSequence = matrixPair.Value.AssertSequence("matrix excludes");
                                    matrixBuilder.Exclude(excludeSequence);
                                    break;

                                default:
                                    hasVector = true;

                                    if (allowExpressions && matrixPair.Value is ExpressionToken)
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

                        if (!hasVector)
                        {
                            context.Error(matrix, $"Matrix must defined at least one vector");
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

                var strategy = new DictionaryContextData()
                {
                    {
                        "fail-fast",
                        new StringContextData(result.FailFast.ToString(CultureInfo.InvariantCulture).ToLowerInvariant())
                    },
                    {
                        "job-index",
                        new StringContextData(i.ToString(CultureInfo.InvariantCulture))
                    },
                    {
                        "job-total",
                        new StringContextData(result.Configurations.Count.ToString(CultureInfo.InvariantCulture))
                    }
                };

                if (result.MaxParallel > 0)
                {
                    strategy.Add(
                        "max-parallel",
                        new StringContextData(result.MaxParallel.ToString(CultureInfo.InvariantCulture))
                    );
                }
                else
                {
                    strategy.Add(
                        "max-parallel",
                        new StringContextData(result.Configurations.Count.ToString(CultureInfo.InvariantCulture))
                    );
                }

                configuration.ContextData.Add(PipelineTemplateConstants.Strategy, strategy);
                context.Memory.AddBytes(PipelineTemplateConstants.Strategy);
                context.Memory.AddBytes(strategy, traverse: true);

                if (!configuration.ContextData.ContainsKey(PipelineTemplateConstants.Matrix))
                {
                    configuration.ContextData.Add(PipelineTemplateConstants.Matrix, null);
                    context.Memory.AddBytes(PipelineTemplateConstants.Matrix);
                }
            }

            return result;
        }

        internal static ContainerResource ConvertToJobContainer(
            TemplateContext context,
            TemplateToken value,
            bool allowExpressions = false)
        {
            var result = new ContainerResource()
            {
                Alias = Guid.NewGuid().ToString("N")
            };
            if (allowExpressions && TemplateUtil.GetTokens(value).Any(x => x is ExpressionToken))
            {
                return result;
            }

            if (value is StringToken containerLiteral)
            {
                result.Image = containerLiteral.Value;
            }
            else if (value is MappingToken containerMapping)
            {
                foreach (var containerPropertyPair in containerMapping)
                {
                    var propertyName = containerPropertyPair.Key.AssertString($"{PipelineTemplateConstants.Container} key");

                    switch (propertyName.Value)
                    {
                        case ContainerPropertyNames.Image:
                            result.Image = containerPropertyPair.Value.AssertString($"{PipelineTemplateConstants.Container} {propertyName}").Value;
                            break;
                        case ContainerPropertyNames.Env:
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
                        case ContainerPropertyNames.Options:
                            result.Options = containerPropertyPair.Value.AssertString($"{PipelineTemplateConstants.Container} {propertyName}").Value;
                            break;
                        case ContainerPropertyNames.Ports:
                            var ports = containerPropertyPair.Value.AssertSequence($"{PipelineTemplateConstants.Container} {propertyName}");
                            var portList = new List<String>(ports.Count);
                            foreach (var portItem in ports)
                            {
                                var portString = portItem.AssertString($"{PipelineTemplateConstants.Container} {propertyName} {portItem.ToString()}").Value;
                                portList.Add(portString);
                            }
                            result.Ports = portList;
                            break;
                        case ContainerPropertyNames.Volumes:
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
            else if (value is ExpressionToken containerExpression)
            {
                result.Image = containerExpression.ToString();
            }

            if (result.Image.StartsWith("docker://", StringComparison.Ordinal))
            {
                result.Image = result.Image.Substring("docker://".Length);
            }

            return result;
        }

        internal static Dictionary<String, ContainerResource> ConvertToJobServiceContainers(
            TemplateContext context,
            TemplateToken services,
            bool allowExpressions = false)
        {
            var result = new Dictionary<String, ContainerResource>(StringComparer.OrdinalIgnoreCase);

            if (allowExpressions && TemplateUtil.GetTokens(services).Any(x => x is ExpressionToken))
            {
                return result;
            }

            var servicesMapping = services.AssertMapping("services");

            foreach (var servicePair in servicesMapping)
            {
                var k = servicePair.Key;
                var v = servicePair.Value;
                var container = ConvertToJobContainer(context, v, allowExpressions);

                result.Add(k.ToString(), container);
            }

            return result;
        }

        private static IEnumerable<PhaseNode> ConvertToJobFactories(
            TemplateContext context,
            PipelineResources resources,
            TemplateToken workflow)
        {
            var jobsMapping = workflow.AssertMapping(PipelineTemplateConstants.Jobs);

            foreach (var jobsPair in jobsMapping)
            {
                var jobNameToken = jobsPair.Key.AssertString($"{PipelineTemplateConstants.Jobs} key");
                if (!NameValidation.IsValid(jobNameToken.Value, true))
                {
                    context.Error(jobNameToken, $"Job name {jobNameToken.Value} is invalid. Names must start with a letter or '_' and contain only alphanumeric characters, '-', or '_'");
                }
                var result = new JobFactory
                {
                    Name = jobNameToken.Value
                };

                var jobFactoryDefinition = jobsPair.Value.AssertMapping($"{PipelineTemplateConstants.Jobs} value");

                foreach (var jobFactoryProperty in jobFactoryDefinition)
                {
                    var propertyName = jobFactoryProperty.Key.AssertString($"job property name");

                    switch (propertyName.Value)
                    {
                        case "actions": // todo: remove before July 31
                            result.Steps.AddRange(ConvertToSteps(context, jobFactoryProperty.Value));
                            break;

                        case PipelineTemplateConstants.ContinueOnError:
                            var continueOnErrorBooleanToken = jobFactoryProperty.Value.AssertBoolean($"job {PipelineTemplateConstants.ContinueOnError}");
                            result.ContinueOnError = continueOnErrorBooleanToken.Value;
                            break;

                        case PipelineTemplateConstants.If:
                            var ifCondition = jobFactoryProperty.Value.AssertString($"job {PipelineTemplateConstants.If}");
                            result.Condition = ConvertToIfCondition(context, ifCondition, true, true);
                            break;

                        case PipelineTemplateConstants.Name:
                            var displayName = jobFactoryProperty.Value.AssertScalar($"job {PipelineTemplateConstants.Name}");
                            ConvertToJobDisplayName(context, displayName, allowExpressions: true); // Validate early if possible
                            if (displayName is StringToken)
                            {
                                result.DisplayName = displayName.ToString();
                            }
                            else
                            {
                                result.JobDisplayName = displayName.Clone(true) as ExpressionToken;
                            }
                            break;

                        case PipelineTemplateConstants.Needs:
                            if (jobFactoryProperty.Value is StringToken needsLiteral)
                            {
                                result.DependsOn.Add(needsLiteral.Value);
                            }
                            else
                            {
                                var needs = jobFactoryProperty.Value.AssertSequence($"job {PipelineTemplateConstants.Needs}");
                                foreach (var needsItem in needs)
                                {
                                    var need = needsItem.AssertString($"job {PipelineTemplateConstants.Needs} item");
                                    result.DependsOn.Add(need.Value);
                                }
                            }
                            break;

                        case PipelineTemplateConstants.RunsOn:
                            ConvertToJobTarget(context, jobFactoryProperty.Value, allowExpressions: true); // Validate early if possible
                            result.JobTarget = jobFactoryProperty.Value.Clone(true);
                            break;

                        case PipelineTemplateConstants.Scopes:
                            foreach (var scope in ConvertToScopes(context, jobFactoryProperty.Value))
                            {
                                result.Scopes.Add(scope);
                            }
                            break;

                        case PipelineTemplateConstants.Steps:
                            result.Steps.AddRange(ConvertToSteps(context, jobFactoryProperty.Value));
                            break;

                        case PipelineTemplateConstants.Strategy:
                            ConvertToStrategy(context, jobFactoryProperty.Value, null, allowExpressions: true); // Validate early if possible
                            result.Strategy = jobFactoryProperty.Value.Clone(true);
                            break;

                        case PipelineTemplateConstants.TimeoutMinutes:
                            ConvertToJobTimeout(context, jobFactoryProperty.Value, allowExpressions: true); // Validate early if possible
                            result.JobTimeout = jobFactoryProperty.Value.Clone(true) as ScalarToken;
                            break;

                        case PipelineTemplateConstants.CancelTimeoutMinutes:
                            ConvertToJobCancelTimeout(context, jobFactoryProperty.Value, allowExpressions: true); // Validate early if possible
                            result.JobCancelTimeout = jobFactoryProperty.Value.Clone(true) as ScalarToken;
                            break;

                        case PipelineTemplateConstants.Container:
                            ConvertToJobContainer(context, jobFactoryProperty.Value, allowExpressions: true);
                            result.JobContainer = jobFactoryProperty.Value.Clone(true);
                            break;

                        case PipelineTemplateConstants.Services:
                            ConvertToJobServiceContainers(context, jobFactoryProperty.Value, allowExpressions: true);
                            result.JobServiceContainers = jobFactoryProperty.Value.Clone(true);
                            break;

                        default:
                            propertyName.AssertUnexpectedValue("job key"); // throws
                            break;
                    }
                }

                // todo: Move "required" support into schema validation
                if (result.JobTarget == null)
                {
                    context.Error(jobFactoryDefinition, $"The '{PipelineTemplateConstants.RunsOn}' property is required");
                }

                if (String.IsNullOrEmpty(result.DisplayName))
                {
                    result.DisplayName = result.Name;
                }

                if (result.Scopes.Count > 0)
                {
                    result.Steps.Insert(
                        0,
                        new ActionStep
                        {
                            Reference = new ScriptReference(),
                            DisplayName = "WARNING: TEMPLATES ARE HIGHLY EXPERIMENTAL",
                            Inputs = new MappingToken(null, null, null)
                            {
                                {
                                    new StringToken(null, null, null, PipelineConstants.ScriptStepInputs.Script),
                                    new StringToken(null, null, null, "echo WARNING: TEMPLATES ARE HIGHLY EXPERIMENTAL")
                                }
                            }
                        });
                    result.Steps.Add(
                        new ActionStep
                        {
                            Reference = new ScriptReference(),
                            DisplayName = "WARNING: TEMPLATES ARE HIGHLY EXPERIMENTAL",
                            Inputs = new MappingToken(null, null, null)
                            {
                                {
                                    new StringToken(null, null, null, PipelineConstants.ScriptStepInputs.Script),
                                    new StringToken(null, null, null, "echo WARNING: TEMPLATES ARE HIGHLY EXPERIMENTAL")
                                }
                            }
                        });
                }

                yield return result;
            }
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

        private static List<Step> ConvertToSteps(
            TemplateContext context,
            TemplateToken steps)
        {
            var stepsSequence = steps.AssertSequence($"job {PipelineTemplateConstants.Steps}");

            var result = new List<Step>();
            foreach (var stepsItem in stepsSequence)
            {
                var step = ConvertToStep(context, stepsItem);
                if (step != null) // step = null means we are hitting error during step conversion, there should be an error in context.errors
                {
                    if (step.Enabled)
                    {
                        result.Add(step);
                    }
                }
            }

            return result;
        }

        private static ActionStep ConvertToStep(
            TemplateContext context,
            TemplateToken stepsItem)
        {
            var step = stepsItem.AssertMapping($"{PipelineTemplateConstants.Steps} item");
            var continueOnError = default(ScalarToken);
            var env = default(TemplateToken);
            var id = default(StringToken);
            var ifCondition = default(String);
            var ifToken = default(StringToken);
            var name = default(ScalarToken);
            var run = default(ScalarToken);
            var scope = default(StringToken);
            var timeoutMinutes = default(ScalarToken);
            var uses = default(StringToken);
            var with = default(TemplateToken);
            var workingDir = default(ScalarToken);
            var path = default(ScalarToken);
            var clean = default(ScalarToken);
            var fetchDepth = default(ScalarToken);
            var lfs = default(ScalarToken);
            var submodules = default(ScalarToken);
            var shell = default(ScalarToken);

            foreach (var stepProperty in step)
            {
                var propertyName = stepProperty.Key.AssertString($"{PipelineTemplateConstants.Steps} item key");

                switch (propertyName.Value)
                {
                    case PipelineTemplateConstants.Clean:
                        clean = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Clean}");
                        break;

                    case PipelineTemplateConstants.ContinueOnError:
                        ConvertToStepContinueOnError(context, stepProperty.Value, allowExpressions: true); // Validate early if possible
                        continueOnError = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} {PipelineTemplateConstants.ContinueOnError}");
                        break;

                    case PipelineTemplateConstants.Env:
                        ConvertToStepEnvironment(context, stepProperty.Value, StringComparer.Ordinal, allowExpressions: true); // Validate early if possible
                        env = stepProperty.Value;
                        break;

                    case PipelineTemplateConstants.FetchDepth:
                        fetchDepth = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.FetchDepth}");
                        break;

                    case PipelineTemplateConstants.Id:
                        id = stepProperty.Value.AssertString($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Id}");
                        if (!NameValidation.IsValid(id.Value, true))
                        {
                            context.Error(id, $"Step id {id.Value} is invalid. Ids must start with a letter or '_' and contain only alphanumeric characters, '-', or '_'");
                        }
                        break;

                    case PipelineTemplateConstants.If:
                        ifToken = stepProperty.Value.AssertString($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.If}");
                        break;

                    case PipelineTemplateConstants.Lfs:
                        lfs = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Lfs}");
                        break;

                    case PipelineTemplateConstants.Name:
                        name = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Name}");
                        break;

                    case PipelineTemplateConstants.Path:
                        path = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Path}");
                        break;

                    case PipelineTemplateConstants.Run:
                        run = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Run}");
                        break;

                    case PipelineTemplateConstants.Shell:
                        shell = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Shell}");
                        break;

                    case PipelineTemplateConstants.Scope:
                        scope = stepProperty.Value.AssertString($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Scope}");
                        break;

                    case PipelineTemplateConstants.Submodules:
                        submodules = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Submodules}");
                        break;

                    case PipelineTemplateConstants.TimeoutMinutes:
                        ConvertToStepTimeout(context, stepProperty.Value, allowExpressions: true); // Validate early if possible
                        timeoutMinutes = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.TimeoutMinutes}");
                        break;

                    case PipelineTemplateConstants.Uses:
                        uses = stepProperty.Value.AssertString($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Uses}");
                        break;

                    case PipelineTemplateConstants.With:
                        ConvertToStepInputs(context, stepProperty.Value, allowExpressions: true); // Validate early if possible
                        with = stepProperty.Value;
                        break;

                    case PipelineTemplateConstants.WorkingDirectory:
                        workingDir = stepProperty.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.WorkingDirectory}");
                        break;

                    default:
                        propertyName.AssertUnexpectedValue($"{PipelineTemplateConstants.Steps} item key"); // throws
                        break;
                }
            }

            // Fixup the if-condition
            var isDefaultScope = String.IsNullOrEmpty(scope?.Value);
            ifCondition = ConvertToIfCondition(context, ifToken, false, isDefaultScope);

            if (run != null)
            {
                var result = new ActionStep
                {
                    ScopeName = scope?.Value,
                    ContextName = id?.Value,
                    ContinueOnError = continueOnError?.Clone(true) as ScalarToken,
                    DisplayName = name?.ToString(),
                    Condition = ifCondition,
                    TimeoutInMinutes = timeoutMinutes?.Clone(true) as ScalarToken,
                    Environment = env?.Clone(true),
                    Reference = new ScriptReference()
                };

                if (String.IsNullOrEmpty(result.DisplayName))
                {
                    var firstLine = run.ToString().TrimStart(' ', '\t', '\r', '\n');
                    var firstNewLine = firstLine.IndexOfAny(new[] { '\r', '\n' });
                    if (firstNewLine >= 0)
                    {
                        firstLine = firstLine.Substring(0, firstNewLine);
                    }
                    result.DisplayName = $"Run: {firstLine}";
                }

                var inputs = new MappingToken(null, null, null);
                inputs.Add(new StringToken(null, null, null, PipelineConstants.ScriptStepInputs.Script), run.Clone(true));

                if (workingDir != null)
                {
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.ScriptStepInputs.WorkingDirectory), workingDir.Clone(true));
                }

                if (shell != null)
                {
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.ScriptStepInputs.Shell), shell.Clone(true));
                }

                result.Inputs = inputs;

                return result;
            }
            else if (uses != null)
            {
                var result = new ActionStep
                {
                    ScopeName = scope?.Value,
                    ContextName = id?.Value,
                    ContinueOnError = continueOnError?.Clone(true) as ScalarToken,
                    DisplayName = name?.ToString(),
                    Condition = ifCondition,
                    TimeoutInMinutes = timeoutMinutes?.Clone(true) as ScalarToken,
                    Inputs = with,
                    Environment = env,
                };

                if (String.IsNullOrEmpty(result.DisplayName))
                {
                    // todo: loc
                    result.DisplayName = $"Action: {uses.Value}";
                }

                if (uses.Value.StartsWith("docker://", StringComparison.Ordinal))
                {
                    var image = uses.Value.Substring("docker://".Length);
                    result.Reference = new ContainerRegistryReference { Image = image };
                }
                else if (uses.Value.StartsWith("./") || uses.Value.StartsWith(".\\"))
                {
                    result.Reference = new RepositoryPathReference
                    {
                        RepositoryType = PipelineConstants.SelfAlias,
                        Path = uses.Value
                    };
                }
                else
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
                        // todo: loc
                        context.Error(uses, $"Expected format {{org}}/{{repo}}[/path]@ref. Actual '{uses.Value}'");
                    }
                    else
                    {
                        var repositoryName = $"{pathSegments[0]}/{pathSegments[1]}";
                        var directoryPath = pathSegments.Length > 2 ? String.Join("/", pathSegments.Skip(2)) : String.Empty;

                        result.Reference = new RepositoryPathReference
                        {
                            RepositoryType = RepositoryTypes.GitHub,
                            Name = repositoryName,
                            Ref = gitRef,
                            Path = directoryPath,
                        };
                    }
                }

                return result;
            }
            else
            {
                // todo: build a "required" concept into the parser
                context.Error(step, $"Either '{PipelineTemplateConstants.Uses}' or '{PipelineTemplateConstants.Run}' is required");
                return null;
            }
        }

        private static String ConvertToIfCondition(
            TemplateContext context,
            StringToken ifCondition,
            Boolean isJob,
            Boolean isDefaultScope)
        {
            if (String.IsNullOrWhiteSpace(ifCondition?.Value))
            {
                return "success()";
            }

            var condition = ifCondition.Value;

            var expressionParser = new ExpressionParser();
            var namedValues = default(INamedValueInfo[]);
            if (!isJob)
            {
                namedValues = isDefaultScope ? s_stepNamedValues : s_stepInTemplateNamedValues;
            }

            var node = default(ExpressionNode);
            try
            {
                node = expressionParser.CreateTree(condition, null, namedValues, s_conditionFunctions) as ExpressionNode;
            }
            catch (Exception ex)
            {
                context.Error(ifCondition, ex);
                return null;
            }

            var hasStatusFunction = node.GetNodes().Any(x =>
            {
                if (x is Function function)
                {
                    return String.Equals(function.Name, "always", StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(function.Name, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(function.Name, "failure", StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(function.Name, "success", StringComparison.OrdinalIgnoreCase);
                }

                return false;
            });

            return hasStatusFunction ? condition : $"and(success(), {condition})";
        }

        /// <summary>
        /// Used for building expression parse trees.
        /// </summary>
        private sealed class NoOpFunction : Function
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
                return null;
            }
        }

        /// <summary>
        /// Used for building expression parse trees.
        /// </summary>
        private sealed class NoOpValue : NamedValue
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
                return null;
            }
        }

        private static readonly INamedValueInfo[] s_stepNamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Strategy),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Matrix),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Steps),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.GitHub),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Job),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Runner),
        };
        private static readonly INamedValueInfo[] s_stepInTemplateNamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Strategy),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Matrix),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Steps),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Inputs),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.GitHub),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Job),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Runner),
        };
        private static readonly IFunctionInfo[] s_conditionFunctions = new IFunctionInfo[]
        {
            new FunctionInfo<NoOpFunction>("always", 0, 0),
            new FunctionInfo<NoOpFunction>("cancelled", 0, 0),
            new FunctionInfo<NoOpFunction>("failure", 0, 0),
            new FunctionInfo<NoOpFunction>("success", 0, 0),
        };
    }
}
