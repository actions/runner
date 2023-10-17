using GitHub.DistributedTask.Pipelines.ContextData;
using System;

namespace Runner.Server.Azure.Devops
{
    public static class ContextDataProviderExtensions
    {
        public static string ToYaml(this IContextDataProvider pipeline)
        {
            // convert back to JToken
            var newcontent = pipeline.ToContextData().ToJToken().ToString();

            // serialize back to YAML
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
            var serializer = new YamlDotNet.Serialization.SerializerBuilder().WithEventEmitter(emitter =>
            {
                return new MyEventEmitter(emitter);
            }).Build();
            return serializer.Serialize(deserializer.Deserialize<Object>(newcontent));   
        }

        private class MyEventEmitter : YamlDotNet.Serialization.EventEmitters.ChainedEventEmitter
        {
            public MyEventEmitter(YamlDotNet.Serialization.IEventEmitter emitter) : base(emitter)
            {
            }

            private class ReplaceDescriptor : YamlDotNet.Serialization.IObjectDescriptor
            {
                public ReplaceDescriptor(Type type, Type staticType)
                {
                    Type = type;
                    StaticType = staticType;
                }

                public object? Value { get; set; }
                public Type Type { get; private set; }
                public Type StaticType { get; private set; }
                public YamlDotNet.Core.ScalarStyle ScalarStyle { get; set; }
            }

            public override void Emit(YamlDotNet.Serialization.ScalarEventInfo eventInfo, YamlDotNet.Core.IEmitter emitter)
            {
                if (eventInfo.Source.Value is string svalue)
                {
                    // Apply expression escaping to allow parsing the result without errors
                    if (svalue.Contains("${{"))
                    {
                        eventInfo = new YamlDotNet.Serialization.ScalarEventInfo(
                            new ReplaceDescriptor(type: eventInfo.Source.Type, staticType: eventInfo.Source.StaticType)
                            {
                                Value = svalue.Replace("${{", "${{ '${{' }}"),
                                ScalarStyle = eventInfo.Source.ScalarStyle
                            });
                    }
                    if (svalue.Contains('\n'))
                    {
                        eventInfo.Style = YamlDotNet.Core.ScalarStyle.Literal;
                        eventInfo.IsPlainImplicit = false;
                        eventInfo.IsQuotedImplicit = false;
                    }
                }
                base.Emit(eventInfo, emitter);
            }
        }
    }
}
