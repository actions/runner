using System;
using System.Text;

using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using Runner.Server.Azure.Devops;
using Sdk.Actions;

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
            DocumentSelector = new TextDocumentSelector(new TextDocumentFilter() { Language = "yaml", Pattern = "**/.github/workflows/*.{yml,yaml}" }),
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
        string content;
        if(!data.Content.TryGetValue(request.TextDocument.Uri, out content)) {
            return null;
        }
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
                var jobs = token.TraverseByPattern(false, "jobs").FirstOrDefault().AssertMapping("");
                var codeLens = new List<CodeLens>();

                MappingToken mappingEvent = null;
                var tk = token.TraverseByPattern(false, "on").FirstOrDefault();
                List<string> events = new List<string>();
                switch(tk.Type) {
                    case TokenType.String:
                        events.Add(tk.AssertString("on").Value);
                        break;
                    case TokenType.Sequence:
                        events.AddRange(from r in tk.AssertSequence("on") select r.AssertString("").Value);
                        break;
                    case TokenType.Mapping:
                        events.AddRange(from r in tk.AssertMapping("on") select r.Key.AssertString("").Value);
                        break;
                }


                codeLens.Add(new CodeLens { Command = new Command { Name = "runner.server.runworkflow", Title = $"Run Workflow", Arguments = new Newtonsoft.Json.Linq.JArray(request.TextDocument.Uri.ToString(), new Newtonsoft.Json.Linq.JArray(events.ToArray())) },
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 0), new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 0))
                });

                for(int i = 0; i < jobs.Count; i++) {
                    codeLens.Add(new CodeLens { Command = new Command { Name = "runner.server.runjob", Title = $"Run job {jobs[i].Key}", Arguments = new Newtonsoft.Json.Linq.JArray(request.TextDocument.Uri.ToString(), jobs[i].Key.ToString(), new Newtonsoft.Json.Linq.JArray(events.ToArray())) },
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(jobs[i].Key.Line.Value - 1, jobs[i].Key.Column.Value - 1),
                            new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(jobs[i].Key.Line.Value - 1, jobs[i].Key.Column.Value - 1)
                        )
                    });
                    var rawstrategy = (jobs[i].Value as IReadOnlyObject)?["strategy"] as MappingToken;
                    var result = StrategyUtils.ExpandStrategy(rawstrategy, false, null, jobs[i].Key.ToString());

                    var flatmatrix = result.FlatMatrix;
                    var includematrix = result.IncludeMatrix;
                    bool failFast = result.FailFast;
                    double? max_parallel = result.MaxParallel;
                    var keys = result.MatrixKeys;
                    if(rawstrategy != null && result.Result == null) {
                        JArray allMatrices = new JArray();
                        Action<string, Dictionary<string, TemplateToken>> addAction = (suffix, item) => {
                            var matrixEntries = item.ToDictionary(kv => kv.Key, kv => kv.Value.ToContextData().ToJToken());
                            var json = JsonConvert.SerializeObject(matrixEntries);

                            if(allMatrices.Count < 2) {
                                codeLens.Add(new CodeLens { Command = new Command { Name = "runner.server.runjob", Title = $"{jobs[i].Key}{suffix}", Arguments = new Newtonsoft.Json.Linq.JArray(request.TextDocument.Uri.ToString(), $"{jobs[i].Key}({json})", new Newtonsoft.Json.Linq.JArray(events.ToArray())) },
                                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                        new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(rawstrategy.Line.Value - 1, rawstrategy.Column.Value - 1),
                                        new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(rawstrategy.Line.Value - 1, rawstrategy.Column.Value - 1)
                                    )
                                });
                            }
                            var entry = new JObject();
                            entry["name"] = $"{jobs[i].Key}{suffix}";
                            entry["jobId"] = $"{jobs[i].Key}";
                            entry["jobIdLong"] = $"{jobs[i].Key}({json})";
                            entry["matrix"] = JsonConvert.DeserializeObject<JObject>(json);
                            allMatrices.Add(entry);
                        };

                        if(keys.Count != 0 || includematrix.Count == 0) {
                            foreach (var item in flatmatrix) {
                                addAction(StrategyUtils.GetDefaultDisplaySuffix(from displayitem in keys.SelectMany(key => item[key].Traverse(true)) where !(displayitem is SequenceToken || displayitem is MappingToken) select displayitem.ToString()), item);
                            }
                        }
                        foreach (var item in includematrix) {
                            addAction(StrategyUtils.GetDefaultDisplaySuffix(from displayitem in item.SelectMany(it => it.Value.Traverse(true)) where !(displayitem is SequenceToken || displayitem is MappingToken) select displayitem.ToString()), item);
                        }

                        if(allMatrices.Count >= 2) {
                            codeLens.Add(new CodeLens { Command = new Command { Name = "runner.server.runjob", Title = "More", Arguments = new Newtonsoft.Json.Linq.JArray(request.TextDocument.Uri.ToString(), allMatrices, new Newtonsoft.Json.Linq.JArray(events.ToArray())) },
                                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                    new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(rawstrategy.Line.Value - 1, rawstrategy.Column.Value - 1),
                                    new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(rawstrategy.Line.Value - 1, rawstrategy.Column.Value - 1)
                                )
                            });
                        }
                    }
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
