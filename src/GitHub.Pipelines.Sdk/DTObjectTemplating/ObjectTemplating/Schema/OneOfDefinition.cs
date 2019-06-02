using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Schema
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
                var definitionKey = TemplateUtil.AssertLiteral(definitionPair.Key, $"{TemplateConstants.Definition} key");
                switch (definitionKey.Value)
                {
                    case TemplateConstants.OneOf:
                        var oneOf = TemplateUtil.AssertSequence(definitionPair.Value, TemplateConstants.OneOf);
                        foreach (var oneOfItem in oneOf)
                        {
                            var reference = TemplateUtil.AssertLiteral(oneOfItem, TemplateConstants.OneOf);
                            OneOf.Add(reference.Value);
                        }
                        break;

                    default:
                        TemplateUtil.AssertUnexpectedValue(definitionKey, $"{TemplateConstants.Definition} key");
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
            List<MappingDefinition> mappings = null;
            SequenceDefinition sequence = null;
            List<ScalarDefinition> scalars = null;

            foreach (var nestedType in OneOf)
            {
                var nestedDefinition = schema.GetDefinition(nestedType);

                if (nestedDefinition.Context.Length > 0)
                {
                    throw new ArgumentException($"'{name}' is a one-of definition and references another definition that defines context. This is currently not supported.");
                }

                if (nestedDefinition is MappingDefinition mapping)
                {
                    if (mappings == null)
                    {
                        mappings = new List<MappingDefinition>();
                    }

                    mappings.Add(mapping);

                    if (!String.IsNullOrEmpty(mapping.LooseKeyType))
                    {
                        foundLooseKeyType = true;
                    }
                }
                else if (nestedDefinition is SequenceDefinition s)
                {
                    // Multiple sequences not allowed
                    if (sequence != null)
                    {
                        throw new ArgumentException($"'{name}' refers to more than one '{TemplateConstants.Sequence}'");
                    }

                    sequence = s;
                }
                else if (nestedDefinition is ScalarDefinition scalar)
                {
                    // First scalar
                    if (scalars == null)
                    {
                        scalars = new List<ScalarDefinition>();
                    }
                    // Multiple scalars, all must be 'Constant'
                    else if ((scalars.Count == 1 && String.IsNullOrEmpty(scalars[0].Constant))
                        || String.IsNullOrEmpty(scalar.Constant))
                    {
                        throw new ArgumentException($"'{name}' refers to more than one '{TemplateConstants.Scalar}', but some do not set '{TemplateConstants.Constant}'");
                    }

                    scalars.Add(scalar);
                }
                else
                {
                    throw new ArgumentException($"'{name}' refers to a '{nestedDefinition.DefinitionType}' definition");
                }
            }

            if (mappings?.Count > 1)
            {
                if (foundLooseKeyType)
                {
                    throw new ArgumentException($"'{name}' refers to two mappings that both set '{TemplateConstants.LooseKeyType}'");
                }

                var seenProperties = new Dictionary<String, PropertyValue>(StringComparer.Ordinal);

                foreach (var mapping in mappings)
                {
                    foreach (var newProperty in GetMergedProperties(schema, mapping))
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