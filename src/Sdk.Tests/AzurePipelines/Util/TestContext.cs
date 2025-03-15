using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace Runner.Server.Azure.Devops
{
    public class TestContext
    {
        public static TestContext Create(string rootDirectory)
        {
            return new TestContext(new LocalFileProvider(rootDirectory));
        }

        private LocalFileProvider fileProvider;
        private TestVariablesProvider variablesProvider;
        private GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter;


        internal TestContext(LocalFileProvider fileProvider)
        {
            this.fileProvider = fileProvider;
            variablesProvider = new TestVariablesProvider();
            traceWriter = new EmptyTraceWriter();
        }

        public TestContext SetWorkingDirectory(string localFolder)
        {
            fileProvider = new LocalFileProvider(localFolder);
            return this;
        }

        public TestContext AddRepo(string repositoryAndRef, string localFolder)
        {
            fileProvider.AddRepo(repositoryAndRef, localFolder);
            return this;
        }


        public TestContext AddTraceWriter(GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter)
        {
            this.traceWriter = traceWriter;
            return this;
        }

        public Context GetContext()
        {
            return new Context
            {
                FileProvider = fileProvider,
                VariablesProvider = variablesProvider,
                TraceWriter = traceWriter ?? new EmptyTraceWriter(),
                Flags = ExpressionFlags.DTExpressionsV1 | ExpressionFlags.ExtendedDirectives | ExpressionFlags.AllowAnyForInsert
            };
        }

        public Pipeline Evaluate(string fileRelativePath)
        {
            return EvaluateAsync(fileRelativePath).GetAwaiter().GetResult();
        }

        public async Task<Pipeline> EvaluateAsync(string fileRelativePath)
        {
            Context context = GetContext();

            var evaluatedRoot = await AzureDevops.ReadTemplate(context, fileRelativePath, null);

            var pline = await new Pipeline().Parse(context.ChildContext(evaluatedRoot, fileRelativePath), evaluatedRoot);
            pline.CheckPipelineForRuntimeFailure();
            return pline;
        }

        public TemplateToken ValidateSyntax(string fileRelativePath)
        {
            return ValidateSyntaxAsync(fileRelativePath).GetAwaiter().GetResult();
        }

        public async Task<TemplateToken> ValidateSyntaxAsync(string fileRelativePath)
        {
            Context context = GetContext();

            var (_, tkn) = await AzureDevops.ParseTemplate(context, fileRelativePath, null, checks: true);
            return tkn;
        }

        public void AutoComplete(string file, long? row, long? column, string[] autoCompletion)
        {
            AutoCompleteAsync(file, row, column, autoCompletion).GetAwaiter().GetResult();
        }

        public async Task AutoCompleteAsync(string file, long? row, long? column, string[] autoCompletion)
        {
            Context context = GetContext();
            context.Row = (int)(row ?? 0);
            context.Column = (int)(column ?? 0);
            context.RawMapping = true;
            context.AutoCompleteMatches = new List<AutoCompleteEntry>();
            try
            {
                await AzureDevops.ParseTemplate(context, file, null, checks: true);
            }
            catch (Exception)
            {
                // ignore
            }
            var list = AutoCompletetionHelper.CollectCompletions(context.Column, context.Row, context, AzureDevops.LoadSchema());
            foreach (var item in autoCompletion)
            {
                if (!list.Any(x => x.Label?.Label == item))
                { 
                    throw new Exception($"Expected AutoCompletion '{item}' not found in " + string.Join(", ", list.Select(x => x.Label?.Label)));
                }
            }
        }
    }
}
