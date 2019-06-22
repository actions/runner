using System;
using Newtonsoft.Json.Linq;

namespace GitHub.Build.WebApi
{
    internal sealed class BuildProcessJsonConverter : TypePropertyJsonConverter<BuildProcess>
    {
        protected override BuildProcess GetInstance(
            Type objectType)
        {
            if (objectType == typeof(DesignerProcess))
            {
                return new DesignerProcess();
            }
            else if (objectType == typeof(YamlProcess))
            {
                return new YamlProcess();
            }
            else if (objectType == typeof(DockerProcess))
            {
                return new DockerProcess();
            }
            else if (objectType == typeof(JustInTimeProcess))
            {
                return new JustInTimeProcess();
            }
            else
            {
                return base.GetInstance(objectType);
            }
        }

        protected override BuildProcess GetInstance(
            Int32 targetType)
        {
            switch (targetType)
            {
                case ProcessType.Yaml:
                    return new YamlProcess();
                case ProcessType.Docker:
                    return new DockerProcess();
                case ProcessType.JustInTime:
                    return new JustInTimeProcess();
                case ProcessType.Designer:
                default:
                    return new DesignerProcess();
            }
        }

        protected override Boolean TryInferType(
            JObject value,
            out Int32 type)
        {
            // if it has a YamlFilename property, assume it's a YamlProcess
            if (value.TryGetValue("yamlFilename", StringComparison.OrdinalIgnoreCase, out JToken yamlFilename))
            {
                type = ProcessType.Yaml;
                return true;
            }
            else
            {
                // default to Designer process
                type = ProcessType.Designer;
                return true;
            }
        }
    }
}
