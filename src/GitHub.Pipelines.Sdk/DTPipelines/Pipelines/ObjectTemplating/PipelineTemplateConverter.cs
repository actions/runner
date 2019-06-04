using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.Expressions;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.Services.Common;

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

                var pipelineMapping = TemplateUtil.AssertMapping(pipeline, "root");

                foreach (var pipelinePair in pipelineMapping)
                {
                    var pipelineKey = TemplateUtil.AssertLiteral(pipelinePair.Key, "root key");

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
                            TemplateUtil.AssertUnexpectedValue(pipelineKey, "root key"); // throws
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

            // Literal
            var displayNameLiteral = TemplateUtil.AssertLiteral(displayName, $"job {PipelineTemplateConstants.Name}");
            result = displayNameLiteral.Value;
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

            // Literal
            if (runsOn is LiteralToken runsOnLiteral)
            {
                var queueName = AgentQueueTarget.PoolNameForVMImage(runsOnLiteral.Value);

                if (String.IsNullOrEmpty(queueName))
                {
                    context.Error(runsOnLiteral, $"Unexpected VM image '{runsOnLiteral.Value}'");
                }
                else
                {
                    result.Queue = new AgentQueueReference { Name = queueName };
                }
            }
            // Mapping
            else if (runsOn is MappingToken runsOnMapping)
            {
                foreach (var runsOnProperty in runsOnMapping)
                {
                    // Expression
                    if (allowExpressions && runsOnProperty.Key is ExpressionToken)
                    {
                        continue;
                    }

                    // Literal
                    var propertyName = TemplateUtil.AssertLiteral(runsOnProperty.Key, $"job {PipelineTemplateConstants.RunsOn} key");

                    switch (propertyName.Value)
                    {
                        case PipelineTemplateConstants.Pool:
                            // Expression
                            if (allowExpressions && runsOnProperty.Value is ExpressionToken)
                            {
                                continue;
                            }

                            // Literal
                            var pool = TemplateUtil.AssertLiteral(runsOnProperty.Value, $"job {PipelineTemplateConstants.RunsOn} key");
                            result.Queue = new AgentQueueReference { Name = pool.Value };
                            break;

                        default:
                            TemplateUtil.AssertUnexpectedValue(propertyName, $"job {PipelineTemplateConstants.RunsOn} key"); // throws
                            break;
                    }
                }
            }
            // Unexpected
            else
            {
                TemplateUtil.AssertLiteral(runsOn, $"job {PipelineTemplateConstants.RunsOn}");
            }

            return result;
        }

        internal static Int32? ConvertToJobTimeout(
            TemplateContext context,
            TemplateToken token,
            Boolean allowExpressions = false)
        {
            return ConvertToInt32(
                context,
                token,
                $"job {PipelineTemplateConstants.Timeout}",
                "Invalid timeout '{0}'",
                allowExpressions);
        }

        internal static Int32? ConvertToJobCancelTimeout(
            TemplateContext context,
            TemplateToken token,
            Boolean allowExpressions = false)
        {
            return ConvertToInt32(
                context,
                token,
                $"job {PipelineTemplateConstants.CancelTimeout}",
                "Invalid cancel timeout '{0}'",
                allowExpressions);
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
            var mapping = TemplateUtil.AssertMapping(environment, "environment");

            foreach (var pair in mapping)
            {
                // Expression key
                if (allowExpressions && pair.Key is ExpressionToken)
                {
                    continue;
                }

                // Literal key
                var key = TemplateUtil.AssertLiteral(pair.Key, "environment key");

                // Expression value
                if (allowExpressions && pair.Value is ExpressionToken)
                {
                    continue;
                }

                // Literal value
                var value = TemplateUtil.AssertLiteral(pair.Value, "environment value");
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
            var mapping = TemplateUtil.AssertMapping(inputs, "inputs");

            foreach (var pair in mapping)
            {
                // Expression key
                if (allowExpressions && pair.Key is ExpressionToken)
                {
                    continue;
                }

                // Literal key
                var key = TemplateUtil.AssertLiteral(pair.Key, "inputs key");

                // Expression value
                if (allowExpressions && pair.Value is ExpressionToken)
                {
                    continue;
                }

                // Literal value
                var value = TemplateUtil.AssertLiteral(pair.Value, "inputs value");
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

            var strategyMapping = TemplateUtil.AssertMapping(token, PipelineTemplateConstants.Strategy);
            var matrixBuilder = default(MatrixBuilder);
            var parallelism = 0;
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
                var strategyKey = TemplateUtil.AssertLiteral(strategyPair.Key, "strategy key");

                switch (strategyKey.Value)
                {
                    // Fail-Fast
                    case PipelineTemplateConstants.FailFast:
                        if (allowExpressions && strategyPair.Value is ExpressionToken)
                        {
                            hasExpressions = true;
                            continue;
                        }

                        var failFast = TemplateUtil.AssertLiteral(strategyPair.Value, $"strategy {PipelineTemplateConstants.FailFast}");
                        result.FailFast = Boolean.Parse(failFast.Value);
                        break;

                    // Max-Parallel
                    case PipelineTemplateConstants.MaxParallel:
                        if (allowExpressions && strategyPair.Value is ExpressionToken)
                        {
                            hasExpressions = true;
                            continue;
                        }

                        result.MaxParallel = ConvertToInt32(
                            context,
                            strategyPair.Value,
                            $"strategy {PipelineTemplateConstants.MaxParallel}",
                            "Invalid max parallel '{0}'");
                        break;

                    // Matrix
                    case PipelineTemplateConstants.Matrix:

                        // Expression
                        if (allowExpressions && strategyPair.Value is ExpressionToken)
                        {
                            hasExpressions = true;
                            continue;
                        }

                        var matrix = TemplateUtil.AssertMapping(strategyPair.Value, "matrix");
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

                            var matrixKey = TemplateUtil.AssertLiteral(matrixPair.Key, "matrix key");
                            switch (matrixKey.Value)
                            {
                                case PipelineTemplateConstants.Include:
                                    if (allowExpressions && matrixPair.Value is ExpressionToken)
                                    {
                                        continue;
                                    }

                                    var includeSequence = TemplateUtil.AssertSequence(matrixPair.Value, "matrix includes");
                                    matrixBuilder.Include(includeSequence);
                                    break;

                                case PipelineTemplateConstants.Exclude:
                                    if (allowExpressions && matrixPair.Value is ExpressionToken)
                                    {
                                        continue;
                                    }

                                    var excludeSequence = TemplateUtil.AssertSequence(matrixPair.Value, "matrix excludes");
                                    matrixBuilder.Exclude(excludeSequence);
                                    break;

                                default:
                                    hasVector = true;

                                    if (allowExpressions && matrixPair.Value is ExpressionToken)
                                    {
                                        continue;
                                    }

                                    var vectorName = matrixKey.Value;
                                    var vectorSequence = TemplateUtil.AssertSequence(matrixPair.Value, "matrix vector value");
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
                        parallelism = ConvertToInt32(
                            context,
                            strategyPair.Value,
                            "parallel",
                            "Invalid parallel setting '{0}'. Must be an integer greater than zero.",
                            allowExpressions: false,
                            minValue: 1);
                        break;

                    default:
                        TemplateUtil.AssertUnexpectedValue(strategyKey, "strategy key"); // throws
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
            else if (parallelism > 0)
            {
                var nameBuilder = new ReferenceNameBuilder();
                var displayNameBuilder = new JobDisplayNameBuilder(jobFactoryDisplayName);

                for (var parallelIndex = 0; parallelIndex < parallelism; parallelIndex++)
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
                    var parallel = new StringContextData(parallelism.ToString(CultureInfo.InvariantCulture));
                    configuration.ContextData.Add(PipelineTemplateConstants.Parallel, parallel);
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

        private static IEnumerable<PhaseNode> ConvertToJobFactories(
            TemplateContext context,
            PipelineResources resources,
            TemplateToken workflow)
        {
            var workflowMapping = TemplateUtil.AssertMapping(workflow, PipelineTemplateConstants.Workflow);

            foreach (var workflowPair in workflowMapping)
            {
                var workflowNameToken = TemplateUtil.AssertLiteral(workflowPair.Key, $"{PipelineTemplateConstants.Workflow} key");
                if (!NameValidation.IsValid(workflowNameToken.Value, true))
                {
                    context.Error(workflowNameToken, $"Job name {workflowNameToken.Value} is invalid. Names must start with a letter or '_' and contain only alphanumeric characters, '-', or '_'");
                }
                var result = new JobFactory
                {
                    Name = workflowNameToken.Value
                };

                var jobFactoryDefinition = TemplateUtil.AssertMapping(workflowPair.Value, $"{PipelineTemplateConstants.Workflow} value");

                foreach (var jobFactoryProperty in jobFactoryDefinition)
                {
                    var propertyName = TemplateUtil.AssertLiteral(jobFactoryProperty.Key, $"job property name");

                    switch (propertyName.Value)
                    {
                        case PipelineTemplateConstants.Actions:
                            result.Steps.AddRange(ConvertToSteps(context, jobFactoryProperty.Value));
                            break;

                        case PipelineTemplateConstants.ContinueOnError:
                            var continueOnError = TemplateUtil.AssertLiteral(jobFactoryProperty.Value, $"job {PipelineTemplateConstants.ContinueOnError}");
                            result.ContinueOnError = Boolean.Parse(continueOnError.Value);
                            break;

                        case PipelineTemplateConstants.If:
                            var ifCondition = TemplateUtil.AssertLiteral(jobFactoryProperty.Value, $"job {PipelineTemplateConstants.If}");
                            result.Condition = ConvertToIfCondition(context, ifCondition, isJob: true);
                            break;

                        case PipelineTemplateConstants.Name:
                            var displayName = TemplateUtil.AssertScalar(jobFactoryProperty.Value, $"job {PipelineTemplateConstants.Name}");
                            ConvertToJobDisplayName(context, displayName, allowExpressions: true); // Validate early if possible
                            if (displayName is LiteralToken)
                            {
                                result.DisplayName = displayName.ToString();
                            }
                            else
                            {
                                result.JobDisplayName = displayName.Clone(true) as ExpressionToken;
                            }
                            break;

                        case PipelineTemplateConstants.Needs:
                            if (jobFactoryProperty.Value is LiteralToken needsLiteral)
                            {
                                result.DependsOn.Add(needsLiteral.Value);
                            }
                            else
                            {
                                var needs = TemplateUtil.AssertSequence(jobFactoryProperty.Value, $"job {PipelineTemplateConstants.Needs}");
                                foreach (var needsItem in needs)
                                {
                                    var need = TemplateUtil.AssertLiteral(needsItem, $"job {PipelineTemplateConstants.Needs} item");
                                    result.DependsOn.Add(need.Value);
                                }
                            }
                            break;

                        case PipelineTemplateConstants.RunsOn:
                            ConvertToJobTarget(context, jobFactoryProperty.Value, allowExpressions: true); // Validate early if possible
                            result.JobTarget = jobFactoryProperty.Value.Clone(true);
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

                        default:
                            TemplateUtil.AssertUnexpectedValue(propertyName, "job key"); // throws
                            break;
                    }
                }

                // todo: Move "required" support into schema validation
                if (result.JobTarget == null)
                {
                    context.Error(workflowMapping, $"The '{PipelineTemplateConstants.RunsOn}' property is required");
                }

                if (String.IsNullOrEmpty(result.DisplayName))
                {
                    result.DisplayName = result.Name;
                }

                yield return result;
            }
        }

        private static List<Step> ConvertToSteps(
            TemplateContext context,
            TemplateToken actions)
        {
            var actionsSequence = TemplateUtil.AssertSequence(actions, $"job {PipelineTemplateConstants.Actions}");

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
                inputs.Add(new LiteralToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Repository), new LiteralToken(null, null, null, PipelineConstants.SelfAlias));
                checkoutAction.Inputs = inputs;

                result.Insert(0, checkoutAction);
            }

            return result;
        }

        private static ActionStep ConvertToStep(
            TemplateContext context,
            TemplateToken actionsItem)
        {
            var action = TemplateUtil.AssertMapping(actionsItem, $"{PipelineTemplateConstants.Actions} item");
            var checkout = default(LiteralToken);
            var env = default(TemplateToken);
            var id = default(LiteralToken);
            var ifCondition = default(String);
            var name = default(LiteralToken);
            var run = default(ScalarToken);
            var timeout = default(LiteralToken);
            var uses = default(LiteralToken);
            var with = default(TemplateToken);
            var workingDir = default(ScalarToken);
            var path = default(ScalarToken);
            var clean = default(ScalarToken);
            var fetchDepth = default(ScalarToken);
            var lfs = default(ScalarToken);
            var submodules = default(ScalarToken);

            foreach (var actionProperty in action)
            {
                var propertyName = TemplateUtil.AssertLiteral(actionProperty.Key, $"{PipelineTemplateConstants.Actions} item key");

                switch (propertyName.Value)
                {
                    case PipelineTemplateConstants.Checkout:
                        checkout = TemplateUtil.AssertLiteral(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Checkout}");
                        break;

                    case PipelineTemplateConstants.Clean:
                        clean = TemplateUtil.AssertScalar(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Clean}");
                        break;

                    case PipelineTemplateConstants.Env:
                        ConvertToStepEnvironment(context, actionProperty.Value, StringComparer.Ordinal, allowExpressions: true); // Validate early if possible
                        env = actionProperty.Value;
                        break;

                    case PipelineTemplateConstants.FetchDepth:
                        fetchDepth = TemplateUtil.AssertScalar(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.FetchDepth}");
                        break;

                    case PipelineTemplateConstants.Id:
                        id = TemplateUtil.AssertLiteral(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Id}");
                        if (!NameValidation.IsValid(id.Value, true))
                        {
                            context.Error(id, $"Action id {id.Value} is invalid. Ids must start with a letter or '_' and contain only alphanumeric characters, '-', or '_'");
                        }
                        break;

                    case PipelineTemplateConstants.If:
                        var ifToken = TemplateUtil.AssertLiteral(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.If}");
                        ifCondition = ConvertToIfCondition(context, ifToken, isJob: false);
                        break;

                    case PipelineTemplateConstants.Lfs:
                        lfs = TemplateUtil.AssertScalar(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Lfs}");
                        break;

                    case PipelineTemplateConstants.Name:
                        name = TemplateUtil.AssertLiteral(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Name}");
                        break;

                    case PipelineTemplateConstants.Path:
                        path = TemplateUtil.AssertScalar(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Path}");
                        break;

                    case PipelineTemplateConstants.Run:
                        run = TemplateUtil.AssertScalar(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Run}");
                        break;

                    case PipelineTemplateConstants.Submodules:
                        submodules = TemplateUtil.AssertScalar(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Submodules}");
                        break;

                    case PipelineTemplateConstants.Timeout:
                        timeout = TemplateUtil.AssertLiteral(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Timeout}");
                        break;

                    case PipelineTemplateConstants.Uses:
                        uses = TemplateUtil.AssertLiteral(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.Uses}");
                        break;

                    case PipelineTemplateConstants.With:
                        ConvertToStepInputs(context, actionProperty.Value, allowExpressions: true); // Validate early if possible
                        with = actionProperty.Value;
                        break;

                    case PipelineTemplateConstants.WorkingDirectory:
                        workingDir = TemplateUtil.AssertScalar(actionProperty.Value, $"{PipelineTemplateConstants.Actions} item {PipelineTemplateConstants.WorkingDirectory}");
                        break;

                    default:
                        TemplateUtil.AssertUnexpectedValue(propertyName, $"{PipelineTemplateConstants.Actions} item key"); // throws
                        break;
                }
            }

            if (run != null)
            {
                var result = new ActionStep
                {
                    Name = id?.Value,
                    DisplayName = name?.Value,
                    Condition = ifCondition,
                    TimeoutInMinutes = timeout != null ? Int32.Parse(timeout.Value) : 0,
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
                inputs.Add(new LiteralToken(null, null, null, PipelineConstants.ScriptStepInputs.Script), run.Clone(true));

                if (workingDir != null)
                {
                    inputs.Add(new LiteralToken(null, null, null, PipelineConstants.ScriptStepInputs.WorkingDirectory), workingDir.Clone(true));
                }

                result.Inputs = inputs;

                return result;
            }
            else if (checkout != null)
            {
                var result = new ActionStep
                {
                    Name = id?.Value,
                    DisplayName = name?.Value,
                    Condition = ifCondition,
                    TimeoutInMinutes = timeout != null ? Int32.Parse(timeout.Value) : 0,
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
                    inputs.Add(new LiteralToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Repository), new LiteralToken(null, null, null, PipelineConstants.SelfAlias));
                }
                else if (string.Equals(checkout.Value, bool.FalseString, StringComparison.OrdinalIgnoreCase))
                {
                    // `- checkout: false` means not checkout, we will set the enable to false and let it get skipped.
                    inputs.Add(new LiteralToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Repository), new LiteralToken(null, null, null, PipelineConstants.SelfAlias));
                    result.Enabled = false;
                }
                else
                {
                    inputs.Add(new LiteralToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Repository), checkout.Clone(true));
                }

                if (path != null)
                {
                    inputs.Add(new LiteralToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Path), path.Clone(true));
                }

                if (clean != null)
                {
                    inputs.Add(new LiteralToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Clean), clean.Clone(true));
                }

                if (fetchDepth != null)
                {
                    inputs.Add(new LiteralToken(null, null, null, PipelineConstants.CheckoutTaskInputs.FetchDepth), fetchDepth.Clone(true));
                }

                if (lfs != null)
                {
                    inputs.Add(new LiteralToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Lfs), lfs.Clone(true));
                }

                if (submodules != null)
                {
                    inputs.Add(new LiteralToken(null, null, null, PipelineConstants.CheckoutTaskInputs.Submodules), submodules.Clone(true));
                }

                result.Inputs = inputs;

                return result;
            }
            else if (uses != null)
            {
                var result = new ActionStep
                {
                    Name = id?.Value,
                    DisplayName = name?.Value,
                    Condition = ifCondition,
                    TimeoutInMinutes = timeout != null ? Int32.Parse(timeout.Value) : 0,
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
            LiteralToken ifCondition,
            Boolean isJob)
        {
            if (String.IsNullOrWhiteSpace(ifCondition.Value))
            {
                return "succeeded()";

            }

            var condition = ifCondition.Value;

            var parserOptions = new ExpressionParserOptions() { AllowHyphens = true };
            var expressionParser = new ExpressionParser(parserOptions);
            var namedValues = isJob ? null : s_actionNamedValues;
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

        private static Int32 ConvertToInt32(
            TemplateContext context,
            TemplateToken token,
            String objectName,
            String errorFormat,
            Boolean allowExpressions = false,
            Int32 minValue = Int32.MinValue)
        {
            if (allowExpressions && token is BasicExpressionToken)
            {
                return default(Int32);
            }

            var literal = TemplateUtil.AssertLiteral(token, objectName);
            if (Int32.TryParse(literal.Value ?? String.Empty, NumberStyles.None, CultureInfo.InvariantCulture, out var value) &&
                value >= minValue)
            {
                return value;
            }

            context.Error(literal, String.Format(CultureInfo.InvariantCulture, errorFormat, literal.Value));
            return default(Int32);
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
            new NamedValueInfo<NoOpValue>(PipelineTemplateConstants.Actions),
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
