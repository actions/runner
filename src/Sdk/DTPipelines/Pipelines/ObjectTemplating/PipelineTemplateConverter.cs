using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.Services.Common;
using RegexUtility = GitHub.DistributedTask.Pipelines.Expressions.RegexUtility;
using WellKnownRegularExpressions = GitHub.DistributedTask.Pipelines.Expressions.WellKnownRegularExpressions;

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
                var queueName = AgentQueueTarget.PoolNameForVMImage(runsOnString.Value);

                if (String.IsNullOrEmpty(queueName))
                {
                    context.Error(runsOnString, $"Unexpected VM image '{runsOnString.Value}'");
                }
                else
                {
                    result.Queue = new AgentQueueReference { Name = queueName };
                }
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

            var numberToken = token.AssertNumber($"job {PipelineTemplateConstants.Timeout}");
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

            var numberToken = token.AssertNumber($"job {PipelineTemplateConstants.CancelTimeout}");
            return (Int32)numberToken.Value;
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
            var parallel = 0;
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

                    case PipelineTemplateConstants.Parallel:
                        // Expression
                        if (allowExpressions && strategyPair.Value is ExpressionToken)
                        {
                            hasExpressions = true;
                            continue;
                        }

                        // Literal
                        var parallelNumber = strategyPair.Value.AssertNumber("parallel");
                        parallel = (Int32)parallelNumber.Value;
                        if (parallel < 1)
                        {
                            context.Error(parallelNumber, "Must be an integer greater than zero");
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
            else if (parallel > 0)
            {
                var nameBuilder = new ReferenceNameBuilder();
                var displayNameBuilder = new JobDisplayNameBuilder(jobFactoryDisplayName);

                for (var parallelIndex = 0; parallelIndex < parallel; parallelIndex++)
                {
                    // New configuration
                    var configuration = new StrategyConfiguration();
                    context.Memory.AddBytes(TemplateMemory.MinObjectSize);

                    // Name
                    nameBuilder.AppendSegment("parallel");
                    nameBuilder.AppendSegment((parallelIndex + 1).ToString(CultureInfo.InvariantCulture));
                    configuration.Name = nameBuilder.Build();
                    context.Memory.AddBytes(configuration.Name);

                    // Display name
                    displayNameBuilder.AppendSegment((parallelIndex + 1).ToString(CultureInfo.InvariantCulture));
                    configuration.DisplayName = displayNameBuilder.Build();
                    context.Memory.AddBytes(configuration.DisplayName);

                    // Parallel context
                    var parallelContext = new StringContextData(parallel.ToString(CultureInfo.InvariantCulture));
                    configuration.ContextData.Add(PipelineTemplateConstants.Parallel, parallelContext);
                    context.Memory.AddBytes(PipelineTemplateConstants.Parallel);
                    context.Memory.AddBytes(parallel, traverse: true);

                    result.Configurations.Add(configuration);
                }
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

                if (!configuration.ContextData.ContainsKey(PipelineTemplateConstants.Parallel))
                {
                    configuration.ContextData.Add(PipelineTemplateConstants.Parallel, null);
                    context.Memory.AddBytes(PipelineTemplateConstants.Parallel);
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
                        case PipelineTemplateConstants.Actions:
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

                        case PipelineTemplateConstants.Strategy:
                            ConvertToStrategy(context, jobFactoryProperty.Value, null, allowExpressions: true); // Validate early if possible
                            result.Strategy = jobFactoryProperty.Value.Clone(true);
                            break;

                        case PipelineTemplateConstants.Timeout:
                            ConvertToJobTimeout(context, jobFactoryProperty.Value, allowExpressions: true); // Validate early if possible
                            result.JobTimeout = jobFactoryProperty.Value.Clone(true) as ScalarToken;
                            break;

                        case PipelineTemplateConstants.CancelTimeout:
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
            TemplateToken actions)
        {
            var actionsSequence = actions.AssertSequence($"job {PipelineTemplateConstants.Actions}");

            bool requireInsertCheckout = true;
            var result = new List<Step>();
            foreach (var actionsItem in actionsSequence)
            {
                var action = ConvertToStep(context, actionsItem);
                if (action != null) // action = null means we are hitting error during step conversion, there should be an error in context.errors
                {
                    if (requireInsertCheckout &&
                        action.Reference is PluginReference agentPlugin &&
                        agentPlugin.Plugin == PipelineConstants.AgentPlugins.Checkout)
                    {
                        requireInsertCheckout = false;
                    }

                    if (action.Enabled)
                    {
                        result.Add(action);
                    }
                }
            }

            if (requireInsertCheckout)
            {
                var checkoutAction = new ActionStep
                {
                    DisplayName = "Checkout",
                    Enabled = true,
                    Reference = new PluginReference
                    {
                        Plugin = PipelineConstants.AgentPlugins.Checkout
                    }
                };

                var inputs = new MappingToken(null, null, null);
                inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Repository), new BasicExpressionToken(null, null, null, "github.repository"));
                inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Ref), new BasicExpressionToken(null, null, null, "github.ref"));
                inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Version), new BasicExpressionToken(null, null, null, "github.sha"));
                inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Token), new BasicExpressionToken(null, null, null, "github.token"));
                inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.WorkspaceRepo), new StringToken(null, null, null, bool.TrueString));
                checkoutAction.Inputs = inputs;

                result.Insert(0, checkoutAction);
            }

            return result;
        }

        private static ActionStep ConvertToStep(
            TemplateContext context,
            TemplateToken actionsItem)
        {
            var action = actionsItem.AssertMapping($"{PipelineTemplateConstants.Actions} item");
            var checkout = default(StringToken);
            var env = default(TemplateToken);
            var id = default(StringToken);
            var ifCondition = default(String);
            var ifToken = default(StringToken);
            var name = default(ScalarToken);
            var run = default(ScalarToken);
            var scope = default(StringToken);
            var timeout = default(NumberToken);
            var uses = default(StringToken);
            var with = default(TemplateToken);
            var workingDir = default(ScalarToken);
            var path = default(ScalarToken);
            var clean = default(ScalarToken);
            var fetchDepth = default(ScalarToken);
            var lfs = default(ScalarToken);
            var submodules = default(ScalarToken);
            var token = default(ScalarToken);

            foreach (var actionProperty in action)
            {
                var propertyName = actionProperty.Key.AssertString($"{PipelineTemplateConstants.Actions} item key");

                switch (propertyName.Value)
                {
                    case PipelineTemplateConstants.Checkout:
                        checkout = actionProperty.Value.AssertString($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Checkout}");
                        break;

                    case PipelineTemplateConstants.Clean:
                        clean = actionProperty.Value.AssertScalar($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Clean}");
                        break;

                    case PipelineTemplateConstants.Env:
                        ConvertToStepEnvironment(context, actionProperty.Value, StringComparer.Ordinal, allowExpressions: true); // Validate early if possible
                        env = actionProperty.Value;
                        break;

                    case PipelineTemplateConstants.FetchDepth:
                        fetchDepth = actionProperty.Value.AssertScalar($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.FetchDepth}");
                        break;

                    case PipelineTemplateConstants.Id:
                        id = actionProperty.Value.AssertString($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Id}");
                        if (!NameValidation.IsValid(id.Value, true))
                        {
                            context.Error(id, $"Action id {id.Value} is invalid. Ids must start with a letter or '_' and contain only alphanumeric characters, '-', or '_'");
                        }
                        break;

                    case PipelineTemplateConstants.If:
                        ifToken = actionProperty.Value.AssertString($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.If}");
                        break;

                    case PipelineTemplateConstants.Lfs:
                        lfs = actionProperty.Value.AssertScalar($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Lfs}");
                        break;

                    case PipelineTemplateConstants.Name:
                        name = actionProperty.Value.AssertScalar($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Name}");
                        break;

                    case PipelineTemplateConstants.Path:
                        path = actionProperty.Value.AssertScalar($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Path}");
                        break;

                    case PipelineTemplateConstants.Run:
                        run = actionProperty.Value.AssertScalar($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Run}");
                        break;

                    case PipelineTemplateConstants.Scope:
                        scope = actionProperty.Value.AssertString($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Scope}");
                        break;

                    case PipelineTemplateConstants.Submodules:
                        submodules = actionProperty.Value.AssertScalar($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Submodules}");
                        break;

                    case PipelineTemplateConstants.Timeout:
                        timeout = actionProperty.Value.AssertNumber($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Timeout}");
                        break;

                    case PipelineTemplateConstants.Uses:
                        uses = actionProperty.Value.AssertString($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Uses}");
                        break;

                    case PipelineTemplateConstants.With:
                        ConvertToStepInputs(context, actionProperty.Value, allowExpressions: true); // Validate early if possible
                        with = actionProperty.Value;
                        break;

                    case PipelineTemplateConstants.WorkingDirectory:
                        workingDir = actionProperty.Value.AssertScalar($"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.WorkingDirectory}");
                        break;

                    default:
                        propertyName.AssertUnexpectedValue($"{PipelineTemplateConstants.Actions} item key"); // throws
                        break;
                }
            }

            if (ifToken != null)
            {
                var isDefaultScope = String.IsNullOrEmpty(scope?.Value);
                ifCondition = ConvertToIfCondition(context, ifToken, false, isDefaultScope);
            }

            if (run != null)
            {
                var result = new ActionStep
                {
                    ScopeName = scope?.Value,
                    ContextName = id?.Value,
                    DisplayName = name?.ToString(),
                    Condition = ifCondition,
                    TimeoutInMinutes = (Int32)(timeout?.Value ?? 0d),
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

                result.Inputs = inputs;

                return result;
            }
            else if (checkout != null)
            {
                var result = new ActionStep
                {
                    ScopeName = scope?.Value,
                    ContextName = id?.Value,
                    DisplayName = name?.ToString(),
                    Condition = ifCondition,
                    TimeoutInMinutes = (Int32)(timeout?.Value ?? 0d),
                    Environment = env?.Clone(true),
                    Reference = new PluginReference()
                    {
                        Plugin = PipelineConstants.AgentPlugins.Checkout
                    }
                };

                if (String.IsNullOrEmpty(result.DisplayName))
                {
                    result.DisplayName = "Checkout";
                }

                var inputs = new MappingToken(null, null, null);
                if (string.Equals(checkout.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Repository), new BasicExpressionToken(null, null, null, "github.repository"));
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Ref), new BasicExpressionToken(null, null, null, "github.ref"));
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Version), new BasicExpressionToken(null, null, null, "github.sha"));
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.WorkspaceRepo), new StringToken(null, null, null, bool.TrueString));
                }
                else if (string.Equals(checkout.Value, bool.FalseString, StringComparison.OrdinalIgnoreCase))
                {
                    // `- checkout: false` means not checkout, we will set the enable to false and let it get skipped.
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Repository), new BasicExpressionToken(null, null, null, "github.repository"));
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Ref), new BasicExpressionToken(null, null, null, "github.ref"));
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Version), new BasicExpressionToken(null, null, null, "github.sha"));
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.WorkspaceRepo), new StringToken(null, null, null, bool.TrueString));
                    result.Enabled = false;
                }
                else
                {
                    var checkoutSegments = checkout.Value.Split('@');
                    var pathSegments = checkoutSegments[0].Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    var gitRef = checkoutSegments.Length == 2 ? checkoutSegments[1] : String.Empty;

                    if (checkoutSegments.Length != 2 ||
                        pathSegments.Length != 2 ||
                        String.IsNullOrEmpty(pathSegments[0]) ||
                        String.IsNullOrEmpty(pathSegments[1]) ||
                        String.IsNullOrEmpty(gitRef))
                    {
                        // todo: loc
                        context.Error(uses, $"Expected format {{org}}/{{repo}}@ref. Actual '{uses.Value}'");
                    }
                    else
                    {
                        var repositoryName = $"{pathSegments[0]}/{pathSegments[1]}";
                        inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Repository), new StringToken(null, null, null, repositoryName));

                        if (RegexUtility.IsMatch(gitRef, WellKnownRegularExpressions.SHA1))
                        {
                            inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Version), new StringToken(null, null, null, gitRef));
                        }
                        else
                        {
                            inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Ref), new StringToken(null, null, null, gitRef));
                        }
                    }
                }

                if (path != null)
                {
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Path), path.Clone(true));
                }

                if (clean != null)
                {
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Clean), clean.Clone(true));
                }

                if (fetchDepth != null)
                {
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.FetchDepth), fetchDepth.Clone(true));
                }

                if (lfs != null)
                {
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Lfs), lfs.Clone(true));
                }

                if (submodules != null)
                {
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Submodules), submodules.Clone(true));
                }

                if (token != null)
                {
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Token), token.Clone(true));
                }
                else
                {
                    inputs.Add(new StringToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Token), new BasicExpressionToken(null, null, null, "github.token"));
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
                    DisplayName = name?.ToString(),
                    Condition = ifCondition,
                    TimeoutInMinutes = (Int32)(timeout?.Value ?? 0d),
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
                context.Error(action, $"Either '{PipelineTemplateConstants.Uses}' or '{PipelineTemplateConstants.Run}' is required");
                return null;
            }
        }

        private static String ConvertToIfCondition(
            TemplateContext context,
            StringToken ifCondition,
            Boolean isJob,
            Boolean isDefaultScope)
        {
            if (String.IsNullOrWhiteSpace(ifCondition.Value))
            {
                return "succeeded()";
            }

            var condition = ifCondition.Value;

            var parserOptions = new ExpressionParserOptions() { AllowHyphens = true };
            var expressionParser = new ExpressionParser(parserOptions);
            var namedValues = default(INamedValueInfo[]);
            if (!isJob)
            {
                namedValues = isDefaultScope ? s_actionNamedValues : s_actionInTemplateNamedValues;
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

            var hasStatusFunction = node.GetParameters<FunctionNode>().Any(x =>
            {
                switch (x.Name ?? String.Empty)
                {
                    case "always":
                    case "canceled":
                    case "failed":
                    case "succeeded":
                    case "succeededOrFailed":
                        return true;
                    default:
                        return false;
                }
            });

            return hasStatusFunction ? condition : $"and(succeeded(), {condition})";
        }

        /// <summary>
        /// Used for building expression parse trees.
        /// </summary>
        private sealed class NoOpFunction : FunctionNode
        {
        }

        /// <summary>
        /// Used for building expression parse trees.
        /// </summary>
        private sealed class NoOpValue : NamedValueNode
        {
        }

        private static readonly INamedValueInfo[] s_actionNamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Strategy),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Matrix),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Parallel),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Actions),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.GitHub),
        };
        private static readonly INamedValueInfo[] s_actionInTemplateNamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Strategy),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Matrix),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Parallel),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Actions),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Inputs),
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.GitHub),
        };
        private static readonly IFunctionInfo[] s_conditionFunctions = new IFunctionInfo[]
        {
            new FunctionInfo<NoOpFunction>("always", 0, 0),
            new FunctionInfo<NoOpFunction>("canceled", 0, 0),
            new FunctionInfo<NoOpFunction>("failed", 0, 0),
            new FunctionInfo<NoOpFunction>("succeeded", 0, 0),
            new FunctionInfo<NoOpFunction>("succeededOrFailed", 0, 0),
        };
    }
}
