using System;
using System.IO;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    internal sealed class ParseResult
    {
        public TemplateContext Context { get; set; }

        public TemplateToken Value { get; set; }

        public String ToYaml()
        {
            if (Value == null)
            {
                return null;
            }

            // Serialize
            using (var stringWriter = new StringWriter())
            {
                TemplateWriter.Write(new YamlObjectWriter(stringWriter), Value);
                stringWriter.Flush();
                return stringWriter.ToString();
            }
        }
    }
}
