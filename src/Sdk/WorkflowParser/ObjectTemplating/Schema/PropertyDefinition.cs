#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating.Schema
{
    internal sealed class PropertyDefinition
    {
        internal PropertyDefinition(TemplateToken token)
        {
            if (token is StringToken stringToken)
            {
                Type = stringToken.Value;
            }
            else
            {
                var mapping = token.AssertMapping($"{TemplateConstants.MappingPropertyValue}");
                foreach (var mappingPair in mapping)
                {
                    var mappingKey = mappingPair.Key.AssertString($"{TemplateConstants.MappingPropertyValue} key");
                    switch (mappingKey.Value)
                    {
                        case TemplateConstants.Type:
                            Type = mappingPair.Value.AssertString($"{TemplateConstants.MappingPropertyValue} {TemplateConstants.Type}").Value;
                            break;
                        case TemplateConstants.Required:
                            Required = mappingPair.Value.AssertBoolean($"{TemplateConstants.MappingPropertyValue} {TemplateConstants.Required}").Value;
                            break;
                        case TemplateConstants.Description:
                            Description = mappingPair.Value.AssertString($"{TemplateConstants.MappingPropertyValue} {TemplateConstants.Description}").Value;
                            break;
                        default:
                            mappingKey.AssertUnexpectedValue($"{TemplateConstants.MappingPropertyValue} key");
                            break;
                    }
                }
            }
        }

        internal String Type { get; set; }

        internal Boolean Required { get; set; }

        internal String Description { get; set; }
    }
}