using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker
{
    public sealed class JobContext : DictionaryContextData
    {
        public ActionResult? Status
        {
            get
            {
                if (this.TryGetValue("status", out var status) && status is StringContextData statusString)
                {
                    return EnumUtil.TryParse<ActionResult>(statusString);
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