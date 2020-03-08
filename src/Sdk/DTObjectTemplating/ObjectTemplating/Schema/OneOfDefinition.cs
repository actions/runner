using System;
using System.Collections.Generic;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
{
    /// <summary>
    /// Must resolve to exactly one of the referenced definitions
    /// </summary>
    internal sealed class OneOfDefinition : Definition
    {
        internal OneOfDefinition()
        {
        }

        internal OneOfDefinition(MappingToken definition)
            : base(definition)
        {
            foreach (var definitionPair in definition)
            {
                var definitionKey = definitionPair.Key.AssertString($"{TemplateConstants.Definition} key");
                switch (definitionKey.Value)
                {
                    case TemplateConstants.OneOf:
                        var oneOf = definitionPair.Value.AssertSequence(TemplateConstants.OneOf);
                        foreach (var oneOfItem in oneOf)
                        {
                            var reference = oneOfItem.AssertString(TemplateConstants.OneOf);
                            OneOf.Add(reference.Value);
                        }
                        break;

                    default:
                        definitionKey.AssertUnexpectedValue($"{TemplateConstants.Definition} key");
                        break;
                }
            }
        }

        internal override DefinitionType DefinitionType => DefinitionType.Mapping;

        internal List<String> OneOf { get; } = new List<String>();

        internal override void Validate(
            TemplateSchema schema,
            String name)
        {
            if (OneOf.Count == 0)
            {
                throw new ArgumentException($"'{name}' does not contain any references");
            }

            var foundLooseKeyType = false;
            var mappingDefinitions = default(List<MappingDefinition>);
            var sequenceDefinition = default(SequenceDefinition);
            var nullDefinition = default(NullDefinition);
            var booleanDefinition = default(BooleanDefinition);
            var numberDefinition = default(NumberDefinition);
            var stringDefinitions = default(List<StringDefinition>);

            foreach (var nestedType in OneOf)
            {
                var nestedDefinition = schema.GetDefinition(nestedType);

                if (nestedDefinition.ReaderContext.Length > 0)
                {
                    throw new ArgumentException($"'{name}' is a one-of definition and references another definition that defines context. This is currently not supported.");
                }

                if (nestedDefinition is MappingDefinition mappingDefinition)
                {
                    if (mappingDefinitions == null)
                    {
                        mappingDefinitions = new List<MappingDefinition>();
                    }

                    mappingDefinitions.Add(mappingDefinition);

                    if (!String.IsNullOrEmpty(mappingDefinition.LooseKeyType))
                    {
                        foundLooseKeyType = true;
                    }
                }
                else if (nestedDefinition is SequenceDefinition s)
                {
                    // Multiple sequence definitions not allowed
                    if (sequenceDefinition != null)
                    {
                        throw new ArgumentException($"'{name}' refers to more than one '{TemplateConstants.Sequence}'");
                    }

                    sequenceDefinition = s;
                }
                else if (nestedDefinition is NullDefinition n)
                {
                    // Multiple sequence definitions not allowed
                    if (nullDefinition != null)
                    {
                        throw new ArgumentException($"'{name}' refers to more than one '{TemplateConstants.Null}'");
                    }

                    nullDefinition = n;
                }
                else if (nestedDefinition is BooleanDefinition b)
                {
                    // Multiple boolean definitions not allowed
                    if (booleanDefinition != null)
                    {
                        throw new ArgumentException($"'{name}' refers to more than one '{TemplateConstants.Boolean}'");
                    }

                    booleanDefinition = b;
                }
                else if (nestedDefinition is NumberDefinition num)
                {
                    // Multiple number definitions not allowed
                    if (numberDefinition != null)
                    {
                        throw new ArgumentException($"'{name}' refers to more than one '{TemplateConstants.Number}'");
                    }

                    numberDefinition = num;
                }
                else if (nestedDefinition is StringDefinition stringDefinition)
                {
                    // First string definition
                    if (stringDefinitions == null)
                    {
                        stringDefinitions = new List<StringDefinition>();
                    }
                    // Multiple string definitions, all must be 'Constant'
                    else if ((stringDefinitions.Count == 1 && String.IsNullOrEmpty(stringDefinitions[0].Constant))
                        || String.IsNullOrEmpty(stringDefinition.Constant))
                    {
                        throw new ArgumentException($"'{name}' refers to more than one '{TemplateConstants.Scalar}', but some do not set '{TemplateConstants.Constant}'");
                    }

                    stringDefinitions.Add(stringDefinition);
                }
                else
                {
                    throw new ArgumentException($"'{name}' refers to a '{nestedDefinition.DefinitionType}' definition");
                }
            }

            if (mappingDefinitions?.Count > 1)
            {
                if (foundLooseKeyType)
                {
                    throw new ArgumentException($"'{name}' refers to two mappings that both set '{TemplateConstants.LooseKeyType}'");
                }

                var seenProperties = new Dictionary<String, PropertyValue>(StringComparer.Ordinal);

                foreach (var mappingDefinition in mappingDefinitions)
                {
                    foreach (var newProperty in GetMergedProperties(schema, mappingDefinition))
                    {
                        // Already seen
                        if (seenProperties.TryGetValue(newProperty.Key, out PropertyValue existingProperty))
                        {
                            // Types match
                            if (String.Equals(existingProperty.Type, newProperty.Value.Type, StringComparison.Ordinal))
                            {
                                continue;
                            }

                            // Collision
                            throw new ArgumentException($"'{name}' contains two mappings with the same property, but each refers to a different type. All matching properties must refer to the same type.");
                        }
                        // New
                        else
                        {
                            seenProperties.Add(newProperty.Key, newProperty.Value);
                        }
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<String, PropertyValue>> GetMergedProperties(
            TemplateSchema schema,
            MappingDefinition mapping)
        {
            foreach (var property in mapping.Properties)
            {
                yield return property;
            }

            if (!String.IsNullOrEmpty(mapping.Inherits))
            {
                var inherited = schema.GetDefinition(mapping.Inherits) as MappingDefinition;

                if (!String.IsNullOrEmpty(inherited.Inherits))
                {
                    throw new NotSupportedException("Multiple levels of inheritance is not supported");
                }

                foreach (var property in inherited.Properties)
                {
                    if (!mapping.Properties.ContainsKey(property.Key))
                    {
                        yield return property;
                    }
                }
            }
        }
    }
}
