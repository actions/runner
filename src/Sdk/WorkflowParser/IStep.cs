#nullable enable

using Newtonsoft.Json;

namespace GitHub.Actions.WorkflowParser
{
    [JsonConverter(typeof(IStepJsonConverter))]
    public interface IStep
    {
        string? Id
        {
            get;
            set;
        }

        IStep Clone(bool omitSource);
    }
}
