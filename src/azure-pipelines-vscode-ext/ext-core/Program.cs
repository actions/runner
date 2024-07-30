using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
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

    public class ErrorWrapper {
        public string Message { get; set; }
        public List<string> Errors { get; set; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> ExpandCurrentPipeline(JSObject handle, string currentFileName, string variables, string parameters, bool returnErrorContent, string schema) {
        var context = new Runner.Server.Azure.Devops.Context {
            FileProvider = new MyFileProvider(handle),
            TraceWriter = new TraceWriter(handle),
            Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives,
            RequiredParametersProvider = new RequiredParametersProvider(handle),
            VariablesProvider = new VariablesProvider { Variables = JsonConvert.DeserializeObject<Dictionary<string, string>>(variables) }
        };
        string yaml = null;
        try {
            Dictionary<string, TemplateToken> cparameters = new Dictionary<string, TemplateToken>();
            foreach(var kv in JsonConvert.DeserializeObject<Dictionary<string, string>>(parameters)) {
                cparameters[kv.Key] = AzurePipelinesUtils.ConvertStringToTemplateToken(kv.Value);
            }
            var template = await AzureDevops.ReadTemplate(context, currentFileName, cparameters, schema);
            var pipeline = await new Runner.Server.Azure.Devops.Pipeline().Parse(context.ChildContext(template, currentFileName), template);
            yaml = pipeline.ToYaml();
            // The errors generated here shouldn't prevent the preview to show the result
            pipeline.CheckPipelineForRuntimeFailure();
            return yaml;
        } catch(TemplateValidationException ex) when(returnErrorContent) {
            var fileIdReplacer = new System.Text.RegularExpressions.Regex("FileId: (\\d+)");
            var allErrors = new List<string>();
            foreach(var error in ex.Errors) {
                var errorContent = fileIdReplacer.Replace(error.Message, match => {
                    return $"{context.FileTable[int.Parse(match.Groups[1].Value) - 1]}";
                });
                allErrors.Add(errorContent);
            }
            await Interop.Error(handle, JsonConvert.SerializeObject(new ErrorWrapper { Message = ex.Message, Errors = allErrors }));
            return yaml;
        } catch(Exception ex) {
            var fileIdReplacer = new System.Text.RegularExpressions.Regex("FileId: (\\d+)");
            var errorContent = fileIdReplacer.Replace(ex.Message, match => {
                return $"{context.FileTable[int.Parse(match.Groups[1].Value) - 1]}";
            });
            if(returnErrorContent) {
                await Interop.Error(handle, JsonConvert.SerializeObject(new ErrorWrapper { Message = ex.Message, Errors = new List<string> { errorContent } }));
            } else {
                await Interop.Message(handle, 2, errorContent);
            }
            return yaml;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task ParseCurrentPipeline(JSObject handle, string currentFileName, string schemaName) {
        var context = new Context {
            FileProvider = new MyFileProvider(handle),
            TraceWriter = new TraceWriter(handle),
            Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives,
            RequiredParametersProvider = new RequiredParametersProvider(handle),
            VariablesProvider = new VariablesProvider { Variables = new Dictionary<string, string>() }
        };
        try {
            var (name, template) = await AzureDevops.ParseTemplate(context, currentFileName, schemaName);
            Interop.Log(handle, 0, "Done: " + template.ToString());
        } catch(TemplateValidationException ex) {
            var fileIdReplacer = new System.Text.RegularExpressions.Regex("FileId: (\\d+)");
            var allErrors = new List<string>();
            foreach(var error in ex.Errors) {
                var errorContent = fileIdReplacer.Replace(error.Message, match => {
                    return $"{context.FileTable[int.Parse(match.Groups[1].Value) - 1]}";
                });
                allErrors.Add(errorContent);
            }
            await Interop.Error(handle, JsonConvert.SerializeObject(new ErrorWrapper { Message = ex.Message, Errors = allErrors }));
        } catch(Exception ex) {
            var fileIdReplacer = new System.Text.RegularExpressions.Regex("FileId: (\\d+)");
            var errorContent = fileIdReplacer.Replace(ex.Message, match => {
                return $"{context.FileTable[int.Parse(match.Groups[1].Value) - 1]}";
            });
            await Interop.Error(handle, JsonConvert.SerializeObject(new ErrorWrapper { Message = ex.Message, Errors = new List<string> { errorContent } }));
        }
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string YAMLToJson(string content) {
        try {
            return AzurePipelinesUtils.YAMLToJson(content);
        } catch {
            return null;
        }
    }

}
