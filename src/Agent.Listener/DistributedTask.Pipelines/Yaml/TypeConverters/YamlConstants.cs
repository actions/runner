using System;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.TypeConverters
{
    internal static class YamlConstants
    {
        internal const String Bash = "bash";
        internal const String Checkout = "checkout";
        internal const String Clean = "clean";
        internal const String Condition = "condition";
        internal const String ContinueOnError = "continueOnError";
        internal const String Demands = "demands";
        internal const String DependsOn = "dependsOn";
        internal const String Deployment = "deployment";
        internal const String EnableAccessToken = "enableAccessToken";
        internal const String Enabled = "enabled";
        internal const String Endpoint = "endpoint";
        internal const String ErrorActionPreference = "errorActionPreference";
        internal const String Environment = "env";
        internal const String FailOnStderr = "failOnStderr";
        internal const String FetchDepth = "fetchDepth";
        internal const String Group = "group";
        internal const String HealthOption = "healthOption";
        internal const String IgnoreLASTEXITCODE = "ignoreLASTEXITCODE";
        internal const String Inputs = "inputs";
        internal const String Lfs = "lfs";
        internal const String Matrix = "matrix";
        internal const String Name = "name";
        internal const String None = "none";
        internal const String Parallel = "parallel";
        internal const String Parameters = "parameters";
        internal const String Percentage = "percentage";
        internal const String Phases = "phases";
        internal const String PowerShell = "powershell";
        internal const String Queue = "queue";
        internal const String Repo = "repo";
        internal const String Resources = "resources";
        internal const String Script = "script";
        internal const String Self = "self";
        internal const String Server = "server";
        internal const String Steps = "steps";
        internal const String Tags = "tags";
        internal const String Task = "task";
        internal const String Template = "template";
        internal const String TimeoutInMinutes = "timeoutInMinutes";
        internal const String Type = "type";
        internal const String Value = "value";
        internal const String Variables = "variables";
        internal const String WorkingDirectory = "workingDirectory";
    }
}