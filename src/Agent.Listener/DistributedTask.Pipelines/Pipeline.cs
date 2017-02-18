// This source file is maintained in two repos. Edits must be made to both copies.
// Unit tests live in the vsts-agent repo on GitHub.
//
// Repo 1) VSO repo under DistributedTask/Sdk/Server/Pipelines
// Repo 2) vsts-agent repo on GitHub under src/Agent.Listener/DistributedTask.Pipelines
//
// The style of this source file aims to follow VSO/DistributedTask conventions.

using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines
{
    ////////////////////////////////////////
    // Process classes
    ////////////////////////////////////////

    public sealed class Process : Phase
    {
        public List<ProcessResource> Resources { get; set; }

        public ProcessTemplateReference Template { get; set; }

        public List<IPhase> Phases { get; set; }
    }

    public sealed class ProcessResource
    {
        public String Name { get; set; }

        public String Type { get; set; }

        public IDictionary<String, Object> Data { get; set; }
    }

    public sealed class ProcessTemplateReference : PhasesTemplateReference
    {
    }

    // A process template cannot reference other process templates, but
    // phases/jobs/steps within can reference templates.
    public sealed class ProcessTemplate : PhasesTemplate
    {
        public List<ProcessResource> Resources { get; set; }
    }

    ////////////////////////////////////////
    // Phase classes
    ////////////////////////////////////////

    public interface IPhase
    {
    }

    public class Phase : Job, IPhase
    {
        public Boolean? Parallel { get; set; }

        public PhaseTarget Target { get; set; }

        public List<IJob> Jobs { get; set; }
    }

    public sealed class PhaseTarget
    {
        public String Type { get; set; }

        public String Name { get; set; }
    }

    public class PhasesTemplateReference : JobsTemplateReference, IPhase
    {
        public List<PhaseSelector> PhaseSelectors { get; set; }
    }

    public sealed class PhaseSelector : JobSelector
    {
        public List<JobSelector> JobSelectors { get; set; }
    }

    // A phase template cannot reference other phase templates, but
    // jobs/steps within can reference templates.
    public class PhasesTemplate : JobsTemplate
    {
        public List<IPhase> Phases { get; set; }
    }

    ////////////////////////////////////////
    // Job classes
    ////////////////////////////////////////

    public interface IJob
    {
    }

    public class Job : IJob
    {
        public String Name { get; set; }

        public Int32? TimeoutInMinutes { get; set; }

        public List<IVariable> Variables { get; set; }

        public List<IStep> Steps { get; set; }
    }

    public interface IVariable
    {
    }

    public sealed class Variable : IVariable
    {
        public String Name { get; set; }

        public String Value { get; set; }

        public Boolean Verbatim { get; set; }
    }

    public class JobsTemplateReference : StepsTemplateReference, IJob
    {
        public List<JobSelector> JobSelectors { get; set; }
    }

    public class JobSelector
    {
        public String Name { get; set; }

        public Dictionary<String, List<ISimpleStep>> StepOverrides { get; set; }
    }

    // A job template cannot reference other job templates, but
    // steps within can reference templates.
    public class JobsTemplate : StepsTemplate
    {
        public List<IJob> Jobs { get; set; }
    }

    public sealed class VariablesTemplateReference : IVariable
    {
        public String Name { get; set; }

        public IDictionary<String, Object> Parameters { get; set; }
    }

    public sealed class VariablesTemplate
    {
        public List<IVariable> Variables { get; set; }
    }

    ////////////////////////////////////////
    // Step classes
    ////////////////////////////////////////

    public interface IStep
    {
        String Name { get; set; }
    }

    public interface ISimpleStep : IStep
    {
        ISimpleStep Clone();
    }

    public sealed class ImportStep : ISimpleStep
    {
        public String Name { get; set; }

        public ISimpleStep Clone()
        {
            return new ImportStep { Name = Name };
        }
    }

    public sealed class ExportStep : ISimpleStep
    {
        public String Name { get; set; }

        public String ResourceType { get; set; }

        public IDictionary<String, String> Inputs { get; set; }

        public ISimpleStep Clone()
        {
            return new ExportStep
            {
                Name = Name,
                ResourceType = ResourceType,
                Inputs = new Dictionary<String, String>(Inputs ?? new Dictionary<String, String>(0)),
            };
        }
    }

    public sealed class TaskStep : ISimpleStep
    {
        public String Name { get; set; }

        public String Condition { get; set; }

        public Boolean ContinueOnError { get; set; }

        public Boolean Enabled { get; set; }

        public IDictionary<String, String> Environment { get; set; }

        public IDictionary<String, String> Inputs { get; set; }

        public TaskReference Reference { get; set; }

        public Int32 TimeoutInMinutes { get; set; }

        public ISimpleStep Clone()
        {
            return new TaskStep()
            {
                Name = Name,
                Condition = Condition,
                ContinueOnError = ContinueOnError,
                Enabled = Enabled,
                Environment = new Dictionary<String, String>(Environment ?? new Dictionary<String, String>(0)),
                Inputs = new Dictionary<String, String>(Inputs ?? new Dictionary<String, String>(0)),
                Reference = Reference?.Clone(),
                TimeoutInMinutes = TimeoutInMinutes,
            };
        }
    }

    public sealed class TaskReference
    {
        public String Name { get; set; }

        public String Version { get; set; }

        public TaskReference Clone()
        {
            return new TaskReference
            {
                Name = Name,
                Version = Version,
            };
        }
    }

    public sealed class StepsPhase : IStep
    {
        public String Name { get; set; }

        public List<ISimpleStep> Steps { get; set; }
    }

    public class StepsTemplateReference : IStep
    {
        public String Name { get; set; }

        public IDictionary<String, Object> Parameters { get; set; }

        public IDictionary<String, List<ISimpleStep>> StepOverrides { get; set; }
    }

    // A step template cannot reference other step templates (enforced during deserialization).
    public class StepsTemplate
    {
        public List<IStep> Steps { get; set; }
    }
}
