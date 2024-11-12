using System;

using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using Runner.Server.Azure.Devops;

namespace Runner.Language.Server;

public class AutoCompleter : ICompletionHandler
{
    private SharedData data;

    public AutoCompleter(SharedData data) {
        this.data = data;
    }

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new CompletionRegistrationOptions() {
            CompletionItem = new CompletionRegistrationCompletionItemOptions { LabelDetailsSupport = true },
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter() { Language = "yaml" }, new TextDocumentFilter() { Language = "azure-pipelines" }),
        };
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


    public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var content = data.Content[request.TextDocument.Uri];
        var currentFileName = "t.yml";
        var files = new Dictionary<string, string>() { { currentFileName, content } };

        var row = request.Position.Line + 1;
        var column = request.Position.Character + 1;
            
        var context = new Runner.Server.Azure.Devops.Context {
            FileProvider = new DefaultInMemoryFileProviderFileProvider(files.ToArray()),
            TraceWriter = new GitHub.DistributedTask.ObjectTemplating.EmptyTraceWriter(),
            Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives | GitHub.DistributedTask.Expressions2.ExpressionFlags.AllowAnyForInsert,
            Column = column,
            Row = row,
            RawMapping = true
        };
        GitHub.DistributedTask.ObjectTemplating.Schema.TemplateSchema? schema = null;
        try {
            var isWorkflow = request.TextDocument.Uri.Path.Contains("/.github/workflows/");
            if(isWorkflow || request.TextDocument.Uri.Path.Contains("/action.yml") || request.TextDocument.Uri.Path.Contains("/action.yaml")) {
                var templateContext = CreateTemplateContext(new EmptyTraceWriter());
                context.AutoCompleteMatches = new List<AutoCompleteEntry>();
                context.Flags = templateContext.Flags;
                templateContext.AutoCompleteMatches = context.AutoCompleteMatches;
                templateContext.Column = context.Column;
                templateContext.Row = context.Row;
                if(!isWorkflow) {
                    templateContext.Schema = PipelineTemplateSchemaFactory.GetActionSchema();
                }
                schema = templateContext.Schema;
                // Get the file ID
                var fileId = templateContext.GetFileId(currentFileName);

                // Read the file
                var fileContent = content;
                // Handle Empty Document
                if(string.IsNullOrWhiteSpace(fileContent)) {
                    context.AutoCompleteMatches.Add(new AutoCompleteEntry {
                        Definitions = [ schema.Definitions[isWorkflow ? "workflow-root" : "action-root"] ],
                        AllowedContext = [],
                        Depth = 0,
                        Token = new MappingToken(fileId, 1, 1)
                    });
                } else {
                    var yamlObjectReader = new YamlObjectReader(fileId, fileContent);
                    TemplateReader.Read(templateContext, isWorkflow ? "workflow-root" : "action-root", yamlObjectReader, fileId, out _);
                }
            } else {
                schema = AzureDevops.LoadSchema();
                var template = await AzureDevops.ParseTemplate(context, currentFileName, null, true);
                _ = template;
            }
        } catch
        {
        }
        List<Runner.Server.Azure.Devops.CompletionItem> list = AutoCompletetionHelper.CollectCompletions(column, row, context, schema);
        return new CompletionList(list.Select(c => new OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem() {
            Label = c.Label.Label,
            LabelDetails = new CompletionItemLabelDetails() { Description = c.Label.Description, Detail = c.Label.Detail },
            Documentation = c.Documentation != null ? new StringOrMarkupContent(new MarkupContent() { Kind = MarkupKind.Markdown, Value = c.Documentation.Value }) : null,
            InsertTextMode = InsertTextMode.AdjustIndentation,
            Kind = (CompletionItemKind) (c.Kind ?? (int)CompletionItemKind.Property),
            SortText = c.SortText,
            FilterText = c.FilterText,
            Preselect = c.Preselect ?? false,
            Detail = c.Detail,
            InsertTextFormat = InsertTextFormat.Snippet,
            CommitCharacters = c.CommitCharacters,
            InsertText = c.InsertText?.Value,
            TextEdit = c.Range != null ? new TextEditOrInsertReplaceEdit(new InsertReplaceEdit {
                Insert = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range {
                    Start = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position { Line = (int)c.Range.Inserting.Start.Line, Character = (int)c.Range.Inserting.Start.Character },
                    End = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position { Line = (int)c.Range.Inserting.End.Line, Character = (int)c.Range.Inserting.End.Character }},
                Replace = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range {
                    Start = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position { Line = (int)c.Range.Replacing.Start.Line, Character = (int)c.Range.Replacing.Start.Character },
                    End = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position { Line = (int)c.Range.Replacing.End.Line, Character = (int)c.Range.Replacing.End.Character }},
                NewText = c.InsertText?.Value ?? c.Label.Label,
            } ) : null
        }));
    }
}
