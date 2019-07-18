using System;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines.ContextData
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class JTokenExtensions
    {
        public static PipelineContextData ToPipelineContextData(this JToken value)
        {
            return value.ToPipelineContextData(1, 100);
        }

        public static PipelineContextData ToPipelineContextData(
            this JToken value, 
            Int32 depth, 
            Int32 maxDepth)
        {
            if (depth < maxDepth)
            {
                if (value.Type == JTokenType.String)
                {
                    return new StringContextData((String)value);
                }
                else if (value.Type == JTokenType.Boolean)
                {
                    return new BooleanContextData((Boolean)value);
                }
                else if (value.Type == JTokenType.Float || value.Type == JTokenType.Integer)
                {
                    return new NumberContextData((Double)value);
                }
                else if (value.Type == JTokenType.Object)
                {
                    var subContext = new DictionaryContextData();
                    var obj = (JObject)value;
                    foreach (var property in obj.Properties())
                    {
                        subContext[property.Name] = ToPipelineContextData(property.Value, depth + 1, maxDepth);
                    }
                    return subContext;
                }
                else if (value.Type == JTokenType.Array)
                {
                    var arrayContext = new ArrayContextData();
                    var arr = (JArray)value;
                    foreach (var element in arr)
                    {
                        arrayContext.Add(ToPipelineContextData(element, depth + 1, maxDepth));
                    }
                    return arrayContext;
                }
                else if (value.Type == JTokenType.Null)
                {
                    return null;
                }
            }

            // We don't understand the type or have reached our max, return as string
            return new StringContextData(value.ToString());
        }
    }
}
