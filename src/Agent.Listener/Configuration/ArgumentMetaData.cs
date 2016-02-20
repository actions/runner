using System;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
{
    public class ArgumentMetaData
    {
        public String Description { get; set; }
        public String DefaultValue { get; set; }
        public Boolean IsSercret { get; set; }
        public Func<String, bool> Validator { get; set; }
    }
}