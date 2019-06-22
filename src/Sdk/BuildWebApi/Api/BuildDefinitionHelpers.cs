using System;
using System.IO;
using System.Text;
using GitHub.Build.WebApi.Internals;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Build.WebApi
{
    public static class BuildDefinitionHelpers
    {
        public static BuildDefinition Deserialize(
            String definitionString)
        {
            var definition = JsonUtility.FromString<BuildDefinition>(definitionString);
            if (definition?.Process == null)
            {
                var legacyDefinition = JsonConvert.DeserializeObject<BuildDefinition3_2>(definitionString);
                definition = legacyDefinition.ToBuildDefinition();
            }

            return definition;
        }

        public static BuildDefinitionTemplate GetTemplateFromStream(
            Stream stream)
        {
            String templateString;
            using (var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            {
                templateString = reader.ReadToEnd();
            }

            var template = JsonConvert.DeserializeObject<BuildDefinitionTemplate>(templateString);
            if (template?.Template?.Process == null)
            {
                var legacyTemplate = JsonConvert.DeserializeObject<BuildDefinitionTemplate3_2>(templateString);
                template = legacyTemplate.ToBuildDefinitionTemplate();
            }

            return template;
        }
    }
}
