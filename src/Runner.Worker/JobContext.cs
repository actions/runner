using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Common;
using System;
using System.Collections.Generic;

namespace GitHub.Runner.Worker
{
    public sealed class JobContext : DictionaryContextData, IEnvironmentContextData
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
                this["status"] = new StringContextData(value.ToString().ToLowerInvariant());
            }
        }

        public DictionaryContextData Services
        {
            get
            {
                if (this.TryGetValue("services", out var services) && services is DictionaryContextData servicesDictionary)
                {
                    return servicesDictionary;
                }
                else
                {
                    this["services"] = new DictionaryContextData();
                    return this["services"] as DictionaryContextData;
                }
            }
        }

        public DictionaryContextData Container
        {
            get
            {
                if (this.TryGetValue("container", out var container) && container is DictionaryContextData containerDictionary)
                {
                    return containerDictionary;
                }
                else
                {
                    this["container"] = new DictionaryContextData();
                    return this["container"] as DictionaryContextData;
                }
            }
        }

        public double? CheckRunId
        {
            get
            {
                if (this.TryGetValue("check_run_id", out var value) && value is NumberContextData number)
                {
                    return number.Value;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value.HasValue)
                {
                    this["check_run_id"] = new NumberContextData(value.Value);
                }
                else
                {
                    this["check_run_id"] = null;
                }
            }
        }

        private readonly HashSet<string> _contextEnvAllowlist = new(StringComparer.OrdinalIgnoreCase)
        {
            "check_run_id",
            "status",
        };

        public IEnumerable<KeyValuePair<string, string>> GetRuntimeEnvironmentVariables()
        {
            foreach (var data in this)
            {
                if (_contextEnvAllowlist.Contains(data.Key))
                {
                    if (data.Value is StringContextData value)
                    {
                        yield return new KeyValuePair<string, string>($"JOB_{data.Key.ToUpperInvariant()}", value);
                    }
                    else if (data.Value is NumberContextData numberValue)
                    {
                        yield return new KeyValuePair<string, string>($"JOB_{data.Key.ToUpperInvariant()}", numberValue.ToString());
                    }
                }
            }
        }
    }
}
