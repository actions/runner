using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TaskOrchestrationContainer : TaskOrchestrationItem, IOrchestrationProcess
    {
        public TaskOrchestrationContainer()
            : base(TaskOrchestrationItemType.Container)
        {
            ContinueOnError = true;
            MaxConcurrency = Int32.MaxValue;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean Parallel
        {
            get;
            set;
        }

        public List<TaskOrchestrationItem> Children
        {
            get
            {
                if (m_children == null)
                {
                    m_children = new List<TaskOrchestrationItem>();
                }

                return m_children;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskOrchestrationContainer Rollback
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = true)]
        public Boolean ContinueOnError
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = true)]
        public Int32 MaxConcurrency
        {
            get;
            set;
        }
        
        /// <summary>
        /// Get additional specifications for this container.
        /// </summary>
        /// <remarks>
        /// This provides an extensibility for consumers of DT SDK to pass additional data
        /// to Orchestrations. Each Orchestration is free to interpret this data as appropriate.
        /// </remarks>
        public IDictionary<String, String> Data
        {
            get
            {
                if (m_data == null)
                {
                    m_data = new Dictionary<String, String>();
                }

                return m_data;
            }
        }

        OrchestrationProcessType IOrchestrationProcess.ProcessType
        {
            get
            {
                return OrchestrationProcessType.Container;
            }
        }

        public IEnumerable<TaskOrchestrationJob> GetJobs()
        {
            var containerQueue = new Queue<TaskOrchestrationContainer>();
            containerQueue.Enqueue(this);

            while (containerQueue.Count > 0)
            {
                var currentContainer = containerQueue.Dequeue();
                foreach (var item in currentContainer.Children)
                {
                    switch (item.ItemType)
                    {
                        case TaskOrchestrationItemType.Container:
                            containerQueue.Enqueue((TaskOrchestrationContainer)item);
                            break;

                        case TaskOrchestrationItemType.Job:
                            yield return item as TaskOrchestrationJob;
                            break;
                    }
                }
            }
        }

        [DataMember(Name = "Children")]
        private List<TaskOrchestrationItem> m_children;

        [DataMember(Name = "Data", EmitDefaultValue = false)]
        private IDictionary<String, String> m_data;
    }
}
