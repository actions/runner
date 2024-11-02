using System;
using System.Text;

using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;

using Newtonsoft.Json;

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
    class Equality : IEqualityComparer<TemplateToken>
    {
        public bool PartialMatch { get; set; }

        public bool Equals(TemplateToken x, TemplateToken y)
        {
            return TemplateTokenEqual(x, y, PartialMatch);
        }

        public int GetHashCode(TemplateToken obj)
        {
            throw new NotImplementedException();
        }
    }
    private static Exception UnexpectedTemplateTokenType(TemplateToken token) {
        return new NotSupportedException($"Unexpected {nameof(TemplateToken)} type '{token.Type}'");
    }
    private static bool TemplateTokenEqual(TemplateToken token, TemplateToken other, bool partialMatch = false) {
        switch(token.Type) {
        case TokenType.Null:
        case TokenType.Boolean:
        case TokenType.Number:
        case TokenType.String:
            switch(other.Type) {
            case TokenType.Null:
            case TokenType.Boolean:
            case TokenType.Number:
            case TokenType.String:
                return EvaluationResult.CreateIntermediateResult(null, token).AbstractEqual(EvaluationResult.CreateIntermediateResult(null, other));
            case TokenType.Mapping:
            case TokenType.Sequence:
                return false;
            default:
                throw UnexpectedTemplateTokenType(other);
            }
        case TokenType.Mapping:
            switch(other.Type) {
            case TokenType.Mapping:
                break;
            case TokenType.Null:
            case TokenType.Boolean:
            case TokenType.Number:
            case TokenType.String:
            case TokenType.Sequence:
                return false;
            default:
                throw UnexpectedTemplateTokenType(other);
            }
            var mapping = token as MappingToken;
            var othermapping = other as MappingToken;
            if(partialMatch ? mapping.Count < othermapping.Count : mapping.Count != othermapping.Count) {
                return false;
            }
            Dictionary<string, TemplateToken> dictionary = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
            if (mapping.Count > 0)
            {
                foreach (var pair in mapping)
                {
                    var keyLiteral = pair.Key.AssertString("dictionary context data key");
                    var key = keyLiteral.Value;
                    var value = pair.Value;
                    dictionary.Add(key, value);
                }
                foreach (var pair in othermapping)
                {
                    var keyLiteral = pair.Key.AssertString("dictionary context data key");
                    var key = keyLiteral.Value;
                    var otherv = pair.Value;
                    TemplateToken value;
                    if(!dictionary.TryGetValue(key, out value) || !TemplateTokenEqual(value, otherv, partialMatch)) {
                        return false;
                    }
                }
            }
            return true;

        case TokenType.Sequence:
            switch(other.Type) {
            case TokenType.Sequence:
                break;
            case TokenType.Null:
            case TokenType.Boolean:
            case TokenType.Number:
            case TokenType.String:
            case TokenType.Mapping:
                return false;
            default:
                throw UnexpectedTemplateTokenType(other);
            }
            var sequence = token as SequenceToken;
            var otherseq = other as SequenceToken;
            if(partialMatch ? sequence.Count < otherseq.Count : sequence.Count != otherseq.Count) {
                return false;
            }
            return (partialMatch ? sequence.Take(otherseq.Count) : sequence).SequenceEqual(otherseq, new Equality() { PartialMatch = partialMatch });

        default:
            throw UnexpectedTemplateTokenType(token);
        }
    }

    string GetDefaultDisplaySuffix(IEnumerable<string> item) {
        var displaySuffix = new StringBuilder();
        int z = 0;
        foreach (var mk in item) {
            if(!string.IsNullOrEmpty(mk)) {
                displaySuffix.Append(z++ == 0 ? "(" : ", ");
                displaySuffix.Append(mk);
            }
        }
        if(z > 0) {
            displaySuffix.Append( ")");
        }
        return displaySuffix.ToString();
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
                            new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(jobs[i].Key.Line.Value - 1, jobs[i].Key.Column.Value - 1),
                            new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(jobs[i].Key.Line.Value - 1, jobs[i].Key.Column.Value - 1)
                        )
                    });
                    var rawstrategy = (jobs[i].Value as IReadOnlyObject)["strategy"] as MappingToken;
                    var flatmatrix = new List<Dictionary<string, TemplateToken>> { new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase) };
                    var includematrix = new List<Dictionary<string, TemplateToken>> { };
                    SequenceToken include = null;
                    SequenceToken exclude = null;
                    bool failFast = true;
                    double? max_parallel = null;
                    // Allow including and excluding via list properties https://github.com/orgs/community/discussions/7835
                    // https://github.com/actions/runner/issues/857
                    // Matrix has partial subobject matching reported here https://github.com/rhysd/actionlint/issues/249
                    // It also reveals that sequences are matched partially, if the left seqence starts with the right sequence they are matched
                    var jobname = "";
                    var matrixexcludeincludelists = false;//workflowContext.HasFeature("system.runner.server.matrixexcludeincludelists");
                    if (rawstrategy != null) {
                        // jobTraceWriter.Info("{0}", "Evaluate strategy");
                        // var templateContext = CreateTemplateContext(jobTraceWriter, workflowContext, contextData);
                        var strategy = rawstrategy;//GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.Strategy, rawstrategy, 0, null, true)?.AssertMapping($"jobs.{jobname}.strategy");
                        // templateContext.Errors.Check();
                        failFast = (from r in strategy where r.Key.AssertString($"jobs.{jobname}.strategy mapping key").Value == "fail-fast" select r).FirstOrDefault().Value?.AssertBoolean($"jobs.{jobname}.strategy.fail-fast")?.Value ?? failFast;
                        max_parallel = (from r in strategy where r.Key.AssertString($"jobs.{jobname}.strategy mapping key").Value == "max-parallel" select r).FirstOrDefault().Value?.AssertNumber($"jobs.{jobname}.strategy.max-parallel")?.Value;
                        var matrix = (from r in strategy where r.Key.AssertString($"jobs.{jobname}.strategy mapping key").Value == "matrix" select r).FirstOrDefault().Value?.AssertMapping($"jobs.{jobname}.strategy.matrix");
                        if(matrix != null) {
                            foreach (var item in matrix)
                            {
                                var key = item.Key.AssertString($"jobs.{jobname}.strategy.matrix mapping key").Value;
                                switch (key)
                                {
                                    case "include":
                                        include = item.Value?.AssertSequence($"jobs.{jobname}.strategy.matrix.include");
                                        break;
                                    case "exclude":
                                        exclude = item.Value?.AssertSequence($"jobs.{jobname}.strategy.matrix.exclude");
                                        break;
                                    default:
                                        var val = item.Value.AssertSequence($"jobs.{jobname}.strategy.matrix.{key}");
                                        var next = new List<Dictionary<string, TemplateToken>>();
                                        foreach (var mel in flatmatrix)
                                        {
                                            foreach (var n in val)
                                            {
                                                var ndict = new Dictionary<string, TemplateToken>(mel, StringComparer.OrdinalIgnoreCase);
                                                ndict.Add(key, n);
                                                next.Add(ndict);
                                            }
                                        }
                                        flatmatrix = next;
                                        break;
                                }
                            }
                            if (exclude != null)
                            {
                                foreach (var item in exclude)
                                {
                                    var map = item.AssertMapping($"jobs.{jobname}.strategy.matrix.exclude.*").ToDictionary(k => k.Key.AssertString($"jobs.{jobname}.strategy.matrix.exclude.* mapping key").Value, k => k.Value, StringComparer.OrdinalIgnoreCase);
                                    flatmatrix.RemoveAll(dict =>
                                    {
                                        foreach (var item in map)
                                        {
                                            TemplateToken val;
                                            if (!dict.TryGetValue(item.Key, out val))
                                            {
                                                // The official github actions service reject this matrix, return false would just ignore it
                                                throw new Exception($"Tried to exclude a matrix key {item.Key} which isn't defined by the matrix");
                                            }
                                            if (!(matrixexcludeincludelists && val is SequenceToken seq ? seq.Any(t => TemplateTokenEqual(t, item.Value, true)) : TemplateTokenEqual(val, item.Value, true))) {
                                                return false;
                                            }
                                        }
                                        return true;
                                    });
                                }
                            }
                        }
                        if(flatmatrix.Count == 0) {
                            // Fix empty matrix after exclude
                            flatmatrix.Add(new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase));
                        }
                    }
                    var keys = flatmatrix.First().Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if (include != null) {
                        foreach(var map in include.SelectMany(item => {
                            var map = item.AssertMapping($"jobs.{jobname}.strategy.matrix.include.*").ToDictionary(k => k.Key.AssertString($"jobs.{jobname}.strategy.matrix.include.* mapping key").Value, k => k.Value, StringComparer.OrdinalIgnoreCase);
                            if(matrixexcludeincludelists) {
                                var ret = new List<Dictionary<string, TemplateToken>>{ new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase) };
                                foreach(var m in map) {
                                    var next = new List<Dictionary<string, TemplateToken>>();
                                    var cur = next.ToArray();
                                    foreach(var n in ret) {
                                        var d = new Dictionary<string, TemplateToken>(n, StringComparer.OrdinalIgnoreCase);
                                        if(m.Value is SequenceToken seq) {
                                            foreach(var v in seq) {
                                                d[m.Key] = v;
                                            }
                                        } else {
                                            d[m.Key] = m.Value;
                                        }
                                        next.Add(d);
                                    }
                                    ret = next;
                                }
                                return ret.AsEnumerable();
                            } else {
                                return new[] { map }.AsEnumerable();
                            }
                        })) {
                            bool matched = false;
                            if(keys.Count > 0) {
                                flatmatrix.ForEach(dict => {
                                    foreach (var item in keys) {
                                        TemplateToken val;
                                        if (map.TryGetValue(item, out val) && !TemplateTokenEqual(dict[item], val, true)) {
                                            return;
                                        }
                                    }
                                    matched = true;
                                    foreach (var item in map) {
                                        if(!keys.Contains(item.Key)) {
                                            dict[item.Key] = item.Value;
                                        }
                                    }
                                });
                            }
                            if (!matched) {
                                includematrix.Add(map);
                            }
                        }
                    }

                    if(rawstrategy != null) {
                        Action<string, Dictionary<string, TemplateToken>> addAction = (suffix, item) => {
                            var json = JsonConvert.SerializeObject(item.ToDictionary(kv => kv.Key, kv => kv.Value.ToContextData().ToJToken()));

                            codeLens.Add(new CodeLens { Command = new Command { Name = "runner.server.runjob", Title = $"{jobs[i].Key}{suffix}", Arguments = new Newtonsoft.Json.Linq.JArray(request.TextDocument.Uri.ToString(), $"{jobs[i].Key}({json})") },
                                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                    new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(rawstrategy.Line.Value - 1, rawstrategy.Column.Value - 1),
                                    new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(rawstrategy.Line.Value - 1, rawstrategy.Column.Value - 1)
                                )
                            });
                        };

                        if(keys.Count != 0 || includematrix.Count == 0) {
                            foreach (var item in flatmatrix) {
                                addAction(GetDefaultDisplaySuffix(from displayitem in keys.SelectMany(key => item[key].Traverse(true)) where !(displayitem is SequenceToken || displayitem is MappingToken) select displayitem.ToString()), item);
                            }
                        }
                        foreach (var item in includematrix) {
                            addAction(GetDefaultDisplaySuffix(from displayitem in item.SelectMany(it => it.Value.Traverse(true)) where !(displayitem is SequenceToken || displayitem is MappingToken) select displayitem.ToString()), item);
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
