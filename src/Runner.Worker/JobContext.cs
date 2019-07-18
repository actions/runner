using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using GitHub.Runner.Common.Util;

namespace GitHub.Runner.Worker
{
    public sealed class JobContext : DictionaryContextData
    {
        public TaskResult? Status
        {
            get
            {
                if (this.TryGetValue("status", out var status) && status is StringContextData statusString)
                {
                    return EnumUtil.TryParse<TaskResult>(statusString);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                this["status"] = new StringContextData(value.ToString());
            }
        }
    }
}