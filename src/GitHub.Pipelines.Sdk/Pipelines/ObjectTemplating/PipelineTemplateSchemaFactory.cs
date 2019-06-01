using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Schema;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.ObjectTemplating
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PipelineTemplateSchemaFactory
    {
        public TemplateSchema CreateSchema()
        {
            var assembly = Assembly.GetExecutingAssembly();

            File.WriteAllText(@"C:\Agent.CoreCLR\_layout\_diag\log.txt", assembly.FullName);
            var json = default(String);
            using (var stream = assembly.GetManifestResourceStream("Microsoft.TeamFoundation.DistributedTask.Pipelines.ObjectTemplating.workflow-v1.0.json"))
            using (var streamReader = new StreamReader(stream))
            {
                json = streamReader.ReadToEnd();
            }

            var objectReader = new JsonObjectReader(json);
            return TemplateSchema.Load(objectReader);
        }
    }
}
