using System.Collections.Generic;
using System.IO;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace Runner.Server.Azure.Devops {

    public class AzurePipelinesUtils {
        public static TemplateToken ConvertStringToTemplateToken(string res) {
            if(res == null) {
                return null;
            }
            var templateContext = AzureDevops.CreateTemplateContext(new EmptyTraceWriter(), new List<string>(), GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1 | GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives | GitHub.DistributedTask.Expressions2.ExpressionFlags.AllowAnyForInsert);
            using (var stringReader = new StringReader(res))
            {
                var yamlObjectReader = new YamlObjectReader(null, stringReader, preserveString: true, forceAzurePipelines: true);
                var ret = TemplateReader.Read(templateContext, "any", yamlObjectReader, null, out _);
                templateContext.Errors.Check();
                return ret;
            }
        }
        public static string YAMLToJson(string content) {
            return Newtonsoft.Json.JsonConvert.SerializeObject(ConvertStringToTemplateToken(content).ToContextData().ToJToken());
        }
    }
}