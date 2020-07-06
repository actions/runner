using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum ActionSourceType
    {
        [DataMember]
        Repository = 1,

        [DataMember]
        ContainerRegistry = 2,

        [DataMember]
        Script = 3,

        CompositeOutput = 4,
    }

    [DataContract]
    [KnownType(typeof(ContainerRegistryReference))]
    [KnownType(typeof(RepositoryPathReference))]
    [KnownType(typeof(ScriptReference))]    
    [KnownType(typeof(CompositeOutputReference))]    
    [JsonConverter(typeof(ActionStepDefinitionReferenceConverter))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ActionStepDefinitionReference
    {
        [DataMember(EmitDefaultValue = false)]
        public abstract ActionSourceType Type { get; }

        public abstract ActionStepDefinitionReference Clone();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ContainerRegistryReference : ActionStepDefinitionReference
    {
        [JsonConstructor]
        public ContainerRegistryReference()
        {
        }

        private ContainerRegistryReference(ContainerRegistryReference referenceToClone)
        {
            this.Image = referenceToClone.Image;
        }

        [DataMember(EmitDefaultValue = false)]
        public override ActionSourceType Type => ActionSourceType.ContainerRegistry;

        /// <summary>
        /// Container image
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Image
        {
            get;
            set;
        }

        public override ActionStepDefinitionReference Clone()
        {
            return new ContainerRegistryReference(this);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RepositoryPathReference : ActionStepDefinitionReference
    {
        [JsonConstructor]
        public RepositoryPathReference()
        {
        }

        private RepositoryPathReference(RepositoryPathReference referenceToClone)
        {
            this.Name = referenceToClone.Name;
            this.Ref = referenceToClone.Ref;
            this.RepositoryType = referenceToClone.RepositoryType;
            this.Path = referenceToClone.Path;
        }

        [DataMember(EmitDefaultValue = false)]
        public override ActionSourceType Type => ActionSourceType.Repository;

        /// <summary>
        /// Repository name
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Repository ref, branch/tag/commit
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Ref
        {
            get;
            set;
        }

        /// <summary>
        /// Repository type, github/AzureRepo/etc
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string RepositoryType
        {
            get;
            set;
        }

        /// <summary>
        /// Path to action entry point directory
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Path
        {
            get;
            set;
        }

        public override ActionStepDefinitionReference Clone()
        {
            return new RepositoryPathReference(this);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ScriptReference : ActionStepDefinitionReference
    {
        [JsonConstructor]
        public ScriptReference()
        {
        }

        private ScriptReference(ScriptReference referenceToClone)
        {
        }

        [DataMember(EmitDefaultValue = false)]
        public override ActionSourceType Type => ActionSourceType.Script;

        public override ActionStepDefinitionReference Clone()
        {
            return new ScriptReference(this);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CompositeOutputReference : ActionStepDefinitionReference
    {
        [JsonConstructor]
        public CompositeOutputReference(Dictionary<String, String> scopeAndContextNames, MappingToken outputs, String parentScopeName)
        {
            this.ScopeAndContextNames = scopeAndContextNames;
            this.ParentScopeName = parentScopeName;
            this.Outputs = outputs;
        }

        private CompositeOutputReference(CompositeOutputReference referenceToClone)
        {
            // this.ParentEnv = referenceToClone.ParentEnv;
            this.ScopeAndContextNames = referenceToClone.ScopeAndContextNames;
        }

        [DataMember(EmitDefaultValue = false)]
        public override ActionSourceType Type => ActionSourceType.CompositeOutput;

        // [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, String> ScopeAndContextNames
        {
            get; 
            set;
        }

        public String ParentScopeName
        {
            get; 
            set;
        }

        public MappingToken Outputs
        {
            get; 
            set;
        }

        public override ActionStepDefinitionReference Clone()
        {
            return new CompositeOutputReference(this);
        }
    }
}
