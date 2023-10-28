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

    public async Task<TemplateToken> GetRequiredParameter(string name) {
        return AzurePipelinesUtils.ConvertStringToTemplateToken(await Interop.RequestRequiredParameter(handle, name));
    }
}