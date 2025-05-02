using GitHub.DistributedTask.ObjectTemplating.Tokens;
using Runner.Server.Azure.Devops;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Linq;
using GitHub.DistributedTask.Pipelines.ContextData;
using Newtonsoft.Json;

public class ParametersProvider : IParametersProvider {
    JSObject handle;

    public ParametersProvider(JSObject handle) {
        this.handle = handle;
    }

    public async Task<TemplateToken> GetParameter(string name, string type, IEnumerable<string> enumerable, TemplateToken defaultValue) {
        var result = await Interop.RequestParameter(handle, name, type, enumerable?.ToArray(), JsonConvert.SerializeObject(defaultValue?.ToContextData()?.ToJToken()));
        return AzurePipelinesUtils.ConvertStringToTemplateToken(result);
    }

    public Task ReportInvalidParameterValue(string name, string type, string message)
    {
        if(type == null) {
            // implementation detail to stop the task for removed parameters
            return Interop.RequestParameter(handle, name, type, null, null).ContinueWith(t => {});
        }
        return Interop.Message(handle, 2, $"Provided value of {name} is not a valid {type}: {message}");
    }
}