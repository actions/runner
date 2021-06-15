using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines.ContextData {
    public interface IDictionaryContextData : IEnumerable<KeyValuePair<String, PipelineContextData>> {
        Int32 Count { get; }
        IEnumerable<String> Keys { get; }
        IEnumerable<PipelineContextData> Values { get; }
        PipelineContextData this[String key] { get; set; }
        void Add(IEnumerable<KeyValuePair<String, PipelineContextData>> pairs);
        Boolean ContainsKey(String key);
    }
}