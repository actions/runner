using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

Console.WriteLine ("Hello, Console!");

public class MyClass {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string CallMeFromJS()
    {
        return "Hello, World!";
    }


    private class MyEventEmitter : YamlDotNet.Serialization.EventEmitters.ChainedEventEmitter {
        public MyEventEmitter(YamlDotNet.Serialization.IEventEmitter emitter) : base(emitter) {

        }

        private class ReplaceDescriptor : YamlDotNet.Serialization.IObjectDescriptor {
            public object? Value { get; set; }
            public Type Type { get; set; }
            public Type StaticType { get; set; }
            public YamlDotNet.Core.ScalarStyle ScalarStyle { get; set; }
        }

        public override void Emit(YamlDotNet.Serialization.ScalarEventInfo eventInfo, YamlDotNet.Core.IEmitter emitter)
        {
            if(eventInfo.Source.Value is string svalue) {
                // Apply expression escaping to allow parsing the result without errors
                if(svalue.Contains("${{")) {
                    eventInfo = new YamlDotNet.Serialization.ScalarEventInfo(new ReplaceDescriptor { Value = svalue.Replace("${{", "${{ '${{' }}"), Type = eventInfo.Source.Type, StaticType = eventInfo.Source.StaticType, ScalarStyle = eventInfo.Source.ScalarStyle });
                }
                if(svalue.Contains('\n')) {
                    eventInfo.Style = YamlDotNet.Core.ScalarStyle.Literal;
                    eventInfo.IsPlainImplicit = false;
                    eventInfo.IsQuotedImplicit = false;
                }
            }
            base.Emit(eventInfo, emitter);
        }
    }

    public class MyFileProvider : IFileProvider
    {
        public async Task<string> ReadFile(string repositoryAndRef, string path)
        {
            if(!string.IsNullOrEmpty(repositoryAndRef)) {
                return await Interop.ReadFile($"{repositoryAndRef}/{path}");
            }
            return await Interop.ReadFile(path);
        }
    }
    

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> ExpandCurrentPipeline(string currentFileName) {
        // Console.WriteLine ("Hello, ExpandCurrentPipeline!");
        // return "434-" + await Interop.ReadFile(filename);
        try {
            var content = await Interop.ReadFile(currentFileName);
            var context = new Runner.Server.Azure.Devops.Context {
                FileProvider = new MyFileProvider(),
                TraceWriter = new GitHub.DistributedTask.ObjectTemplating.EmptyTraceWriter(),
                Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives
            };
            var template = await AzureDevops.ReadTemplate(context, currentFileName);
            var pipeline = await new Runner.Server.Azure.Devops.Pipeline().Parse(context.ChildContext(template, currentFileName), template);
            var newcontent = pipeline.ToContextData().ToJToken().ToString();
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
            var serializer = new YamlDotNet.Serialization.SerializerBuilder().WithEventEmitter(emitter => {
                return new MyEventEmitter(emitter);
            }).Build();
            return serializer.Serialize(deserializer.Deserialize<Object>(newcontent));
        } catch(Exception ex) {
            return ex.ToString();
        }
    }
}
