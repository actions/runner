using System.Globalization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    public static class PipelineStrings
    {
        public static string AmbiguousQueueSpecification(params object[] args)
        {
            const string Format = @"The pool name {0} is ambiguous.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AmbiguousSecureFileSpecification(params object[] args)
        {
            const string Format = @"The secure file name {0} is ambiguous.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AmbiguousServiceEndpointSpecification(params object[] args)
        {
            const string Format = @"The service connection name {0} is ambiguous.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AmbiguousTaskSpecification(params object[] args)
        {
            const string Format = @"The task name {0} is ambiguous. Specify one of the following identifiers to resolve the ambiguity: {1}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AmbiguousVariableGroupSpecification(params object[] args)
        {
            const string Format = @"The variable group name {0} is ambiguous.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AzureKeyVaultTaskName(params object[] args)
        {
            const string Format = @"Download secrets: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerResourceInvalidRegistryEndpointType(params object[] args)
        {
            const string Format = @"Expected 'dockerregistry' service connection type for image registry referenced by {0}, but got {1} for service connection {2}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerResourceNotFound(params object[] args)
        {
            const string Format = @"A container resource with name {0} could not be found. The container resource does not exist. If you intended to specify an image, use NAME:TAG or NAME@DIGEST. For example, ubuntu:latest";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContainerEndpointNotFound(params object[] args)
        {
            const string Format = @"Container {0} references service connection {1} which does not exist or is not authorized for use.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string CheckoutMultipleRepositoryNotSupported(params object[] args)
        {
            const string Format = @"Checkout of multiple repositories is not supported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string CheckoutStepRepositoryNotSupported(params object[] args)
        {
            const string Format = @"Checkout of repository '{0}' is not supported. Only 'self' and 'none' are supported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string CheckoutMustBeTheFirstStep(params object[] args)
        {
            const string Format = @"Checkout should be the first step in the job.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ExpressionInvalid(params object[] args)
        {
            const string Format = @"'{0}' is not a valid expression. Expressions must be enclosed with '$[' and ']'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DemandExpansionInvalid(params object[] args)
        {
            const string Format = @"Demand '{0}' is not valid when '{1}' evaluates to '{2}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseGraphCycleDetected(params object[] args)
        {
            const string Format = @"Job {0} depends on job {1} which creates a cycle in the dependency graph.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StageGraphCycleDetected(params object[] args)
        {
            const string Format = @"Stage {0} depends on stage {1} which creates a cycle in the dependency graph.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StagePhaseGraphCycleDetected(params object[] args)
        {
            const string Format = @"Stage {0} job {1} depends on job {2} which creates a cycle in the dependency graph.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidRegexOptions(params object[] args)
        {
            const string Format = @"Provider regex options '{0}' are invalid. Supported combination of flags: `{1}`. Eg: 'IgnoreCase, Multiline', 'Multiline'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidRetryStageNeverRun(params object[] args)
        {
            const string Format = @"Unable to retry stage {0} because it has never been run.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidRetryStageNotComplete(params object[] args)
        {
            const string Format = @"Unable to retry the pipeline because stage {0} is currently in progress.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidTypeForLengthFunction(params object[] args)
        {
            const string Format = @"Kind '{0}' not supported. Only arrays, strings, dictionaries, or collections are supported for the length function.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidValidationOptionNoImplementation(params object[] args)
        {
            const string Format = @"The validation option {0} was specified but no implementation was provided for {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseDependencyNotFound(params object[] args)
        {
            const string Format = @"Job {0} depends on unknown job {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StagePhaseDependencyNotFound(params object[] args)
        {
            const string Format = @"Stage {0} job {1} depends on unknown job {2}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StageDependencyNotFound(params object[] args)
        {
            const string Format = @"Stage {0} depends on unknown stage {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseJobNameInvalidForSlicing(params object[] args)
        {
            const string Format = @"The job name {0} is not valid for the specified execution options. Valid jobs names include JobN or N, where N is a value from 1 to maximum parallelism.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseJobNumberDoesNotExist(params object[] args)
        {
            const string Format = @"Job {0} uses a maximum parallelism of {1}. The job {2} does not exist with the specified parallelism settings.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseJobMatrixExpansionExceedLimit(params object[] args)
        {
            const string Format = @"The matrix expansion resulted in {0} jobs which exceeds the maximum allowable job count of {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseJobSlicingExpansionExceedLimit(params object[] args)
        {
            const string Format = @"The slicing expansion resulted in {0} jobs which exceeds the maximum allowable job count of {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseMatrixConfigurationDoesNotExist(params object[] args)
        {
            const string Format = @"Job {0} does not specify a matrix configuration named {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseNameInvalid(params object[] args)
        {
            const string Format = @"Job {0} has an invalid name. Valid names may only contain alphanumeric characters and '_' and may not start with a number.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StageNameInvalid(params object[] args)
        {
            const string Format = @"Stage {0} has an invalid name. Valid names may only contain alphanumeric characters and '_' and may not start with a number.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StagePhaseNameInvalid(params object[] args)
        {
            const string Format = @"Stage {0} job {1} has an invalid name. Valid names may only contain alphanumeric characters and '_' and may not start with a number.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseNamesMustBeUnique(params object[] args)
        {
            const string Format = @"The job name {0} appears more than once. Job names must be unique within a pipeline.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StagePhaseNamesMustBeUnique(params object[] args)
        {
            const string Format = @"Stage {0} job {1} appears more than once. Job names must be unique within a stage.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseTargetRequired(params object[] args)
        {
            const string Format = @"Job {0}: Target is required.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StageVariableGroupNotSupported(params object[] args)
        {
            const string Format = @"Stage {0}: Variable group reference {1} is not supported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PhaseVariableGroupNotSupported(params object[] args)
        {
            const string Format = @"Job {0}: Variable group reference {1} is not supported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PipelineNotValid(params object[] args)
        {
            const string Format = @"The pipeline is not valid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PipelineNotValidWithErrors(params object[] args)
        {
            const string Format = @"The pipeline is not valid. {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PipelineNotValidNoStartingPhase(params object[] args)
        {
            const string Format = @"The pipeline must contain at least one job with no dependencies.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PipelineNotValidNoStartingStage(params object[] args)
        {
            const string Format = @"The pipeline must contain at least one stage with no dependencies.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StageNotValidNoStartingPhase(params object[] args)
        {
            const string Format = @"Stage {0} must contain at least one job with no dependencies.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string QueueNotDefined(params object[] args)
        {
            const string Format = @"Either a pool ID or name is required.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string QueueNotFound(params object[] args)
        {
            const string Format = @"Could not find a pool with ID {0}. The pool does not exist or has not been authorized for use. For authorization details, refer to https://aka.ms/yamlauthz.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string QueueNotFoundByName(params object[] args)
        {
            const string Format = @"Could not find a pool with name {0}. The pool does not exist or has not been authorized for use. For authorization details, refer to https://aka.ms/yamlauthz.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string RegexFailed(params object[] args)
        {
            const string Format = @"Regular expression failed evaluating '{0}' : {1}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SecureFileNotFound(params object[] args)
        {
            const string Format = @"A secure file with name {0} could not be found. The secure file does not exist or has not been authorized for use. For authorization details, refer to https://aka.ms/yamlauthz.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SecureFileNotFoundForInput(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} input {2} references secure file {3} which could not be found. The secure file does not exist or has not been authorized for use. For authorization details, refer to https://aka.ms/yamlauthz.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ServiceEndpointNotFound(params object[] args)
        {
            const string Format = @"A service connection with name {0} could not be found. The service connection does not exist or has not been authorized for use. For authorization details, refer to https://aka.ms/yamlauthz.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ServiceEndpointNotFoundForInput(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} input {2} references service connection {3} which could not be found. The service connection does not exist or has not been authorized for use. For authorization details, refer to https://aka.ms/yamlauthz.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StepConditionIsNotValid(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} specifies condition {2} which is not valid. Reason: {3}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StepInputEndpointAuthSchemeMismatch(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} input {2} expects a service connection of type {3} with authentication scheme {4} but the provided service connection {5} is of type {6} using authentication scheme {7}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StepInputEndpointTypeMismatch(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} input {2} expects a service connection of type {3} but the provided service connection {4} is of type {5}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StepNameInvalid(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} has an invalid name. Valid names may only contain alphanumeric characters and '_' and may not start with a number.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StepNamesMustBeUnique(params object[] args)
        {
            const string Format = @"Job {0}: The step name {1} appears more than once. Step names must be unique within a job.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StepNotSupported(params object[] args)
        {
            const string Format = @"Only task steps and group steps are supported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StepTaskInputInvalid(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} input '{2}' with value '{3}' does not satisfy '{4}': {5}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StepTaskReferenceInvalid(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} has an invalid task definition reference. A valid task definition reference must specify either an ID or a name and a version specification with a major version specified.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StepActionReferenceInvalid(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} has an invalid action definition reference. A valid action definition reference can be either a container resource or a repository resource.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TaskInvalidForGivenTarget(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} references task '{2}' at version '{3}' which is not valid for the given job target.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TaskMissing(params object[] args)
        {
            const string Format = @"A task is missing. The pipeline references a task called '{2}'. This usually indicates the task isn't installed, and you may be able to install it from the Marketplace: https://marketplace.visualstudio.com. (Task version {3}, job '{0}', step '{1}'.)";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TaskStepReferenceInvalid(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} task reference is invalid. {2}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ActionStepReferenceInvalid(params object[] args)
        {
            const string Format = @"Job {0}: Step {1} action reference is invalid. {2}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TaskTemplateNotSupported(params object[] args)
        {
            const string Format = @"Task template {0} at version {1} is not supported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TemplateStoreNotProvided(params object[] args)
        {
            const string Format = @"Unable to resolve task template {0} because no implementation was provided for {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnsupportedTargetType(params object[] args)
        {
            const string Format = @"Job {0}: Target {1} is not supported.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string RepositoryNotSpecified(params object[] args)
        {
            const string Format = @"The checkout step does not specify a repository";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string RepositoryResourceNotFound(params object[] args)
        {
            const string Format = @"The checkout step references the repository '{0}' which is not defined by the pipeline";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string VariableGroupNotFound(params object[] args)
        {
            const string Format = @"Variable group {0} was not found or is not authorized for use. For authorization details, refer to https://aka.ms/yamlauthz.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string VariableGroupNotFoundForPhase(params object[] args)
        {
            const string Format = @"Job {0}: Variable group {1} could not be found. The variable group does not exist or has not been authorized for use. For authorization details, refer to https://aka.ms/yamlauthz";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string VariableGroupNotFoundForStage(params object[] args)
        {
            const string Format = @"Stage {0}: Variable group {1} could not be found. The variable group does not exist or has not been authorized for use.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string JobNameWhenNoNameIsProvided(params object[] args)
        {
            const string Format = @"Job{0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StageNameWhenNoNameIsProvided(params object[] args)
        {
            const string Format = @"Stage{0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidAbsoluteRollingValue(params object[] args)
        {
            const string Format = @"Absolute rolling value should be greater than zero.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidPercentageRollingValue(params object[] args)
        {
            const string Format = @"Percentage rolling value should be with in 1 to 100.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidRollingOption(params object[] args)
        {
            const string Format = @"{0} is not supported as rolling option.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string EnvironmentNotFound(params object[] args)
        {
            const string Format = @"Job {0}: Environment {1} could not be found. The environment does not exist or has not been authorized for use.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string EnvironmentRequired(params object[] args)
        {
            const string Format = @"Job {0}: Environment is required.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string EnvironmentResourceNotFound(params object[] args)
        {
            const string Format = @"Job {0}: Resource {1} does not exist in environment {2}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StageNamesMustBeUnique(params object[] args)
        {
            const string Format = @"The stage name {0} appears more than once. Stage names must be unique within a pipeline.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ServiceConnectionUsedInVariableGroupNotValid(params object[] args)
        {
            const string Format = @"Service connection : {0} used in variable group : {1} is not valid. Either service connection does not exist or has not been authorized for use. For authorization details, refer to https://aka.ms/yamlauthz.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
