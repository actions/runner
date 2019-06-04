using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using GitHub.DistributedTask.ObjectTemplating.Schema;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PipelineTemplateSchemaFactory
    {
        public TemplateSchema CreateSchema()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var json = default(String);
            using (var stream = assembly.GetManifestResourceStream("GitHub.DistributedTask.Pipelines.ObjectTemplating.workflow-v1.0.json"))
            using (var streamReader = new StreamReader(stream))
            {
                json = streamReader.ReadToEnd();
            }

            var objectReader = new JsonObjectReader(json);
            return TemplateSchema.Load(objectReader);
        }
    }
}
