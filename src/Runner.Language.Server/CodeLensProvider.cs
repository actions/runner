using System;

using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using Runner.Server.Azure.Devops;

namespace Runner.Language.Server;

public class CodeLensProvider : ICodeLensHandler
{
    private SharedData data;

    public CodeLensProvider(SharedData data) {
        this.data = data;
    }

    public CodeLensRegistrationOptions GetRegistrationOptions(CodeLensCapability capability, ClientCapabilities clientCapabilities)
    {
        return new CodeLensRegistrationOptions { 
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

    public async Task<CodeLensContainer?> Handle(CodeLensParams request, CancellationToken cancellationToken)
    {
        var content = data.Content[request.TextDocument.Uri];
        var currentFileName = "t.yml";
        var files = new Dictionary<string, string>() { { currentFileName, content } };
            
        var context = new Runner.Server.Azure.Devops.Context {
            FileProvider = new DefaultInMemoryFileProviderFileProvider(files.ToArray()),
            TraceWriter = new GitHub.DistributedTask.ObjectTemplating.EmptyTraceWriter(),
            Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives | GitHub.DistributedTask.Expressions2.ExpressionFlags.AllowAnyForInsert,
        };
        TemplateToken? token = null;
        List<int>? semTokens = null;
        try {
            var isWorkflow = request.TextDocument.Uri.Path.Contains("/.github/workflows/");
            if(isWorkflow) {
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
                var jobs = token.TraverseByPattern("jobs").FirstOrDefault().AssertMapping("");
                var codeLens = new List<CodeLens>();



                codeLens.Add(new CodeLens { Command = new Command { Name = "runner.server.runworkflow", Title = $"Run Workflow", Arguments = new Newtonsoft.Json.Linq.JArray(request.TextDocument.Uri.ToString()) },
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 0), new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 0))
                });

                for(int i = 0; i < jobs.Count; i++) {
                    codeLens.Add(new CodeLens { Command = new Command { Name = "runner.server.runjob", Title = $"Run job {jobs[i].Key}", Arguments = new Newtonsoft.Json.Linq.JArray(request.TextDocument.Uri.ToString(), jobs[i].Key.ToString()) },
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(jobs[i].Key.Line.Value, jobs[i].Key.Column.Value),
                            new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(jobs[i].Key.Line.Value, jobs[i].Key.Column.Value)
                        )
                    });
                }
                return new CodeLensContainer(codeLens);
            } else {
                var template = await AzureDevops.ParseTemplate(context, currentFileName, null, true);
                token = template.Item2;
            }
        } catch
        {
        }
        return new CodeLensContainer();
    }
}
