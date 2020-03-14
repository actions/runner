using System;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
{
    internal sealed class PropertyValue
    {
        internal PropertyValue(TemplateToken token)
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
                        default:
                            mappingKey.AssertUnexpectedValue($"{TemplateConstants.MappingPropertyValue} key");
                            break;
                    }
                }
            }
        }

        internal String Type { get; set; }

        internal Boolean Required { get; set; }
    }
}
