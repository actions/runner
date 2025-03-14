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
    }
}
