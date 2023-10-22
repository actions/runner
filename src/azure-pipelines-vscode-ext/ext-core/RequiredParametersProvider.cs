using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using Runner.Server.Azure.Devops;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.IO;
using System.Threading.Tasks;

public class RequiredParametersProvider : IRequiredParametersProvider {
    JSObject handle;

    public RequiredParametersProvider(JSObject handle) {
        this.handle = handle;
    }

    public static TemplateToken ConvertStringToTemplateToken(string res) {
        if(res == null) {
            return null;
        }
        var templateContext = AzureDevops.CreateTemplateContext(new EmptyTraceWriter(), new List<string>(), GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives);
        using (var stringReader = new StringReader(res))
        {
            var yamlObjectReader = new YamlObjectReader(null, stringReader, preserveString: true, forceAzurePipelines: true);
            var ret = TemplateReader.Read(templateContext, "any", yamlObjectReader, null, out _);
            templateContext.Errors.Check();
            return ret;
        }
    }

    public async Task<TemplateToken> GetRequiredParameter(string name) {
        return ConvertStringToTemplateToken(await Interop.RequestRequiredParameter(handle, name));
    }
}