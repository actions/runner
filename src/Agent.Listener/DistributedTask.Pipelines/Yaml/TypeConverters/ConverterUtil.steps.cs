using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.Contracts;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml.TypeConverters
{
    internal static partial class ConverterUtil
    {
        internal static IList<IStep> ReadSteps(IParser parser, Boolean simpleOnly = false)
        {
            var result = new List<IStep>();
            parser.Expect<SequenceStart>();
            while (parser.Allow<SequenceEnd>() == null)
            {
                result.Add(ReadStep(parser, simpleOnly));
            }

            return result;
        }

        internal static IStep ReadStep(IParser parser, Boolean simpleOnly = false)
        {
            IStep result;
            parser.Expect<MappingStart>();
            var scalar = parser.Expect<Scalar>();
            if (String.Equals(scalar.Value, YamlConstants.Task, StringComparison.Ordinal))
            {
                var task = new TaskStep { Enabled = true };
                scalar = parser.Expect<Scalar>();
                String[] refComponents = (scalar.Value ?? String.Empty).Split('@');
                Int32 version;
                if (refComponents.Length != 2 ||
                    String.IsNullOrEmpty(refComponents[0]) ||
                    String.IsNullOrEmpty(refComponents[1]) ||
                    !Int32.TryParse(refComponents[1], NumberStyles.None, CultureInfo.InvariantCulture, out version))
                {
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Task reference must be in the format <NAME>@<VERSION>. For example MyTask@2. The following task reference format is invalid: '{scalar.Value}'");
                }

                task.Reference = new TaskReference
                {
                    Name = refComponents[0],
                    Version = refComponents[1],
                };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case YamlConstants.Inputs:
                            task.Inputs = ReadMappingOfStringString(parser, StringComparer.OrdinalIgnoreCase);
                            break;
                        default:
                            SetTaskControlProperty(parser, task, scalar);
                            break;
                    }
                }

                result = task;
            }
            else if (String.Equals(scalar.Value, YamlConstants.Script, StringComparison.Ordinal))
            {
                var task = new TaskStep
                {
                    Enabled = true,
                    Reference = new TaskReference
                    {
                        Name = "CmdLine",
                        Version = "2",
                    },
                    Inputs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase),
                };

                task.Inputs["script"] = parser.Expect<Scalar>().Value ?? String.Empty;
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case YamlConstants.FailOnStderr:
                            task.Inputs["failOnStderr"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        case YamlConstants.WorkingDirectory:
                            task.Inputs["workingDirectory"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        default:
                            SetTaskControlProperty(parser, task, scalar);
                            break;
                    }
                }

                result = task;
            }
            else if (String.Equals(scalar.Value, YamlConstants.Bash, StringComparison.Ordinal))
            {
                var task = new TaskStep
                {
                    Enabled = true,
                    Reference = new TaskReference
                    {
                        Name = "Bash",
                        Version = "3",
                    },
                    Inputs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase),
                };

                task.Inputs["targetType"] = "inline";
                task.Inputs["script"] = parser.Expect<Scalar>().Value ?? String.Empty;
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case YamlConstants.FailOnStderr:
                            task.Inputs["failOnStderr"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        case YamlConstants.WorkingDirectory:
                            task.Inputs["workingDirectory"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        default:
                            SetTaskControlProperty(parser, task, scalar);
                            break;
                    }
                }

                result = task;
            }
            else if (String.Equals(scalar.Value, YamlConstants.PowerShell, StringComparison.Ordinal))
            {
                var task = new TaskStep
                {
                    Enabled = true,
                    Reference = new TaskReference
                    {
                        Name = "PowerShell",
                        Version = "2",
                    },
                    Inputs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase),
                };

                task.Inputs["targetType"] = "inline";
                task.Inputs["script"] = parser.Expect<Scalar>().Value ?? String.Empty;
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case YamlConstants.ErrorActionPreference:
                            task.Inputs["errorActionPreference"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        case YamlConstants.FailOnStderr:
                            task.Inputs["failOnStderr"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        case YamlConstants.IgnoreLASTEXITCODE:
                            task.Inputs["ignoreLASTEXITCODE"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        case YamlConstants.WorkingDirectory:
                            task.Inputs["workingDirectory"] = parser.Expect<Scalar>().Value ?? String.Empty;
                            break;
                        default:
                            SetTaskControlProperty(parser, task, scalar);
                            break;
                    }
                }

                result = task;
            }
            else if (String.Equals(scalar.Value, YamlConstants.Checkout, StringComparison.Ordinal))
            {
                var checkoutStep = new CheckoutStep();
                scalar = parser.Expect<Scalar>();
                checkoutStep.Name = scalar.Value ?? String.Empty;
                if (String.Equals(checkoutStep.Name, YamlConstants.Self, StringComparison.Ordinal))
                {
                    while (parser.Allow<MappingEnd>() == null)
                    {
                        scalar = parser.Expect<Scalar>();
                        switch (scalar.Value ?? String.Empty)
                        {
                            case YamlConstants.Clean:
                                checkoutStep.Clean = ReadNonEmptyString(parser);
                                break;

                            case YamlConstants.FetchDepth:
                                checkoutStep.FetchDepth = ReadNonEmptyString(parser);
                                break;

                            case YamlConstants.Lfs:
                                checkoutStep.Lfs = ReadNonEmptyString(parser);
                                break;
                        }
                    }
                }
                else if (String.Equals(checkoutStep.Name, YamlConstants.None, StringComparison.Ordinal))
                {
                    parser.Expect<MappingEnd>();
                }
                else
                {
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected resource name '{scalar.Value}'. The '{YamlConstants.Checkout}' step currently can only be used with the resource name '{YamlConstants.Self}' or '{YamlConstants.None}'.");
                }

                result = checkoutStep;
            }
            else if (String.Equals(scalar.Value, YamlConstants.Group, StringComparison.Ordinal))
            {
                if (simpleOnly)
                {
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"A step '{YamlConstants.Group}' cannot be nested within a step group or steps template.");
                }

                var stepGroup = new StepGroup() { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    if (String.Equals(scalar.Value, YamlConstants.Steps, StringComparison.Ordinal))
                    {
                        stepGroup.Steps = ReadSteps(parser, simpleOnly: true).Cast<ISimpleStep>().ToList();
                    }
                    else
                    {
                        throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
                    }
                }

                result = stepGroup;
            }
            else if (String.Equals(scalar.Value, YamlConstants.Template, StringComparison.Ordinal))
            {
                if (simpleOnly)
                {
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Steps '{YamlConstants.Template}' cannot be nested within a step group or steps template.");
                }

                var templateReference = new StepsTemplateReference { Name = ReadNonEmptyString(parser) };
                while (parser.Allow<MappingEnd>() == null)
                {
                    scalar = parser.Expect<Scalar>();
                    switch (scalar.Value ?? String.Empty)
                    {
                        case YamlConstants.Parameters:
                            templateReference.Parameters = ReadMapping(parser);
                            break;

                        case YamlConstants.Steps:
                            templateReference.StepOverrides = ReadStepOverrides(parser);
                            break;

                        default:
                            throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
                    }
                }

                result = templateReference;
            }
            else
            {
                throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unknown step type: '{scalar.Value}'");
            }

            return result;
        }

        internal static IDictionary<String, IList<ISimpleStep>> ReadStepOverrides(IParser parser)
        {
            var result = new Dictionary<String, IList<ISimpleStep>>();
            parser.Expect<MappingStart>();
            while (parser.Allow<MappingEnd>() == null)
            {
                String key = ReadNonEmptyString(parser);
                result[key] = ReadSteps(parser, simpleOnly: true).Cast<ISimpleStep>().ToList();
            }

            return result;
        }

        internal static void SetProperty(IParser parser, StepsTemplateReference reference, Scalar scalar)
        {
            switch (scalar.Value ?? String.Empty)
            {
                case YamlConstants.Parameters:
                    reference.Parameters = ReadMapping(parser);
                    break;

                case YamlConstants.Steps:
                    reference.StepOverrides = ReadStepOverrides(parser);
                    break;

                default:
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property: '{scalar.Value}'");
            }
        }

        internal static void SetTaskControlProperty(IParser parser, TaskStep task, Scalar scalar)
        {
            switch (scalar.Value ?? String.Empty)
            {
                case YamlConstants.Condition:
                    task.Condition = parser.Expect<Scalar>().Value;
                    break;
                case YamlConstants.ContinueOnError:
                    task.ContinueOnError = ReadBoolean(parser);
                    break;
                case YamlConstants.Enabled:
                    task.Enabled = ReadBoolean(parser);
                    break;
                case YamlConstants.Environment:
                    task.Environment = ReadMappingOfStringString(parser, StringComparer.Ordinal);
                    break;
                case YamlConstants.Name:
                    task.Name = parser.Expect<Scalar>().Value;
                    break;
                case YamlConstants.TimeoutInMinutes:
                    task.TimeoutInMinutes = ReadInt32(parser);
                    break;
                default:
                    throw new SyntaxErrorException(scalar.Start, scalar.End, $"Unexpected property {scalar.Value}");
            }
        }

        internal static void WriteSteps(IEmitter emitter, IList<IStep> steps)
        {
            emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
            foreach (IStep step in steps)
            {
                WriteStep(emitter, step);
            }

            emitter.Emit(new SequenceEnd());
        }

        internal static void WriteStep(IEmitter emitter, IStep step, Boolean noBootstrap = false)
        {
            if (!noBootstrap)
            {
                emitter.Emit(new MappingStart());
            }

            if (step is StepsTemplateReference)
            {
                var reference = step as StepsTemplateReference;
                if (!noBootstrap)
                {
                    emitter.Emit(new Scalar(YamlConstants.Template));
                    emitter.Emit(new Scalar(reference.Name));

                    if (reference.Parameters != null && reference.Parameters.Count > 0)
                    {
                        emitter.Emit(new Scalar(YamlConstants.Parameters));
                        WriteMapping(emitter, reference.Parameters);
                    }
                }

                if (reference.StepOverrides != null && reference.StepOverrides.Count > 0)
                {
                    emitter.Emit(new Scalar(YamlConstants.Steps));
                    WriteStepOverrides(emitter, reference.StepOverrides);
                }
            }
            else if (step is StepGroup)
            {
                var group = step as StepGroup;
                emitter.Emit(new Scalar(YamlConstants.Group));
                emitter.Emit(new Scalar(group.Name));
                if (group.Steps != null && group.Steps.Count > 0)
                {
                    emitter.Emit(new Scalar(YamlConstants.Steps));
                    WriteSteps(emitter, group.Steps.Cast<IStep>().ToList());
                }
            }
            else if (step is TaskStep)
            {
                var task = step as TaskStep;
                if (String.Equals(task.Reference.Name, "CmdLine", StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(task.Reference.Version, "2", StringComparison.Ordinal) &&
                    task.Inputs != null)
                {
                    emitter.Emit(new Scalar(YamlConstants.Script));
                    String script;
                    task.Inputs.TryGetValue("script", out script);
                    emitter.Emit(new Scalar(script ?? String.Empty));
                    WriteTaskPreInputProperties(emitter, task);

                    String failOnStderr;
                    if (task.Inputs.TryGetValue("failOnStderr", out failOnStderr)
                        && !String.IsNullOrEmpty(failOnStderr))
                    {
                        emitter.Emit(new Scalar(YamlConstants.FailOnStderr));
                        emitter.Emit(new Scalar(failOnStderr));
                    }

                    String workingDirectory;
                    if (task.Inputs.TryGetValue("workingDirectory", out workingDirectory) &&
                        !String.IsNullOrEmpty(workingDirectory))
                    {
                        emitter.Emit(new Scalar(YamlConstants.WorkingDirectory));
                        emitter.Emit(new Scalar(workingDirectory));
                    }

                    WriteTaskPostInputProperties(emitter, task);
                }
                else if (String.Equals(task.Reference.Name, "Bash", StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(task.Reference.Version, "3", StringComparison.Ordinal) &&
                    task.Inputs != null &&
                    task.Inputs.ContainsKey("targetType") &&
                    String.Equals(task.Inputs["targetType"], "inline", StringComparison.OrdinalIgnoreCase))
                {
                    emitter.Emit(new Scalar(YamlConstants.Bash));
                    String script;
                    task.Inputs.TryGetValue("script", out script);
                    emitter.Emit(new Scalar(script ?? String.Empty));
                    WriteTaskPreInputProperties(emitter, task);

                    String failOnStderr;
                    if (task.Inputs.TryGetValue("failOnStderr", out failOnStderr)
                        && !String.IsNullOrEmpty(failOnStderr))
                    {
                        emitter.Emit(new Scalar(YamlConstants.FailOnStderr));
                        emitter.Emit(new Scalar(failOnStderr));
                    }

                    String workingDirectory;
                    if (task.Inputs.TryGetValue("workingDirectory", out workingDirectory) &&
                        !String.IsNullOrEmpty(workingDirectory))
                    {
                        emitter.Emit(new Scalar(YamlConstants.WorkingDirectory));
                        emitter.Emit(new Scalar(workingDirectory));
                    }

                    WriteTaskPostInputProperties(emitter, task);
                }
                else if (String.Equals(task.Reference.Name, "PowerShell", StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(task.Reference.Version, "2", StringComparison.Ordinal) &&
                    task.Inputs != null &&
                    task.Inputs.ContainsKey("targetType") &&
                    String.Equals(task.Inputs["targetType"], "inline", StringComparison.OrdinalIgnoreCase))
                {
                    emitter.Emit(new Scalar(YamlConstants.PowerShell));
                    String script;
                    task.Inputs.TryGetValue("script", out script);
                    emitter.Emit(new Scalar(script ?? String.Empty));
                    WriteTaskPreInputProperties(emitter, task);

                    String errorActionPreference;
                    if (task.Inputs.TryGetValue("errorActionPreference", out errorActionPreference)
                        && !String.IsNullOrEmpty(errorActionPreference))
                    {
                        emitter.Emit(new Scalar(YamlConstants.ErrorActionPreference));
                        emitter.Emit(new Scalar(errorActionPreference));
                    }

                    String failOnStderr;
                    if (task.Inputs.TryGetValue("failOnStderr", out failOnStderr)
                        && !String.IsNullOrEmpty(failOnStderr))
                    {
                        emitter.Emit(new Scalar(YamlConstants.FailOnStderr));
                        emitter.Emit(new Scalar(failOnStderr));
                    }

                    String ignoreLASTEXITCODE;
                    if (task.Inputs.TryGetValue("ignoreLASTEXITCODE", out ignoreLASTEXITCODE)
                        && !String.IsNullOrEmpty(ignoreLASTEXITCODE))
                    {
                        emitter.Emit(new Scalar(YamlConstants.IgnoreLASTEXITCODE));
                        emitter.Emit(new Scalar(ignoreLASTEXITCODE));
                    }

                    String workingDirectory;
                    if (task.Inputs.TryGetValue("workingDirectory", out workingDirectory) &&
                        !String.IsNullOrEmpty(workingDirectory))
                    {
                        emitter.Emit(new Scalar(YamlConstants.WorkingDirectory));
                        emitter.Emit(new Scalar(workingDirectory));
                    }

                    WriteTaskPostInputProperties(emitter, task);
                }
                else
                {
                    emitter.Emit(new Scalar(YamlConstants.Task));
                    if (String.IsNullOrEmpty(task.Reference.Version))
                    {
                        emitter.Emit(new Scalar(task.Reference.Name));
                    }
                    else
                    {
                        emitter.Emit(new Scalar($"{task.Reference.Name}@{task.Reference.Version}"));
                    }

                    WriteTaskPreInputProperties(emitter, task);
                    if (task.Inputs != null && task.Inputs.Count > 0)
                    {
                        emitter.Emit(new Scalar(YamlConstants.Inputs));
                        WriteMapping(emitter, task.Inputs);
                    }

                    WriteTaskPostInputProperties(emitter, task);
                }
            }
            else if (step is CheckoutStep)
            {
                var checkoutStep = step as CheckoutStep;
                emitter.Emit(new Scalar(YamlConstants.Checkout));
                if (String.Equals(checkoutStep.Name, YamlConstants.None, StringComparison.OrdinalIgnoreCase))
                {
                    emitter.Emit(new Scalar(YamlConstants.None));
                }
                else if (String.Equals(checkoutStep.Name, YamlConstants.Self, StringComparison.OrdinalIgnoreCase))
                {
                    emitter.Emit(new Scalar(YamlConstants.Self));
                    if (!String.IsNullOrEmpty(checkoutStep.Clean))
                    {
                        emitter.Emit(new Scalar(YamlConstants.Clean));
                        emitter.Emit(new Scalar(checkoutStep.Clean));
                    }

                    if (!String.IsNullOrEmpty(checkoutStep.FetchDepth))
                    {
                        emitter.Emit(new Scalar(YamlConstants.FetchDepth));
                        emitter.Emit(new Scalar(checkoutStep.FetchDepth));
                    }

                    if (!String.IsNullOrEmpty(checkoutStep.Lfs))
                    {
                        emitter.Emit(new Scalar(YamlConstants.Lfs));
                        emitter.Emit(new Scalar(checkoutStep.Lfs));
                    }
                }
                else
                {
                    // Should not reach here.
                    throw new NotSupportedException($"Unexpected checkout step resource name: '{checkoutStep.Name}'");
                }
            }

            if (!noBootstrap)
            {
                emitter.Emit(new MappingEnd());
            }
        }

        internal static void WriteStepOverrides(IEmitter emitter, IDictionary<String, IList<ISimpleStep>> overrides)
        {
            emitter.Emit(new MappingStart());
            foreach (KeyValuePair<String, IList<ISimpleStep>> pair in overrides)
            {
                emitter.Emit(new Scalar(pair.Key));
                WriteSteps(emitter, pair.Value.Cast<IStep>().ToList());
            }

            emitter.Emit(new MappingEnd());
        }

        internal static void WriteStepsTemplate(IEmitter emitter, StepsTemplate template, Boolean noBootstrapper = false)
        {
            if (!noBootstrapper)
            {
                emitter.Emit(new MappingStart());
            }

            if (template.Steps != null && template.Steps.Count > 0)
            {
                emitter.Emit(new Scalar(YamlConstants.Steps));
                WriteSteps(emitter, template.Steps);
            }

            if (!noBootstrapper)
            {
                emitter.Emit(new MappingEnd());
            }
        }

        private static void WriteTaskPreInputProperties(IEmitter emitter, TaskStep task)
        {
            if (!String.IsNullOrEmpty(task.Name))
            {
                emitter.Emit(new Scalar(YamlConstants.Name));
                emitter.Emit(new Scalar(task.Name));
            }

            if (!task.Enabled)
            {
                emitter.Emit(new Scalar(YamlConstants.Enabled));
                emitter.Emit(new Scalar("false"));
            }

            if (!String.IsNullOrEmpty(task.Condition))
            {
                emitter.Emit(new Scalar(YamlConstants.Condition));
                emitter.Emit(new Scalar(task.Condition));
            }

            if (task.ContinueOnError)
            {
                emitter.Emit(new Scalar(YamlConstants.ContinueOnError));
                emitter.Emit(new Scalar("true"));
            }

            if (task.TimeoutInMinutes > 0)
            {
                emitter.Emit(new Scalar(YamlConstants.TimeoutInMinutes));
                emitter.Emit(new Scalar(String.Format(CultureInfo.InvariantCulture, "{0}", task.TimeoutInMinutes)));
            }
        }

        private static void WriteTaskPostInputProperties(IEmitter emitter, TaskStep task)
        {
            if (task.Environment != null && task.Environment.Count > 0)
            {
                emitter.Emit(new Scalar(YamlConstants.Environment));
                WriteMapping(emitter, task.Environment);
            }
        }
    }
}
