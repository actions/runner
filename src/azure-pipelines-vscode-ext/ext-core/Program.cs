using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Runner.Server.Azure.Devops;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

while(true) {
    await Interop.Sleep(10 * 60 * 1000);
}

public class MyClass {
    
    public class MyFileProvider : IFileProvider
    {
        public MyFileProvider(JSObject handle) {
            this.handle = handle;
        }
        private JSObject handle;
        public async Task<string> ReadFile(string repositoryAndRef, string path)
        {
            return await Interop.ReadFile(handle, repositoryAndRef, path);
        }
    }

    public class TraceWriter : GitHub.DistributedTask.ObjectTemplating.ITraceWriter {
        private JSObject handle;

        public TraceWriter(JSObject handle) {
            this.handle = handle;
        }

        public void Error(string format, params object[] args)
        {
            if(args?.Length == 1 && args[0] is Exception ex) {
                Interop.Log(handle, 5, string.Format("{0} {1}", format, ex.Message));
                return;
            }
            try {
                Interop.Log(handle, 5, args?.Length > 0 ? string.Format(format, args) : format);
            } catch {
                Interop.Log(handle, 5, format);
            }
        }

        public void Info(string format, params object[] args)
        {
            try {
                Interop.Log(handle, 3, args?.Length > 0 ? string.Format(format, args) : format);
            } catch {
                Interop.Log(handle, 3, format);
            }
        }

        public void Verbose(string format, params object[] args)
        {
            try {
                Interop.Log(handle, 2, args?.Length > 0 ? string.Format(format, args) : format);
            } catch {
                Interop.Log(handle, 2, format);
            }
        }
    }

    private class VariablesProvider : IVariablesProvider {
        public IDictionary<string, string> Variables { get; set; }
        public IDictionary<string, string> GetVariablesForEnvironment(string name = null) {
            return Variables;
        }
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> ExpandCurrentPipeline(JSObject handle, string currentFileName, string variables, string parameters, bool returnErrorContent) {
        var context = new Runner.Server.Azure.Devops.Context {
            FileProvider = new MyFileProvider(handle),
            TraceWriter = new TraceWriter(handle),
            Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives,
            RequiredParametersProvider = new RequiredParametersProvider(handle),
            VariablesProvider = new VariablesProvider { Variables = JsonConvert.DeserializeObject<Dictionary<string, string>>(variables) }
        };
        try {
            Dictionary<string, TemplateToken> cparameters = new Dictionary<string, TemplateToken>();
            foreach(var kv in JsonConvert.DeserializeObject<Dictionary<string, string>>(parameters)) {
                cparameters[kv.Key] = AzurePipelinesUtils.ConvertStringToTemplateToken(kv.Value);
            }
            var template = await AzureDevops.ReadTemplate(context, currentFileName, cparameters);
            var pipeline = await new Runner.Server.Azure.Devops.Pipeline().Parse(context.ChildContext(template, currentFileName), template);
            return pipeline.ToYaml();
        } catch(Exception ex) {
            var fileIdReplacer = new System.Text.RegularExpressions.Regex("FileId: (\\d+)");
            var errorContent = fileIdReplacer.Replace(ex.Message, match => {
                return $"File: {context.FileTable[int.Parse(match.Groups[1].Value) - 1]}";
            });
            if(returnErrorContent) {
                await Interop.Error(handle, errorContent);
            } else {
                await Interop.Message(handle, 2, errorContent);
            }
            return null;
        }
    }
}
