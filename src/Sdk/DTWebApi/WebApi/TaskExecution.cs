using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskExecution
    {
        public TaskExecution()
        {
        }

        private TaskExecution(TaskExecution taskExecutionToBeCloned)
        {
            if (taskExecutionToBeCloned.ExecTask != null)
            {
                this.ExecTask = taskExecutionToBeCloned.ExecTask.Clone();
            }

            if (taskExecutionToBeCloned.PlatformInstructions != null)
            {
                this.PlatformInstructions = new Dictionary<String, Dictionary<String, String>>(taskExecutionToBeCloned.PlatformInstructions, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// The utility task to run.  Specifying this means that this task definition is simply a meta task to call another task.
        /// This is useful for tasks that call utility tasks like powershell and commandline
        /// </summary>
        [DataMember(Order = 10, EmitDefaultValue = false)]
        public TaskReference ExecTask
        {
            get;
            set;
        }

        /// <summary>
        /// If a task is going to run code, then this provides the type/script etc... information by platform.
        /// For example, it might look like.
        ///     net45: {
        ///         typeName: "GitHub.Automation.Tasks.PowerShellTask",
        ///         assemblyName: "GitHub.Automation.Tasks.PowerShell.dll"
        ///     }
        ///     net20: {
        ///         typeName: "GitHub.Automation.Tasks.PowerShellTask",
        ///         assemblyName: "GitHub.Automation.Tasks.PowerShell.dll"
        ///     }
        ///     java: {
        ///         jar: "powershelltask.tasks.automation.teamfoundation.microsoft.com",
        ///     }
        ///     node: {
        ///         script: "powershellhost.js",
        ///     }
        /// </summary>
        [DataMember(Order = 20, EmitDefaultValue = false)]
        public Dictionary<String, Dictionary<String, String>> PlatformInstructions
        {
            get;
            set;
        }

        internal TaskExecution Clone()
        {
            return new TaskExecution(this);
        }
    }
}
