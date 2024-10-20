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
using GitHub.DistributedTask.ObjectTemplating.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using Sdk.Actions;

while (true) {
    await Interop.Sleep(10 * 60 * 1000);
}

public partial class MyClass {
    
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
    [JSExport]
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
    [JSExport]
    public static async Task ParseCurrentPipeline(JSObject handle, string currentFileName, string schemaName, int column, int row) {
        var context = new Context {
            FileProvider = new MyFileProvider(handle),
            TraceWriter = new TraceWriter(handle),
            Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives,
            RequiredParametersProvider = new RequiredParametersProvider(handle),
            VariablesProvider = new VariablesProvider { Variables = new Dictionary<string, string>() },
            Column = column,
            Row = row
        };
        var check = column == 0 && row == 0;
        try {
            var (name, template) = await AzureDevops.ParseTemplate(context, currentFileName, schemaName, true);
            Interop.Log(handle, 0, "Done: " + template.ToString());
        } catch(TemplateValidationException ex) when(check) {
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
            if(check) {
                var fileIdReplacer = new System.Text.RegularExpressions.Regex("FileId: (\\d+)");
                var errorContent = fileIdReplacer.Replace(ex.Message, match => {
                    return $"{context.FileTable[int.Parse(match.Groups[1].Value) - 1]}";
                });
                await Interop.Error(handle, JsonConvert.SerializeObject(new ErrorWrapper { Message = ex.Message, Errors = new List<string> { errorContent } }));
            }
        }
        if(!check && context.AutoCompleteMatches.Count > 0) {
            // Bug Only suggest scalar values if cursor is within the token
            // Don't suggest mapping and array on the other location, or fix autocomplete structure
            // transform string + multi enum values to oneofdefinition with constants so autocomplete works / use allowed values
            var schema = AzureDevops.LoadSchema();
            List<CompletionItem> list = AutoCompletetionHelper.CollectCompletions(column, row, context, schema);
            await Interop.AutoCompleteList(handle, JsonConvert.SerializeObject(list));
            var (pos, doc) = GetHoverResult(context, row, column);
            await Interop.HoverResult(handle, JsonConvert.SerializeObject(pos), doc);
        }
        if(check && context.SemTokens?.Count > 0) {
            await Interop.SemTokens(handle, [.. context.SemTokens]);
        }
        
    }

    private static (Runner.Server.Azure.Devops.Range, string) GetHoverResult(Context context, int row, int column) {
        var last = context.AutoCompleteMatches?.LastOrDefault();
        if(last?.Tokens?.Any() == true) {
            var tkn = last.Tokens.LastOrDefault(t => t.Index <= last.Index);
            if(tkn == null || tkn.Kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.String) {
                return (null, null);
            }

            var i = last.Tokens.IndexOf(tkn);

            var desc = PipelinesDescriptions.LoadDescriptions();
            
            return  (new Runner.Server.Azure.Devops.Range { Start = new Position { Line = row - 1, Character = column - 1 - (last.Index - tkn.Index) }, End = new Position { Line = row - 1, Character = column - 1 - (last.Index - tkn.Index) + tkn.RawValue.Length } }, i > 2 && last.Tokens[i - 2].Kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.NamedValue && last.Tokens[i - 1].Kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Dereference && new [] { "github", "runner", "strategy" }.Contains(last.Tokens[i - 2].RawValue.ToLower()) && desc[last.Tokens[i - 2].RawValue].TryGetValue(tkn.RawValue, out var d)
                    || i > 4 && last.Tokens[i - 4].Kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.NamedValue && last.Tokens[i - 3].Kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Dereference 
                    && last.Tokens[i - 2].Kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.PropertyName && last.Tokens[i - 1].Kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Dereference && new [] { "steps", "jobs", "needs" }.Contains(last.Tokens[i - 4].RawValue.ToLower()) && desc[last.Tokens[i - 4].RawValue].TryGetValue(tkn.RawValue, out d)
                    || desc["root"].TryGetValue(tkn.RawValue, out d)
                    || desc["functions"].TryGetValue(tkn.RawValue, out d) ? d.Description : tkn.RawValue);
        }
        return (new Runner.Server.Azure.Devops.Range { Start = last.Token.PreWhiteSpace != null ? new Position { Line = (int)last.Token.PreWhiteSpace.Line - 1, Character = (int)last.Token.PreWhiteSpace.Character - 1 } : new Position { Line = last.Token.Line.Value - 1, Character = last.Token.Column.Value - 1 }, End = new Position { Line = (int)last.Token.PostWhiteSpace.Line - 1, Character = (int)last.Token.PostWhiteSpace.Character - 1 } }, last.Definitions.FirstOrDefault()?.Description ?? "???");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [JSExport]
    public static string YAMLToJson(string content) {
        try {
            return AzurePipelinesUtils.YAMLToJson(content);
        } catch {
            return null;
        }
    }

}
