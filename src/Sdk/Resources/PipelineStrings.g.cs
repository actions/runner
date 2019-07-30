using System.Globalization;

namespace GitHub.DistributedTask.Pipelines
{
    public static class PipelineStrings
    {

        public static string AmbiguousQueueSpecification(object arg0)
        {
            const string Format = @"The pool name {0} is ambiguous.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AmbiguousSecureFileSpecification(object arg0)
        {
            const string Format = @"The secure file name {0} is ambiguous.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AmbiguousServiceEndpointSpecification(object arg0)
        {
            const string Format = @"The service connection name {0} is ambiguous.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AmbiguousTaskSpecification(object arg0, object arg1)
        {
            const string Format = @"The task name {0} is ambiguous. Specify one of the following identifiers to resolve the ambiguity: {1}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string AmbiguousVariableGroupSpecification(object arg0)
        {
            const string Format = @"The variable group name {0} is ambiguous.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string AzureKeyVaultTaskName(object arg0)
        {
            const string Format = @"Download secrets: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ContainerResourceInvalidRegistryEndpointType(object arg0, object arg1, object arg2)
        {
            const string Format = @"Expected 'dockerregistry' service connection type for image registry referenced by {0}, but got {1} for service connection {2}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string ContainerResourceNotFound(object arg0)
        {
            const string Format = @"A container resource with name {0} could not be found. The container resource does not exist. If you intended to specify an image, use NAME:TAG or NAME@DIGEST. For example, ubuntu:latest";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ContainerEndpointNotFound(object arg0, object arg1)
        {
            const string Format = @"Container {0} references service connection {1} which does not exist or is not authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string CheckoutMultipleRepositoryNotSupported()
        {
            const string Format = @"Checkout of multiple repositories is not supported.";
            return Format;
        }

        public static string CheckoutStepRepositoryNotSupported(object arg0)
        {
            const string Format = @"Checkout of repository '{0}' is not supported. Only 'self' and 'none' are supported.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string CheckoutMustBeTheFirstStep()
        {
            const string Format = @"Checkout should be the first step in the job.";
            return Format;
        }

        public static string ExpressionInvalid(object arg0)
        {
            const string Format = @"'{0}' is not a valid expression. Expressions must be enclosed with '$[' and ']'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string DemandExpansionInvalid(object arg0, object arg1, object arg2)
        {
            const string Format = @"Demand '{0}' is not valid when '{1}' evaluates to '{2}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string PhaseGraphCycleDetected(object arg0, object arg1)
        {
            const string Format = @"Job {0} depends on job {1} which creates a cycle in the dependency graph.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string StageGraphCycleDetected(object arg0, object arg1)
        {
            const string Format = @"Stage {0} depends on stage {1} which creates a cycle in the dependency graph.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string StagePhaseGraphCycleDetected(object arg0, object arg1, object arg2)
        {
            const string Format = @"Stage {0} job {1} depends on job {2} which creates a cycle in the dependency graph.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string InvalidRegexOptions(object arg0, object arg1)
        {
            const string Format = @"Provider regex options '{0}' are invalid. Supported combination of flags: `{1}`. Eg: 'IgnoreCase, Multiline', 'Multiline'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string InvalidRetryStageNeverRun(object arg0)
        {
            const string Format = @"Unable to retry stage {0} because it has never been run.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidRetryStageNotComplete(object arg0)
        {
            const string Format = @"Unable to retry the pipeline because stage {0} is currently in progress.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidTypeForLengthFunction(object arg0)
        {
            const string Format = @"Kind '{0}' not supported. Only arrays, strings, dictionaries, or collections are supported for the length function.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidValidationOptionNoImplementation(object arg0, object arg1)
        {
            const string Format = @"The validation option {0} was specified but no implementation was provided for {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string PhaseDependencyNotFound(object arg0, object arg1)
        {
            const string Format = @"Job {0} depends on unknown job {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string StagePhaseDependencyNotFound(object arg0, object arg1, object arg2)
        {
            const string Format = @"Stage {0} job {1} depends on unknown job {2}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string StageDependencyNotFound(object arg0, object arg1)
        {
            const string Format = @"Stage {0} depends on unknown stage {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string PhaseJobNameInvalidForSlicing(object arg0)
        {
            const string Format = @"The job name {0} is not valid for the specified execution options. Valid jobs names include JobN or N, where N is a value from 1 to maximum parallelism.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string PhaseJobNumberDoesNotExist(object arg0, object arg1, object arg2)
        {
            const string Format = @"Job {0} uses a maximum parallelism of {1}. The job {2} does not exist with the specified parallelism settings.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string PhaseJobMatrixExpansionExceedLimit(object arg0, object arg1)
        {
            const string Format = @"The matrix expansion resulted in {0} jobs which exceeds the maximum allowable job count of {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string PhaseJobSlicingExpansionExceedLimit(object arg0, object arg1)
        {
            const string Format = @"The slicing expansion resulted in {0} jobs which exceeds the maximum allowable job count of {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string PhaseMatrixConfigurationDoesNotExist(object arg0, object arg1)
        {
            const string Format = @"Job {0} does not specify a matrix configuration named {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string PhaseNameInvalid(object arg0)
        {
            const string Format = @"Job {0} has an invalid name. Valid names may only contain alphanumeric characters and '_' and may not start with a number.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string StageNameInvalid(object arg0)
        {
            const string Format = @"Stage {0} has an invalid name. Valid names may only contain alphanumeric characters and '_' and may not start with a number.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string StagePhaseNameInvalid(object arg0, object arg1)
        {
            const string Format = @"Stage {0} job {1} has an invalid name. Valid names may only contain alphanumeric characters and '_' and may not start with a number.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string PhaseNamesMustBeUnique(object arg0)
        {
            const string Format = @"The job name {0} appears more than once. Job names must be unique within a pipeline.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string StagePhaseNamesMustBeUnique(object arg0, object arg1)
        {
            const string Format = @"Stage {0} job {1} appears more than once. Job names must be unique within a stage.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string PhaseTargetRequired(object arg0)
        {
            const string Format = @"Job {0}: Target is required.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string StageVariableGroupNotSupported(object arg0, object arg1)
        {
            const string Format = @"Stage {0}: Variable group reference {1} is not supported.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string PhaseVariableGroupNotSupported(object arg0, object arg1)
        {
            const string Format = @"Job {0}: Variable group reference {1} is not supported.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string PipelineNotValid()
        {
            const string Format = @"The pipeline is not valid.";
            return Format;
        }

        public static string PipelineNotValidWithErrors(object arg0)
        {
            const string Format = @"The pipeline is not valid. {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string PipelineNotValidNoStartingPhase()
        {
            const string Format = @"The pipeline must contain at least one job with no dependencies.";
            return Format;
        }

        public static string PipelineNotValidNoStartingStage()
        {
            const string Format = @"The pipeline must contain at least one stage with no dependencies.";
            return Format;
        }

        public static string StageNotValidNoStartingPhase(object arg0)
        {
            const string Format = @"Stage {0} must contain at least one job with no dependencies.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string QueueNotDefined()
        {
            const string Format = @"Either a pool ID or name is required.";
            return Format;
        }

        public static string QueueNotFound(object arg0)
        {
            const string Format = @"Could not find a pool with ID {0}. The pool does not exist or has not been authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string QueueNotFoundByName(object arg0)
        {
            const string Format = @"Could not find a pool with name {0}. The pool does not exist or has not been authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string RegexFailed(object arg0, object arg1)
        {
            const string Format = @"Regular expression failed evaluating '{0}' : {1}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string SecureFileNotFound(object arg0)
        {
            const string Format = @"A secure file with name {0} could not be found. The secure file does not exist or has not been authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string SecureFileNotFoundForInput(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"Job {0}: Step {1} input {2} references secure file {3} which could not be found. The secure file does not exist or has not been authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string ServiceEndpointNotFound(object arg0)
        {
            const string Format = @"A service connection with name {0} could not be found. The service connection does not exist or has not been authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ServiceEndpointNotFoundForInput(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"Job {0}: Step {1} input {2} references service connection {3} which could not be found. The service connection does not exist or has not been authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string StepConditionIsNotValid(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"Job {0}: Step {1} specifies condition {2} which is not valid. Reason: {3}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string StepInputEndpointAuthSchemeMismatch(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
        {
            const string Format = @"Job {0}: Step {1} input {2} expects a service connection of type {3} with authentication scheme {4} but the provided service connection {5} is of type {6} using authentication scheme {7}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public static string StepInputEndpointTypeMismatch(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            const string Format = @"Job {0}: Step {1} input {2} expects a service connection of type {3} but the provided service connection {4} is of type {5}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        public static string StepNameInvalid(object arg0, object arg1)
        {
            const string Format = @"Job {0}: Step {1} has an invalid name. Valid names may only contain alphanumeric characters and '_' and may not start with a number.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string StepNamesMustBeUnique(object arg0, object arg1)
        {
            const string Format = @"Job {0}: The step name {1} appears more than once. Step names must be unique within a job.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string StepNotSupported()
        {
            const string Format = @"Only task steps and group steps are supported.";
            return Format;
        }

        public static string StepTaskInputInvalid(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            const string Format = @"Job {0}: Step {1} input '{2}' with value '{3}' does not satisfy '{4}': {5}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        public static string StepTaskReferenceInvalid(object arg0, object arg1)
        {
            const string Format = @"Job {0}: Step {1} has an invalid task definition reference. A valid task definition reference must specify either an ID or a name and a version specification with a major version specified.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string StepActionReferenceInvalid(object arg0, object arg1)
        {
            const string Format = @"Job {0}: Step {1} has an invalid action definition reference. A valid action definition reference can be either a container resource or a repository resource.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string TaskInvalidForGivenTarget(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"Job {0}: Step {1} references task '{2}' at version '{3}' which is not valid for the given job target.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string TaskMissing(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"A task is missing. The pipeline references a task called '{2}'. This usually indicates the task isn't installed, and you may be able to install it from the Marketplace: https://marketplace.visualstudio.com. (Task version {3}, job '{0}', step '{1}'.)";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string TaskStepReferenceInvalid(object arg0, object arg1, object arg2)
        {
            const string Format = @"Job {0}: Step {1} task reference is invalid. {2}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string ActionStepReferenceInvalid(object arg0, object arg1, object arg2)
        {
            const string Format = @"Job {0}: Step {1} action reference is invalid. {2}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string TaskTemplateNotSupported(object arg0, object arg1)
        {
            const string Format = @"Task template {0} at version {1} is not supported.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string TemplateStoreNotProvided(object arg0, object arg1)
        {
            const string Format = @"Unable to resolve task template {0} because no implementation was provided for {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string UnsupportedTargetType(object arg0, object arg1)
        {
            const string Format = @"Job {0}: Target {1} is not supported.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string RepositoryNotSpecified()
        {
            const string Format = @"The checkout step does not specify a repository";
            return Format;
        }

        public static string RepositoryResourceNotFound(object arg0)
        {
            const string Format = @"The checkout step references the repository '{0}' which is not defined by the pipeline";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string VariableGroupNotFound(object arg0)
        {
            const string Format = @"Variable group {0} was not found or is not authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string VariableGroupNotFoundForPhase(object arg0, object arg1)
        {
            const string Format = @"Job {0}: Variable group {1} could not be found. The variable group does not exist or has not been authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string VariableGroupNotFoundForStage(object arg0, object arg1)
        {
            const string Format = @"Stage {0}: Variable group {1} could not be found. The variable group does not exist or has not been authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string JobNameWhenNoNameIsProvided(object arg0)
        {
            const string Format = @"Job{0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string StageNameWhenNoNameIsProvided(object arg0)
        {
            const string Format = @"Stage{0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidAbsoluteRollingValue()
        {
            const string Format = @"Absolute rolling value should be greater than zero.";
            return Format;
        }

        public static string InvalidPercentageRollingValue()
        {
            const string Format = @"Percentage rolling value should be with in 1 to 100.";
            return Format;
        }

        public static string InvalidRollingOption(object arg0)
        {
            const string Format = @"{0} is not supported as rolling option.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string EnvironmentNotFound(object arg0, object arg1)
        {
            const string Format = @"Job {0}: Environment {1} could not be found. The environment does not exist or has not been authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string EnvironmentRequired(object arg0)
        {
            const string Format = @"Job {0}: Environment is required.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string EnvironmentResourceNotFound(object arg0, object arg1, object arg2)
        {
            const string Format = @"Job {0}: Resource {1} does not exist in environment {2}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string StageNamesMustBeUnique(object arg0)
        {
            const string Format = @"The stage name {0} appears more than once. Stage names must be unique within a pipeline.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ServiceConnectionUsedInVariableGroupNotValid(object arg0, object arg1)
        {
            const string Format = @"Service connection : {0} used in variable group : {1} is not valid. Either service connection does not exist or has not been authorized for use.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }
    }
}
