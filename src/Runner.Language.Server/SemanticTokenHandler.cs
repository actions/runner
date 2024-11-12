using System;
using System.Collections.Immutable;
using System.Linq.Expressions;

using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using Runner.Server.Azure.Devops;

namespace Runner.Language.Server;

public class SemanticTokenHandler : ISemanticTokensFullHandler
{
    private SharedData data;

    public SemanticTokenHandler(SharedData data) {
        this.data = data;
    }
    public SemanticTokensRegistrationOptions GetRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
    {
        return new SemanticTokensRegistrationOptions {
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter() { Language = "yaml" }, new TextDocumentFilter() { Language = "azure-pipelines" }),
            Full = true,
            Legend = new SemanticTokensLegend {
                TokenModifiers = new List<SemanticTokenModifier>{ new SemanticTokenModifier("readonly"), new SemanticTokenModifier("defaultLibrary"), new SemanticTokenModifier("numeric") },
                TokenTypes = new List<SemanticTokenType> { new SemanticTokenType("variable"), new SemanticTokenType("parameter"), new SemanticTokenType("function"), new SemanticTokenType("property"), new SemanticTokenType("constant"), new SemanticTokenType("punctuation"), new SemanticTokenType("string")}
            }
        };
    }

    private static TemplateContext  CreateTemplateContext(GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter) {
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


    public async Task<OmniSharp.Extensions.LanguageServer.Protocol.Models.SemanticTokens?> Handle(SemanticTokensParams request, CancellationToken cancellationToken)
    {
        var content = data.Content[request.TextDocument.Uri];
        var currentFileName = "t.yml";
        var files = new Dictionary<string, string>() { { currentFileName, content } };
            
        var context = new Runner.Server.Azure.Devops.Context {
            FileProvider = new DefaultInMemoryFileProviderFileProvider(files.ToArray()),
            TraceWriter = new GitHub.DistributedTask.ObjectTemplating.EmptyTraceWriter(),
            Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives | GitHub.DistributedTask.Expressions2.ExpressionFlags.AllowAnyForInsert,
            RawMapping = true
        };
        TemplateToken? token = null;
        List<int>? semTokens = null;
        try {
            var isWorkflow = request.TextDocument.Uri.Path.Contains("/.github/workflows/");
            if(isWorkflow || request.TextDocument.Uri.Path.Contains("/action.yml") || request.TextDocument.Uri.Path.Contains("/action.yaml")) {
                var templateContext = CreateTemplateContext(new EmptyTraceWriter());
                context.AutoCompleteMatches = new List<AutoCompleteEntry>();
                context.Flags = templateContext.Flags;
                semTokens = templateContext.SemTokens;
                if(!isWorkflow) {
                    templateContext.Schema = PipelineTemplateSchemaFactory.GetActionSchema();
                }
                // Get the file ID
                var fileId = templateContext.GetFileId(currentFileName);

                // Read the file
                var fileContent = content;
                var yamlObjectReader = new YamlObjectReader(fileId, fileContent);
                token = TemplateReader.Read(templateContext, isWorkflow ? "workflow-root" : "action-root", yamlObjectReader, fileId, out _);


            } else {
                var template = await AzureDevops.ParseTemplate(context, currentFileName, null, true);
                token = template.Item2;
            }
        } catch
        {
        }
        semTokens ??= context.SemTokens;
        if(semTokens != null) {
            return new SemanticTokens {
                Data = [.. semTokens],
            };
        }
        List<int> rdata = new List<int>();
        if(token != null) {
            var lastLine = 1;
            var lastColumn = 0;
            foreach(var t in token.Traverse()) {
                if(t is LiteralToken l && l.RawData != null && t.Line != null && t.Column != null) {
                    rdata.AddRange([t.Line.Value - lastLine, (t.Line.Value - lastLine) != 0 ? t.Column.Value - 1: t.Column.Value - lastColumn, l.RawData.Length, 0, 0]);
                    lastLine = t.Line.Value;
                    lastColumn = t.Column.Value;
                }
            }
        }
        return new OmniSharp.Extensions.LanguageServer.Protocol.Models.SemanticTokens {
            Data = [.. rdata],
        };
    }
}
