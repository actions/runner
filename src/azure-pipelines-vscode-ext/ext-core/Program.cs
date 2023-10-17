using Runner.Server.Azure.Devops;
using System;
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
            if(!string.IsNullOrEmpty(repositoryAndRef)) {
                return await Interop.ReadFile(handle, $"{repositoryAndRef}/{path}");
            }
            return await Interop.ReadFile(handle, path);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> ExpandCurrentPipeline(JSObject handle, string currentFileName) {
        try {
            var context = new Runner.Server.Azure.Devops.Context {
                FileProvider = new MyFileProvider(handle),
                TraceWriter = new GitHub.DistributedTask.ObjectTemplating.EmptyTraceWriter(),
                Flags = GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives
            };
            var template = await AzureDevops.ReadTemplate(context, currentFileName);
            var pipeline = await new Runner.Server.Azure.Devops.Pipeline().Parse(context.ChildContext(template, currentFileName), template);
            return pipeline.ToYaml();
        } catch(Exception ex) {
            await Interop.Message(2, ex.ToString());
            return null;
        }
    }
}
