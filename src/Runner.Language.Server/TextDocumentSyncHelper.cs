using System;
using System.Linq;
using System.Text.RegularExpressions;

using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;

using MediatR;

using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using YamlDotNet.RepresentationModel;

using Runner.Server.Azure.Devops;

namespace Runner.Language.Server;

public partial class TextDocumentSyncHelper : TextDocumentSyncHandlerBase
{
    private SharedData data;
    public TextDocumentSyncHelper(SharedData data) {
        this.data = data;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new TextDocumentAttributes(uri, "yaml");
    }

    private async Task<bool> ShouldHandle(TextDocumentIdentifier doc, string content, string? langID) {
        var known = langID == "azure-pipelines" || doc.Uri.Path.Contains("/.github/workflows/") || doc.Uri.Path.EndsWith("/action.yml") || doc.Uri.Path.EndsWith("/azure-pipeline.yml");
        
        if(known) {
            return true;
        }
        try {
            var input = new StringReader(content);
            var yamlStream = new YamlStream();
            yamlStream.Load(input);
            var rootNode = (YamlMappingNode)yamlStream.Documents[0].RootNode; 
            var isPipeline = CheckIsPipeline(rootNode);
            return isPipeline != null;
        } catch {

        }
        
        return false;

        static bool CheckAllIsIn(YamlMappingNode obj, string[] allowed)
        {
            foreach (var k in obj.Children.Keys)
            {
                if (!allowed.Contains(k.ToString()))
                {
                    return false;
                }
            }
            return true;
        }

        static YamlMappingNode CheckIsPipeline(YamlMappingNode obj)
        {
            try
            {
                var hasPipelineProperties = 
                    (obj.Children.ContainsKey("trigger") || obj.Children.ContainsKey("pr") ||
                    (obj.Children.ContainsKey("resources") && 
                    (obj["resources"] is YamlMappingNode resources &&
                    (resources.Children.ContainsKey("builds") ||
                        resources.Children.ContainsKey("containers") ||
                        resources.Children.ContainsKey("pipelines") ||
                        resources.Children.ContainsKey("repositories") ||
                        resources.Children.ContainsKey("webhooks") ||
                        resources.Children.ContainsKey("packages")))) ||
                    obj.Children.ContainsKey("schedules") ||
                    obj.Children.ContainsKey("lockBehavior") ||
                    obj.Children.ContainsKey("variables") ||
                    obj.Children.ContainsKey("parameters")) &&
                    (obj.Children.ContainsKey("stages") ||
                    obj.Children.ContainsKey("jobs") ||
                    obj.Children.ContainsKey("steps"))
                    || obj.Children.ContainsKey("extends") && obj["extends"] is YamlMappingNode extends && extends.Children.ContainsKey("template")
                    || obj.Children.ContainsKey("steps") && ((YamlSequenceNode)obj["steps"]).Children.Any(x => x is YamlMappingNode step && 
                    (step.Children.ContainsKey("task") || step.Children.ContainsKey("script") ||
                        step.Children.ContainsKey("bash") || step.Children.ContainsKey("pwsh") ||
                        step.Children.ContainsKey("powershell") || step.Children.ContainsKey("template")))
                    || obj.Children.ContainsKey("jobs") && ((YamlSequenceNode)obj["jobs"]).Children.Any(x => x is YamlMappingNode job && 
                    (job.Children.ContainsKey("job") || job.Children.ContainsKey("deployment") ||
                        job.Children.ContainsKey("template")))
                    || obj.Children.ContainsKey("stages") && ((YamlSequenceNode)obj["stages"]).Children.Any(x => x is YamlMappingNode stage && 
                    (stage.Children.ContainsKey("stage") || stage.Children.ContainsKey("template")))
                    || obj.Children.ContainsKey("variables") && ((YamlSequenceNode)obj["variables"]).Children.Any(x => x is YamlMappingNode variable &&
                    (variable.Children.ContainsKey("name") && variable.Children.ContainsKey("value") ||
                        variable.Children.ContainsKey("group") || variable.Children.ContainsKey("template")))
                    || obj.Children.ContainsKey("variables") && CheckAllIsIn(obj, new[] { "parameters", "variables" });

                return hasPipelineProperties ? obj : null;
            }
            catch
            {
                return null;
            }
        }
    } 

    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        if(await ShouldHandle(request.TextDocument, request.TextDocument.Text, request.TextDocument.LanguageId)) {
            this.data.Content[request.TextDocument.Uri] = request.TextDocument.Text;
            await ValidateSyntaxAsync(request.TextDocument.Uri);
        } else {
            this.data.Content.Remove(request.TextDocument.Uri);
            SendDiagnostics(request.TextDocument.Uri, new List<string>());
        }
        return Unit.Value;
    }

    public override async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        if(await ShouldHandle(request.TextDocument, request.ContentChanges.FirstOrDefault()?.Text, null)) {
            this.data.Content[request.TextDocument.Uri] = request.ContentChanges.FirstOrDefault()?.Text ?? "";
            await ValidateSyntaxAsync(request.TextDocument.Uri);
        } else {
            this.data.Content.Remove(request.TextDocument.Uri);
            SendDiagnostics(request.TextDocument.Uri, new List<string>());
        }
        return Unit.Value;
    }

    private static TemplateContext CreateTemplateContext(GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter) {
        ExpressionFlags flags = ExpressionFlags.None;

        var templateContext = new TemplateContext() {
            Flags = flags,
            CancellationToken = CancellationToken.None,
            Errors = new TemplateValidationErrors(10, 500),
            Memory = new TemplateMemory(
                maxDepth: 100,
                maxEvents: 1000000,
                maxBytes: 10 * 1024 * 1024),
            TraceWriter = traceWriter,
            Schema = PipelineTemplateSchemaFactory.GetSchema()
        };
        return templateContext;
    }

    private async Task ValidateSyntaxAsync(DocumentUri uri)
    {
        var content = this.data.Content[uri];
        var currentFileName = "t.yml";
        var files = new Dictionary<string, string>() { { currentFileName, content } };
            
        var context = new Context {
            FileProvider = new DefaultInMemoryFileProviderFileProvider(files.ToArray()),
            TraceWriter = new GitHub.DistributedTask.ObjectTemplating.EmptyTraceWriter(),
            Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives | GitHub.DistributedTask.Expressions2.ExpressionFlags.AllowAnyForInsert,
        };
        try {
            var isWorkflow = uri.Path.Contains("/.github/workflows/");
            if(isWorkflow || uri.Path.Contains("/action.yml") || uri.Path.Contains("/action.yaml")) {
                var templateContext = CreateTemplateContext(new EmptyTraceWriter());
                // Get the file ID
                var fileId = templateContext.GetFileId(currentFileName);
                if(!isWorkflow) {
                    templateContext.Schema = PipelineTemplateSchemaFactory.GetActionSchema();
                }

                // Read the file
                var fileContent = content;
                using (var stringReader = new StringReader(fileContent))
                {
                    var yamlObjectReader = new YamlObjectReader(fileId, stringReader);
                    TemplateReader.Read(templateContext, isWorkflow ? "workflow-root" : "action-root", yamlObjectReader, fileId, out _);
                }

                templateContext.Errors.Check();
            } else {
                var template = await AzureDevops.ParseTemplate(context, currentFileName, null, true);
                _ = template;
            }
            SendDiagnostics(uri, new List<string>());
        } catch(TemplateValidationException ex) {
            var fileIdReplacer = new System.Text.RegularExpressions.Regex("FileId: (\\d+)");
            var allErrors = new List<string>();
            foreach(var error in ex.Errors) {
                var errorContent = fileIdReplacer.Replace(error.Message, match => {
                    return $"{context.FileTable[int.Parse(match.Groups[1].Value) - 1]}";
                });
                allErrors.Add(errorContent);
            }
            SendDiagnostics(uri, allErrors);
        } catch(Exception ex) {
            var fileIdReplacer = new System.Text.RegularExpressions.Regex("FileId: (\\d+)");
            var errorContent = fileIdReplacer.Replace(ex.Message, match => {
                return $"{context.FileTable[int.Parse(match.Groups[1].Value) - 1]}";
            });
            SendDiagnostics(uri, new List<string> { errorContent });
        }
    }

    private int version = 0;

    private void SendDiagnostics(DocumentUri documentUri, List<string> allErrors)
    {
        List<Diagnostic> diagnostics = new List<Diagnostic>();
        foreach(var ex in allErrors) {
            var matched = false;
            string[] err = null;
            var i = ex.IndexOf(" (Line: ");
            var regex1 = TemplateErrorRegex();
            var regex2 = YamlErrorRegex();
            var regex3 = MyRegex();
            if(i != -1 && !matched) {
                var m = regex1.Match(ex.Substring(i));
                if(m != null) {
                    string[] coll = [ m.Groups[0].Value, ex.Substring(0, i) ];
                    err = coll.Concat(m.Groups.AsReadOnly().Skip(1).Select(g => g.Value)).ToArray();
                }
            }
            if(err != null) {
                var r = err[1];
                var row = int.Parse(err[2]) - 1;
                var column = int.Parse(err[3]) - 1;
                var msg = err[4];
                var range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(row, column), new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(row, int.MaxValue));
                var diag = new Diagnostic() { Range = range, Message = msg, Severity = DiagnosticSeverity.Error };
                var uri = documentUri;//handle.refToUri[r];
                if(uri != null) {
                    matched = true;
                    //items.push([uri, [diag]]);
                    diagnostics.Add(diag);
                }
            }
            err = null;
            if(i != -1 && !matched) {
                var m = regex2.Match(ex.Substring(i - 1));
                if(m != null) {
                    string[] coll = [ m.Groups[0].Value, ex.Substring(0, i - 1) ];
                    err = coll.Concat(m.Groups.AsReadOnly().Skip(1).Select(g => g.Value)).ToArray();
                }
            }
            if(err != null) {
                var r = err[1];
                var row = int.Parse(err[2]) - 1;
                var column = int.Parse(err[3]) - 1;
                var rowEnd = int.Parse(err[4]) - 1;
                var columnEnd = int.Parse(err[5]) - 1;
                var msg = err[6];
                var range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(row, column), new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(rowEnd, columnEnd));
                var diag = new Diagnostic() { Range = range, Message = msg, Severity = DiagnosticSeverity.Error };
                var uri = documentUri;//handle.refToUri[r];
                if(uri != null) {
                    matched = true;
                    //items.push([uri, [diag]]);
                    diagnostics.Add(diag);
                }
            }
            err = !matched ? regex3.Match(ex)?.Groups?.AsReadOnly().Select(g => g.Value).ToArray() : null;
            if(err != null) {
                var r = err[1];
                var msg = err[2];
                var range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 0), new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 0));
                var diag = new Diagnostic() { Range = range, Message = msg, Severity = DiagnosticSeverity.Error };
                var uri = documentUri;//handle.refToUri[r];
                if(uri != null) {
                    matched = true;
                    //items.push([uri, [diag]]);
                    diagnostics.Add(diag);
                }
            }
            if(!matched) {
                // var uri = handle.refToUri[`(self)/${handle.filename}`];
                // var range = new vscode.Range(new vscode.Position(0, 0), new vscode.Position(0, 0));
                // var diag = new vscode.Diagnostic(range, ex, vscode.DiagnosticSeverity.Error);
                // if(uri) {
                //     items.push([uri, [diag]]);
                // }
                var range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 0), new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 0));
                var diag = new Diagnostic() { Range = range, Message = ex, Severity = DiagnosticSeverity.Error };
            }
        }
        
        data.Server?.PublishDiagnostics(new PublishDiagnosticsParams() { Diagnostics = diagnostics, Uri = documentUri, Version = version++ });
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSyncRegistrationOptions() {
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter() { Language = "yaml" }, new TextDocumentFilter() { Language = "azure-pipelines" }),
            Change = TextDocumentSyncKind.Full,
            Save = new SaveOptions() { IncludeText = true }
        };
    }

    [GeneratedRegex("^([^:]+): (.*)$")]
    private static partial Regex MyRegex();
    [GeneratedRegex("^: \\(Line: (\\d+), Col: (\\d+), Idx: \\d+\\) - \\(Line: (\\d+), Col: (\\d+), Idx: \\d+\\): (.*)$")]
    private static partial Regex YamlErrorRegex();
    [GeneratedRegex("^ \\(Line: (\\d+), Col: (\\d+)\\): (.*)$")]
    private static partial Regex TemplateErrorRegex();
}
