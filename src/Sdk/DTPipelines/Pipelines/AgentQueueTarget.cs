using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Pipelines.Runtime;
using GitHub.DistributedTask.Pipelines.Validation;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides options for phase execution on an agent within a queue.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AgentQueueTarget : PhaseTarget
    {
        public AgentQueueTarget()
            : base(PhaseTargetType.Queue)
        {
        }

        private AgentQueueTarget(AgentQueueTarget targetToClone)
            : base(targetToClone)
        {
            this.Queue = targetToClone.Queue?.Clone();
            this.Execution = targetToClone.Execution?.Clone();

            if (targetToClone.AgentSpecification != null)
            {
                this.AgentSpecification = new JObject(targetToClone.AgentSpecification);
            }

            if (targetToClone.SidecarContainers?.Count > 0)
            {
                m_sidecarContainers = new Dictionary<String, ExpressionValue<String>>(targetToClone.SidecarContainers, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets or sets the target queue from which agents will be selected.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(QueueJsonConverter))]
        public AgentQueueReference Queue
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public JObject AgentSpecification
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets parallel execution options which control expansion and execution of the phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public ParallelExecutionOptions Execution
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets workspace options which control how agent manage the workspace of the phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public WorkspaceOptions Workspace
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the container the phase will be run in.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [JsonConverter(typeof(ExpressionValueJsonConverter<String>))]
        public ExpressionValue<String> Container
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the sidecar containers that will run alongside the phase.
        /// </summary>
        public IDictionary<String, ExpressionValue<String>> SidecarContainers
        {
            get
            {
                if (m_sidecarContainers == null)
                {
                    m_sidecarContainers = new Dictionary<String, ExpressionValue<String>>(StringComparer.OrdinalIgnoreCase);
                }
                return m_sidecarContainers;
            }
        }

        public override PhaseTarget Clone()
        {
            return new AgentQueueTarget(this);
        }

        public override Boolean IsValid(TaskDefinition task)
        {
            ArgumentUtility.CheckForNull(task, nameof(task));
            return task.RunsOn.Contains(TaskRunsOnConstants.RunsOnAgent, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a clone of this and attempts to resolve all expressions and macros.
        /// </summary>
        internal AgentQueueTarget Evaluate(
            IPipelineContext context,
            ValidationResult result)
        {
            var qname = String.Empty;
            try
            {
                qname = context.ExpandVariables(this.Queue?.Name?.GetValue(context).Value);
            }
            catch (DistributedTask.Expressions.ExpressionException ee)
            {
                result.Errors.Add(new PipelineValidationError(ee.Message));
                return null;
            }

            var literalTarget = this.Clone() as AgentQueueTarget;

            var spec = this.AgentSpecification;
            if (spec != null)
            {
                spec = context.Evaluate(this.AgentSpecification).Value;
                literalTarget.AgentSpecification = spec;
            }

            // Note! The "vmImage" token of the agent spec is currently treated specially. 
            // This is a temporary relationship that allows vmImage agent specs to specify
            //  the hosted pool to use.
            // It would be better to factor out this work into a separate, plug-in validator. 
            if (String.IsNullOrEmpty(qname) && spec != null)
            {
                const string VMImage = "vmImage"; // should be: YamlConstants.VMImage, which is inaccessible :(
                spec.TryGetValue(VMImage, out var token);
                if (token != null && token.Type == JTokenType.String)
                {
                    var rawTokenValue = token.Value<String>();
                    var resolvedPoolName = PoolNameForVMImage(rawTokenValue);
                    if (resolvedPoolName == null)
                    {
                        result.Errors.Add(new PipelineValidationError($"Unexpected vmImage '{rawTokenValue}'"));
                        return null;
                    }
                    else
                    {
                        spec.Remove(VMImage);
                        literalTarget.Queue = new AgentQueueReference
                        {
                            Name = resolvedPoolName
                        };
                    }
                }
            }
            else
            {
                literalTarget.Queue.Name = qname;
            }

            return literalTarget;
        }

        /// <summary>
        /// returns true for strings structured like expressions or macros. 
        /// they could techincally be literals though.
        /// </summary>
        internal static Boolean IsProbablyExpressionOrMacro(String s)
        {
            return ExpressionValue.IsExpression(s) || VariableUtility.IsVariable(s);
        }

        /// <summary>
        /// returns true if this model is composed only of literal values (no expressions)
        /// </summary>
        internal Boolean IsLiteral()
        {
            var queue = this.Queue;
            if (queue != null)
            {
                var queueName = queue.Name;
                if (queueName != null)
                {
                    if (!queueName.IsLiteral || VariableUtility.IsVariable(queueName.Literal))
                    {
                        return false;
                    }
                }
            }

            var spec = this.AgentSpecification;
            if (spec != null)
            {
                bool IsLiteral(JObject o)
                {
                    foreach (var pair in o)
                    {
                        switch (pair.Value.Type)
                        {
                            case JTokenType.String:
                                if (IsProbablyExpressionOrMacro(pair.Value.Value<String>()))
                                {
                                    return false;
                                }
                                break;
                            case JTokenType.Object:
                                if (!IsLiteral(pair.Value.Value<JObject>()))
                                {
                                    return false;
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    return true;
                }

                if (!IsLiteral(spec))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Temporary code to translate vmImage. Pool providers work will move this to a different layer
        /// </summary>
        /// <param name="vmImageValue"></param>
        /// <returns>Hosted pool name</returns>
        internal static String PoolNameForVMImage(String vmImageValue)
        {
            switch ((vmImageValue ?? String.Empty).ToUpperInvariant())
            {
                case "UBUNTU 16.04":
                case "UBUNTU-16.04":
                case "UBUNTU LATEST":
                case "UBUNTU-LATEST":
                    return "Hosted Ubuntu 1604";
                case "UBUNTU 18.04":
                case "UBUNTU-18.04":
                    return "Hosted Ubuntu 1804";
                case "VISUAL STUDIO 2015 ON WINDOWS SERVER 2012R2":
                case "VS2015-WIN2012R2":
                    return "Hosted";
                case "VISUAL STUDIO 2017 ON WINDOWS SERVER 2016":
                case "VS2017-WIN2016":
                    return "Hosted VS2017";
                case "WINDOWS-2019-VS2019":
                case "WINDOWS-2019":
                case "WINDOWS LATEST":
                case "WINDOWS-LATEST":
                    return "Hosted Windows 2019 with VS2019";
                case "WINDOWS SERVER 1803":
                case "WIN1803":
                    return "Hosted Windows Container";
                case "MACOS 10.13":
                case "MACOS-10.13":
                case "XCODE 9 ON MACOS 10.13":
                case "XCODE9-MACOS10.13":
                case "XCODE 10 ON MACOS 10.13":
                case "XCODE10-MACOS10.13":
                    return "Hosted macOS High Sierra";
                case "MACOS 10.14":
                case "MACOS-10.14":
                case "MACOS LATEST":
                case "MACOS-LATEST":
                    return "Hosted macOS";
                default:
                    return null;
            }
        }

        /// <summary>
        /// PipelineBuildContexts have build options. 
        /// GraphExecutionContexts have dependencies. 
        /// We might need either depending on the situation. 
        /// </summary>
        private TaskAgentPoolReference ValidateQueue(
            IPipelineContext context,
            ValidationResult result,
            BuildOptions buildOptions)
        {
            var queueId = 0;
            var queueName = (String)null;
            var queueNameIsUnresolvableExpression = false; // true iff Name is an expression, we're allowed to use them, and it has no current value
            var queue = this.Queue;
            if (queue != null)
            {
                queueId = queue.Id;

                // resolve name
                var expressionValueName = queue.Name;
                if (expressionValueName != null && (buildOptions.EnableResourceExpressions || expressionValueName.IsLiteral))
                {
                    // resolve expression
                    try
                    {
                        queueName = expressionValueName.GetValue(context).Value;
                        queueNameIsUnresolvableExpression = !expressionValueName.IsLiteral && String.IsNullOrEmpty(queueName);
                    }
                    catch (Exception ee)
                    {
                        // something bad happened trying to fetch the value. 
                        // We do not really care what though. Just record the error and move on. 
                        queueName = null;

                        if (buildOptions.ValidateExpressions && buildOptions.ValidateResources)
                        {
                            result.Errors.Add(new PipelineValidationError(ee.Message));
                        }
                    }

                    // resolve name macro
                    if (buildOptions.EnableResourceExpressions && queueName != null && VariableUtility.IsVariable(queueName))
                    {
                        queueName = context.ExpandVariables(queueName);
                        if (VariableUtility.IsVariable(queueName))
                        {
                            // name appears to be a macro that is not defined. 
                            queueNameIsUnresolvableExpression = true;
                        }
                    }
                }
            }

            if (queueNameIsUnresolvableExpression || (queueId == 0 && String.IsNullOrEmpty(queueName)))
            {
                // could not determine what queue user was talking about
                if (!buildOptions.AllowEmptyQueueTarget && buildOptions.ValidateResources)
                {
                    // expression-based queue names are allowed to be unresolved at compile time.
                    // TEMPORARY: literal queue names do not error at compile time if special keys exist
                    if (!queueNameIsUnresolvableExpression || buildOptions.ValidateExpressions)
                    {
                        if (!String.IsNullOrEmpty(queueName))
                        {
                            result.Errors.Add(new PipelineValidationError(PipelineStrings.QueueNotFoundByName(queueName)));
                        }
                        else
                        {
                            var expressionValueName = queue?.Name;
                            if (expressionValueName == null || expressionValueName.IsLiteral)
                            {
                                result.Errors.Add(new PipelineValidationError(PipelineStrings.QueueNotDefined()));
                            }
                            else if (expressionValueName != null)
                            {
                                result.Errors.Add(new PipelineValidationError(PipelineStrings.QueueNotFoundByName(expressionValueName.Expression)));
                            }
                        }
                    }
                }
            }
            else
            {
                // we have a valid queue. record the reference
                result.AddQueueReference(id: queueId, name: queueName);

                // Attempt to resolve the queue using any identifier specified. We will look up by either ID
                // or name and the ID is preferred since it is immutable and more specific.
                if (buildOptions.ValidateResources)
                {
                    TaskAgentQueue taskAgentQueue = null;
                    var resourceStore = context.ResourceStore;
                    if (resourceStore != null)
                    {
                        if (queueId != 0)
                        {
                            taskAgentQueue = resourceStore.GetQueue(queueId);
                            if (taskAgentQueue == null)
                            {
                                result.UnauthorizedResources.Queues.Add(new AgentQueueReference { Id = queueId });
                                result.Errors.Add(new PipelineValidationError(PipelineStrings.QueueNotFound(queueId)));
                            }
                        }
                        else if (!String.IsNullOrEmpty(queueName))
                        {
                            taskAgentQueue = resourceStore.GetQueue(queueName);
                            if (taskAgentQueue == null)
                            {
                                result.UnauthorizedResources.Queues.Add(new AgentQueueReference { Name = queueName });
                                result.Errors.Add(new PipelineValidationError(PipelineStrings.QueueNotFoundByName(queueName)));
                            }
                        }
                    }

                    // Store the resolved values inline to the resolved resource for this validation run
                    if (taskAgentQueue != null)
                    {
                        this.Queue.Id = taskAgentQueue.Id;
                        return taskAgentQueue.Pool;
                    }
                }
            }

            return null;
        }

        internal override void Validate(
            IPipelineContext context,
            BuildOptions buildOptions,
            ValidationResult result,
            IList<Step> steps,
            ISet<Demand> taskDemands)
        {
            // validate queue
            var resolvedPool = ValidateQueue(context, result, buildOptions);
            Boolean includeTaskDemands = resolvedPool == null || !resolvedPool.IsHosted;

            // Add advanced-checkout min agent demand
            Boolean advancedCheckout = false;
            int checkoutTasks = 0;
            int injectedSystemTasks = 0;
            bool countInjectSystemTasks = true;
            for (int index = 0; index < steps.Count; index++)
            {
                var step = steps[index];
                // Task
                if (step.Type == StepType.Task)
                {
                    var task = step as TaskStep;
                    if (task.Name.StartsWith("__system_"))
                    {
                        if (countInjectSystemTasks)
                        {
                            injectedSystemTasks++;
                        }
                    }
                    else if (task.IsCheckoutTask())
                    {
                        countInjectSystemTasks = false;
                        checkoutTasks++;
                        if (context.EnvironmentVersion < 2)
                        {
                            if (index > 0 && index - injectedSystemTasks > 0)
                            {
                                result.Errors.Add(new PipelineValidationError(PipelineStrings.CheckoutMustBeTheFirstStep()));
                            }
                        }
                        else
                        {
                            if (index > 0)
                            {
                                advancedCheckout = true;
                            }
                        }

                        if (task.Inputs.TryGetValue(PipelineConstants.CheckoutTaskInputs.Repository, out String repository) &&
                            !String.Equals(repository, PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase) &&
                            !String.Equals(repository, PipelineConstants.NoneAlias, StringComparison.OrdinalIgnoreCase) &&
                            !String.Equals(repository, PipelineConstants.DesignerRepo, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Errors.Add(new PipelineValidationError(PipelineStrings.CheckoutStepRepositoryNotSupported(task.Inputs[PipelineConstants.CheckoutTaskInputs.Repository])));
                        }
                    }
                    else
                    {
                        countInjectSystemTasks = false;
                    }
                }
            }

            if (checkoutTasks > 1)
            {
                result.Errors.Add(new PipelineValidationError(PipelineStrings.CheckoutMultipleRepositoryNotSupported()));
            }

            if (advancedCheckout)
            {
                taskDemands.Add(new DemandMinimumVersion(PipelineConstants.AgentVersionDemandName, PipelineConstants.AdvancedCheckoutMinAgentVersion));
            }

            // Now we need to ensure we have only a single demand for the mimimum agent version. We effectively remove
            // every agent version demand we find and keep track of the one with the highest value. Assuming we located
            // one or more of these demands we will ensure it is merged in at the end.
            var minimumAgentVersionDemand = ResolveAgentVersionDemand(taskDemands);
            minimumAgentVersionDemand = ResolveAgentVersionDemand(this.Demands, minimumAgentVersionDemand);

            // not include demands from task if phase is running inside container
            // container suppose provide any required tool task needs
            if (this.Container != null)
            {
                includeTaskDemands = false;
            }

            // Merge the phase demands with the implicit demands from tasks.
            if (includeTaskDemands && buildOptions.RollupStepDemands)
            {
                this.Demands.UnionWith(taskDemands);
            }

            // If we resolved a minimum agent version demand then we go ahead and merge it in
            // We want to do this even if targetting Hosted
            if (minimumAgentVersionDemand != null)
            {
                this.Demands.Add(minimumAgentVersionDemand);
            }
        }

        private static DemandMinimumVersion ResolveAgentVersionDemand(
            ISet<Demand> demands,
            DemandMinimumVersion currentMinimumVersion = null)
        {
            var minVersionDemand = DemandMinimumVersion.MaxAndRemove(demands);
            if (minVersionDemand != null && (currentMinimumVersion == null || DemandMinimumVersion.CompareVersion(minVersionDemand.Value, currentMinimumVersion.Value) > 0))
            {
                return minVersionDemand;
            }
            else
            {
                return currentMinimumVersion;
            }
        }

        internal override JobExecutionContext CreateJobContext(
            PhaseExecutionContext context,
            String jobName,
            Int32 attempt,
            Boolean continueOnError,
            Int32 timeoutInMinutes,
            Int32 cancelTimeoutInMinutes,
            IJobFactory jobFactory)
        {
            context.Trace?.EnterProperty("CreateJobContext");
            var execution = this.Execution ?? new ParallelExecutionOptions();
            var jobContext = execution.CreateJobContext(
                context,
                jobName,
                attempt,
                this.Container,
                this.SidecarContainers,
                continueOnError,
                timeoutInMinutes,
                cancelTimeoutInMinutes,
                jobFactory);
            context.Trace?.LeaveProperty("CreateJobContext");

            if (jobContext != null)
            {
                jobContext.Job.Definition.Workspace = this.Workspace?.Clone();
            }

            return jobContext;
        }

        internal override ExpandPhaseResult Expand(
            PhaseExecutionContext context,
            Boolean continueOnError,
            Int32 timeoutInMinutes,
            Int32 cancelTimeoutInMinutes,
            IJobFactory jobFactory,
            JobExpansionOptions options)
        {
            context.Trace?.EnterProperty("Expand");
            var execution = this.Execution ?? new ParallelExecutionOptions();
            var result = execution.Expand(
                context,
                this.Container,
                this.SidecarContainers,
                continueOnError,
                timeoutInMinutes,
                cancelTimeoutInMinutes,
                jobFactory,
                options);
            context.Trace?.LeaveProperty("Expand");

            foreach (var job in result.Jobs)
            {
                job.Definition.Workspace = this.Workspace?.Clone();
            }

            return result;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_sidecarContainers?.Count == 0)
            {
                m_sidecarContainers = null;
            }
        }

        [DataMember(Name = "SidecarContainers", EmitDefaultValue = false)]
        private IDictionary<String, ExpressionValue<String>> m_sidecarContainers;

        /// <summary>
        /// Ensures conversion of a TaskAgentQueue into an AgentQueueReference works properly when the serializer 
        /// is configured to write/honor type information. This is a temporary converter that may be removed after
        /// M127 ships.
        /// </summary>
        private sealed class QueueJsonConverter : VssSecureJsonConverter
        {
            public override Boolean CanWrite => false;

            public override Boolean CanConvert(Type objectType)
            {
                return objectType.Equals(typeof(AgentQueueReference));
            }

            public override Object ReadJson(
                JsonReader reader,
                Type objectType,
                Object existingValue,
                JsonSerializer serializer)
            {
                var rawValue = JObject.Load(reader);
                using (var objectReader = rawValue.CreateReader())
                {
                    var newValue = new AgentQueueReference();
                    serializer.Populate(objectReader, newValue);
                    return newValue;
                }
            }

            public override void WriteJson(
                JsonWriter writer,
                Object value,
                JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
