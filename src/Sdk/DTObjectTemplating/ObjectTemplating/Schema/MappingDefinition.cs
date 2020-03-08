using System;
using System.Collections.Generic;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
{
    internal sealed class MappingDefinition : Definition
    {
        internal MappingDefinition()
        {
        }

        internal MappingDefinition(MappingToken definition)
            : base(definition)
        {
            foreach (var definitionPair in definition)
            {
                var definitionKey = definitionPair.Key.AssertString($"{TemplateConstants.Definition} key");
                switch (definitionKey.Value)
                {
                    case TemplateConstants.Mapping:
                        var mapping = definitionPair.Value.AssertMapping($"{TemplateConstants.Definition} {TemplateConstants.Mapping}");
                        foreach (var mappingPair in mapping)
                        {
                            var mappingKey = mappingPair.Key.AssertString($"{TemplateConstants.Definition} {TemplateConstants.Mapping} key");
                            switch (mappingKey.Value)
                            {
                                case TemplateConstants.Properties:
                                    var properties = mappingPair.Value.AssertMapping($"{TemplateConstants.Definition} {TemplateConstants.Mapping} {TemplateConstants.Properties}");
                                    foreach (var propertiesPair in properties)
                                    {
                                        var propertyName = propertiesPair.Key.AssertString($"{TemplateConstants.Definition} {TemplateConstants.Mapping} {TemplateConstants.Properties} key");
                                        Properties.Add(propertyName.Value, new PropertyValue(propertiesPair.Value));
                                    }
                                    break;

                                case TemplateConstants.LooseKeyType:
                                    var looseKeyType = mappingPair.Value.AssertString($"{TemplateConstants.Definition} {TemplateConstants.Mapping} {TemplateConstants.LooseKeyType}");
                                    LooseKeyType = looseKeyType.Value;
                                    break;

                                case TemplateConstants.LooseValueType:
                                    var looseValueType = mappingPair.Value.AssertString($"{TemplateConstants.Definition} {TemplateConstants.Mapping} {TemplateConstants.LooseValueType}");
                                    LooseValueType = looseValueType.Value;
                                    break;

                                default:
                                    definitionKey.AssertUnexpectedValue($"{TemplateConstants.Definition} key");
                                    break;
                            }
                        }
                        break;

                    default:
                        definitionKey.AssertUnexpectedValue($"{TemplateConstants.Definition} key");
                        break;
                }
            }
        }

        internal override DefinitionType DefinitionType => DefinitionType.Mapping;

        internal String Inherits { get; set; }

        internal String LooseKeyType { get; set; }

        internal String LooseValueType { get; set; }

        internal Dictionary<String, PropertyValue> Properties { get; } = new Dictionary<String, PropertyValue>(StringComparer.Ordinal);

        internal override void Validate(
            TemplateSchema schema,
            String name)
        {
            // Lookup loose key type
            if (!String.IsNullOrEmpty(LooseKeyType))
            {
                schema.GetDefinition(LooseKeyType);

                // Lookup loose value type
                if (!String.IsNullOrEmpty(LooseValueType))
                {
                    schema.GetDefinition(LooseValueType);
                }
                else
                {
                    throw new ArgumentException($"Property '{TemplateConstants.LooseKeyType}' is defined but '{TemplateConstants.LooseValueType}' is not defined on '{name}'");
                }
            }
            // Otherwise validate loose value type not be defined
            else if (!String.IsNullOrEmpty(LooseValueType))
            {
                throw new ArgumentException($"Property '{TemplateConstants.LooseValueType}' is defined but '{TemplateConstants.LooseKeyType}' is not defined");
            }

            // Lookup each property
            foreach (var property in Properties)
            {
                if (String.IsNullOrEmpty(property.Value.Type))
                {
                    throw new ArgumentException($"Type not specified for the '{property.Key}' property on the '{name}' type");
                }

                schema.GetDefinition(property.Value.Type);
            }

            if (!String.IsNullOrEmpty(Inherits))
            {
                var inherited = schema.GetDefinition(Inherits);

                if (inherited.ReaderContext.Length > 0)
                {
                    throw new NotSupportedException($"Property '{TemplateConstants.Context}' is not supported on inhertied definitions");
                }

                if (inherited.DefinitionType != DefinitionType.Mapping)
                {
                    throw new NotSupportedException($"Expected structure of inherited definition to match. Actual '{inherited.DefinitionType}'");
                }

                var inheritedMapping = inherited as MappingDefinition;

                if (!String.IsNullOrEmpty(inheritedMapping.Inherits))
                {
                    throw new NotSupportedException($"Property '{TemplateConstants.Inherits}' is not supported on inherited definition");
                }

                if (!String.IsNullOrEmpty(inheritedMapping.LooseKeyType))
                {
                    throw new NotSupportedException($"Property '{TemplateConstants.LooseKeyType}' is not supported on inherited definition");
                }

                if (!String.IsNullOrEmpty(inheritedMapping.LooseValueType))
                {
                    throw new NotSupportedException($"Property '{TemplateConstants.LooseValueType}' is not supported on inherited definition");
                }
            }
        }
    }
}
