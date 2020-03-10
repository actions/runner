using System;
using System.IO;
using GitHub.DistributedTask.Pipelines.ContextData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class FromJson : Function
    {
        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var json = Parameters[0].Evaluate(context).ConvertToString();
            using (var stringReader = new StringReader(json))
            using (var jsonReader = new JsonTextReader(stringReader) { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double })
            {
                var token = JToken.ReadFrom(jsonReader);
                return token.ToPipelineContextData();
            }
        }
    }}
