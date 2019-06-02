using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Common.Contracts
{
    [DataContract]
    public class TaskInputDefinitionBase : BaseSecuredObject
    {
        public TaskInputDefinitionBase()
        {
            InputType = TaskInputType.String;
            DefaultValue = String.Empty;
            Required = false;
            HelpMarkDown = String.Empty;
        }

        protected TaskInputDefinitionBase(TaskInputDefinitionBase inputDefinitionToClone)
            : this(inputDefinitionToClone, null)
        {
        }

        protected TaskInputDefinitionBase(TaskInputDefinitionBase inputDefinitionToClone, ISecuredObject securedObject)
            : base(securedObject)
        {
            this.DefaultValue = inputDefinitionToClone.DefaultValue;
            this.InputType = inputDefinitionToClone.InputType;
            this.Label = inputDefinitionToClone.Label;
            this.Name = inputDefinitionToClone.Name;
            this.Required = inputDefinitionToClone.Required;
            this.HelpMarkDown = inputDefinitionToClone.HelpMarkDown;
            this.VisibleRule = inputDefinitionToClone.VisibleRule;
            this.GroupName = inputDefinitionToClone.GroupName;

            if (inputDefinitionToClone.Validation != null)
            {
                this.Validation = inputDefinitionToClone.Validation.Clone(securedObject);
            }

            if (inputDefinitionToClone.m_aliases != null)
            {
                this.m_aliases = new List<String>(inputDefinitionToClone.m_aliases);
            }

            if (inputDefinitionToClone.m_options != null)
            {
                this.m_options = new Dictionary<String, String>(inputDefinitionToClone.m_options);
            }
            if (inputDefinitionToClone.m_properties != null)
            {
                this.m_properties = new Dictionary<String, String>(inputDefinitionToClone.m_properties);
            }
        }

        public IList<String> Aliases
        {
            get
            {
                if (m_aliases == null)
                {
                    m_aliases = new List<String>();
                }
                return m_aliases;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Label
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String DefaultValue
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean Required
        {
            get;
            set;
        }

        [DataMember(Name = "Type")]
        public String InputType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String HelpMarkDown
        {
            get;
            set;
        }

        // VisibleRule should specify the condition at which this input is to be shown/displayed
        // Typical format is "NAME OF THE DEPENDENT INPUT = VALUE TOBE BOUND"
        [DataMember(EmitDefaultValue = false)]
        public string VisibleRule
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string GroupName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskInputValidation Validation
        {
            get;
            set;
        }

        public Dictionary<String, String> Options
        {
            get
            {
                if (m_options == null)
                {
                    m_options = new Dictionary<String, String>();
                }
                return m_options;
            }
        }

        public Dictionary<String, String> Properties
        {
            get
            {
                if (m_properties == null)
                {
                    m_properties = new Dictionary<String, String>();
                }
                return m_properties;
            }
        }

        public virtual TaskInputDefinitionBase Clone(
            ISecuredObject securedObject)
        {
            return new TaskInputDefinitionBase(this, securedObject);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.DefaultValue.GetHashCode() ^ this.Label.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var taskInput2 = obj as TaskInputDefinitionBase;
            if (taskInput2 == null
                || !string.Equals(InputType, taskInput2.InputType, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Label, taskInput2.Label, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Name, taskInput2.Name, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(GroupName, taskInput2.GroupName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(DefaultValue, taskInput2.DefaultValue, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(HelpMarkDown, taskInput2.HelpMarkDown, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(VisibleRule, taskInput2.VisibleRule, StringComparison.OrdinalIgnoreCase)
                || !this.Required.Equals(taskInput2.Required))
            {
                return false;
            }

            if (!AreListsEqual(Aliases, taskInput2.Aliases)
                || !AreDictionariesEqual(Properties, taskInput2.Properties)
                || !AreDictionariesEqual(Options, taskInput2.Options))
            {
                return false;
            }

            if ((Validation != null && taskInput2.Validation == null)
                || (Validation == null && taskInput2.Validation != null)
                || ((Validation != null && taskInput2.Validation != null)
                    && !Validation.Equals(taskInput2.Validation)))
            {
                return false;
            }

            return true;
        }

        private bool AreDictionariesEqual(Dictionary<String, String> input1, Dictionary<String, String> input2)
        {
            if (input1 == null && input2 == null)
            {
                return true;
            }

            if ((input1 == null && input2 != null)
                || (input1 != null && input2 == null)
                || (input1.Count != input2.Count))
            {
                return false;
            }

            foreach (var key in input1.Keys)
            {
                if (!(input2.ContainsKey(key) && String.Equals(input1[key], input2[key], StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }

        private Boolean AreListsEqual(IList<String> list1, IList<String> list2)
        {
            if (list1.Count != list2.Count)
            {
                return false;
            }

            for (Int32 i = 0; i < list1.Count; i++)
            {
                if (!String.Equals(list1[i], list2[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        [DataMember(Name = "Aliases", EmitDefaultValue = false)]
        private List<String> m_aliases;

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        private Dictionary<String, String> m_options;

        [DataMember(Name = "Properties", EmitDefaultValue = false)]
        private Dictionary<String, String> m_properties;
    }
}
