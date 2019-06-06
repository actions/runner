using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

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

        [DataMember]
        AgentPlugin = 4,
    }

    [DataContract]
    [KnownType(typeof(ContainerRegistryReference))]
    [KnownType(typeof(RepositoryPathReference))]
    [KnownType(typeof(PluginReference))]
    [KnownType(typeof(ScriptReference))]    
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
#pragma warning disable CS0618 // Type or member is obsolete
            this.Container = referenceToClone.Container;
#pragma warning restore CS0618 // Type or member is obsolete
            this.Image = referenceToClone.Image;
        }

        [DataMember(EmitDefaultValue = false)]
        public override ActionSourceType Type => ActionSourceType.ContainerRegistry;

        /// <summary>
        /// Container resource alias
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [Obsolete("Deprecated", false)]
        public string Container
        {
            get;
            set;
        }

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
#pragma warning disable CS0618 // Type or member is obsolete
            this.Repository = referenceToClone.Repository;
#pragma warning restore CS0618 // Type or member is obsolete
            this.Name = referenceToClone.Name;
            this.Ref = referenceToClone.Ref;
            this.RepositoryType = referenceToClone.RepositoryType;
            this.Path = referenceToClone.Path;
        }

        [DataMember(EmitDefaultValue = false)]
        public override ActionSourceType Type => ActionSourceType.Repository;

        /// <summary>
        /// Repository resource alias
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [Obsolete("Deprecated", false)]
        public string Repository
        {
            get;
            set;
        }

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
    public class PluginReference : ActionStepDefinitionReference
    {
        [JsonConstructor]
        public PluginReference()
        {
        }

        private PluginReference(PluginReference referenceToClone)
        {
            this.Plugin = referenceToClone.Plugin;
        }

        [DataMember(EmitDefaultValue = false)]
        public override ActionSourceType Type => ActionSourceType.AgentPlugin;

        /// <summary>
        /// Agent plugin name
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Plugin
        {
            get;
            set;
        }

        public override ActionStepDefinitionReference Clone()
        {
            return new PluginReference(this);
        }
    }
}
