using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Runner.Server.Azure.Devops;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Linq;

public class RequiredParametersProvider : IRequiredParametersProvider {
    JSObject handle;

    public RequiredParametersProvider(JSObject handle) {
        this.handle = handle;
    }

    public async Task<TemplateToken> GetRequiredParameter(string name, string type, IEnumerable<string> enumerable) {
        var result = await Interop.RequestRequiredParameter(handle, name, type, enumerable?.ToArray());
        return AzurePipelinesUtils.ConvertStringToTemplateToken(result);
    }

    public Task ReportInvalidParameterValue(string name, string type, string message)
    {
        return Interop.Message(handle, 2, $"Provided value of {name} is not a valid {type}: {message}");
    }
}