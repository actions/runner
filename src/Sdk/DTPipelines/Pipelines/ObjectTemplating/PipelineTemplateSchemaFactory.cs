using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Schema;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PipelineTemplateSchemaFactory
    {
        public static TemplateSchema GetSchema()
        {
            if (s_schema == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var json = default(String);
                using (var stream = assembly.GetManifestResourceStream("GitHub.DistributedTask.Pipelines.ObjectTemplating.workflow-v1.0.json"))
                using (var streamReader = new StreamReader(stream))
                {
                    json = streamReader.ReadToEnd();
                }

                var objectReader = new JsonObjectReader(null, json);
                var schema = TemplateSchema.Load(objectReader);
                Interlocked.CompareExchange(ref s_schema, schema, null);
            }

            return s_schema;
        }

        private static TemplateSchema s_schema;

        public static string LoadResource(string resource, Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }
            using (var stream = assembly.GetManifestResourceStream(resource))
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }
        public static TemplateSchema LoadSchema(string resource)
        {
            var assembly = Assembly.GetCallingAssembly();
            var json = LoadResource(resource, assembly);

            var objectReader = new JsonObjectReader(null, json);
            return TemplateSchema.Load(objectReader);
        }
    }
}
