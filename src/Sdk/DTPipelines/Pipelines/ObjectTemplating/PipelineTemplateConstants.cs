using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PipelineTemplateConstants
    {
        public const String Always = "always";
        public const String BooleanStepsContext = "boolean-steps-context";
        public const String BooleanStrategyContext = "boolean-strategy-context";
        public const String CancelTimeoutMinutes = "cancel-timeout-minutes";
        public const String Cancelled = "cancelled";
        public const String Clean= "clean";
        public const String Container = "container";
        public const String ContinueOnError = "continue-on-error";
        public const String Credentials = "credentials";
        public const String Defaults = "defaults";
        public const String Env = "env";
        public const String Environment = "environment";
        public const String Event = "event";
        public const String EventPattern = "github.event";
        public const String Exclude = "exclude";
        public const String FailFast = "fail-fast";
        public const String Failure = "failure";
        public const String FetchDepth = "fetch-depth";
        public const String GitHub = "github";
        public const String HashFiles = "hashFiles";
        public const String Id = "id";
        public const String If = "if";
        public const String Image = "image";
        public const String Include = "include";
        public const String Inputs = "inputs";
        public const String Job = "job";
        public const String JobDefaultsRun = "job-defaults-run";
        public const String JobIfResult = "job-if-result";
        public const String JobOutputs = "job-outputs";
        public const String Jobs = "jobs";
        public const String Lfs = "lfs";
        public const String Matrix = "matrix";
        public const String MaxParallel = "max-parallel";
        public const String Name = "name";
        public const String Needs = "needs";
        public const String NumberStepsContext = "number-steps-context";
        public const String NumberStrategyContext = "number-strategy-context";
        public const String On = "on";
        public const String Options = "options";
        public const String Outputs = "outputs";
        public const String OutputsPattern = "needs.*.outputs";
        public const String Password = "password";
        public const String Path = "path";
        public const String Pool = "pool";
        public const String Ports = "ports";
        public const String Result = "result";
        public const String Run = "run";
        public const String RunDisplayPrefix = "Run ";
        public const String Runner = "runner";
        public const String RunsOn = "runs-on";
        public const String Secrets = "secrets";
        public const String Services = "services";
        public const String Shell = "shell";
        public const String Skipped = "skipped";
        public const String StepEnv = "step-env";
        public const String StepIfResult = "step-if-result";
        public const String StepWith = "step-with";
        public const String Steps = "steps";
        public const String Strategy = "strategy";
        public const String StringStepsContext = "string-steps-context";
        public const String StringStrategyContext = "string-strategy-context";
        public const String Submodules = "submodules";
        public const String Success = "success";
        public const String Template = "template";
        public const String TimeoutMinutes = "timeout-minutes";
        public const String Username = "username";
        public const String Uses = "uses";
        public const String VmImage = "vmImage";
        public const String Volumes = "volumes";
        public const String With = "with";
        public const String Workflow = "workflow";
        public const String Workflow_1_0 = "workflow-v1.0";
        public const String WorkflowRoot = "workflow-root";
        public const String WorkingDirectory = "working-directory";
        public const String Workspace = "workspace";
    }
}
