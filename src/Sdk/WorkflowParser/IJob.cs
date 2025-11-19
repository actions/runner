#nullable enable

using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;
using Newtonsoft.Json;

namespace GitHub.Actions.WorkflowParser
{
    [JsonConverter(typeof(IJobJsonConverter))]
    public interface IJob
    {
        JobType Type
        {
            get;
        }

        StringToken? Id
        {
            get;
            set;
        }

        IList<StringToken> Needs
        {
            get;
        }

        public Permissions? Permissions
        {
            get;
            set;
        }

        IJob Clone(bool omitSource);
    }
}
