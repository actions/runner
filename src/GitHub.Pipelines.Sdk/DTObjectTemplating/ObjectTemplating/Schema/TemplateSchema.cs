using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Schema
{
    /// <summary>
    /// This models the root schema object and contains definitions
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class TemplateSchema
    {
        internal TemplateSchema()
            : this(null)
        {
        }

        private TemplateSchema(MappingToken mapping)
        {
            // Add built-in type: scalar
            var scalarDefinition = new ScalarDefinition();
            Definitions.Add(TemplateConstants.Scalar, scalarDefinition);

            // Add built-in type: sequence
            var sequenceDefinition = new SequenceDefinition { ItemType = TemplateConstants.Any };
            Definitions.Add(TemplateConstants.Sequence, sequenceDefinition);

            // Add built-in type: mapping
            var mappingDefinition = new MappingDefinition { LooseKeyType = TemplateConstants.Scalar, LooseValueType = TemplateConstants.Any };
            Definitions.Add(TemplateConstants.Mapping, mappingDefinition);

            // Add built-in type: any
            var anyDefinition = new OneOfDefinition();
            anyDefinition.OneOf.Add(TemplateConstants.Scalar);
            anyDefinition.OneOf.Add(TemplateConstants.Sequence);
            anyDefinition.OneOf.Add(TemplateConstants.Mapping);
            Definitions.Add(TemplateConstants.Any, anyDefinition);

            if (mapping != null)
            {
                foreach (var pair in mapping)
                {
                    var key = TemplateUtil.AssertLiteral(pair.Key, $"{TemplateConstants.TemplateSchema} key");
                    switch (key.Value)
                    {
                        case TemplateConstants.Version:
                            var version = TemplateUtil.AssertLiteral(pair.Value, TemplateConstants.Version);
                            Version = version.Value;
                            break;

                        case TemplateConstants.Definitions:
                            var definitions = TemplateUtil.AssertMapping(pair.Value, TemplateConstants.Definitions);
                            foreach (var definitionsPair in definitions)
                            {
                                var definitionsKey = TemplateUtil.AssertLiteral(definitionsPair.Key, $"{TemplateConstants.Definitions} key");
                                var definitionsValue = TemplateUtil.AssertMapping(definitionsPair.Value, TemplateConstants.Definition);
                                var definition = default(Definition);
                                foreach (var definitionPair in definitionsValue)
                                {
                                    var definitionKey = TemplateUtil.AssertLiteral(definitionPair.Key, $"{TemplateConstants.Definition} key");
                                    switch (definitionKey.Value)
                                    {
                                        case TemplateConstants.Scalar:
                                            definition = new ScalarDefinition(definitionsValue);
                                            break;

                                        case TemplateConstants.Sequence:
                                            definition = new SequenceDefinition(definitionsValue);
                                            break;

                                        case TemplateConstants.Mapping:
                                            definition = new MappingDefinition(definitionsValue);
                                            break;

                                        case TemplateConstants.OneOf:
                                            definition = new OneOfDefinition(definitionsValue);
                                            break;

                                        case TemplateConstants.Context:
                                            continue;

                                        default:
                                            TemplateUtil.AssertUnexpectedValue(definitionKey, "definition mapping key"); // throws
                                            break;
                                    }

                                    break;
                                }

                                if (definition == null)
                                {
                                    throw new ArgumentException($"Unable to determine definition details. Specify the '{TemplateConstants.Structure}' property");
                                }

                                Definitions.Add(definitionsKey.Value, definition);
                            }
                            break;

                        default:
                            TemplateUtil.AssertUnexpectedValue(key, $"{TemplateConstants.TemplateSchema} key"); // throws
                            break;
                    }
                }
            }
        }

        internal Dictionary<String, Definition> Definitions { get; } = new Dictionary<String, Definition>(StringComparer.Ordinal);

        internal String Version { get; }

        /// <summary>
        /// Loads a user's schema file
        /// </summary>
        internal static TemplateSchema Load(IObjectReader objectReader)
        {
            var context = new TemplateContext
            {
                CancellationToken = CancellationToken.None,
                Errors = new TemplateValidationErrors(maxErrors: 10, maxMessageLength: 500),
                Memory = new TemplateMemory(
                    maxDepth: 50,
                    maxEvents: 1000000, // 1 million
                    maxBytes: 1024 * 1024), // 1 mb
                TraceWriter = new EmptyTraceWriter(),
            };

            var value = TemplateReader.Read(context, TemplateConstants.TemplateSchema, objectReader, null, Schema, out _);

            if (context.Errors.Count > 0)
            {
                throw new TemplateValidationException(context.Errors);
            }

            var mapping = TemplateUtil.AssertMapping(value, TemplateConstants.TemplateSchema);
            var schema = new TemplateSchema(mapping);
            schema.Validate();
            return schema;
        }

        internal IEnumerable<T> Get<T>(Definition definition)
            where T : Definition
        {
            if (definition is T match)
            {
                yield return match;
            }
            else if (definition is OneOfDefinition oneOf)
            {
                foreach (var reference in oneOf.OneOf)
                {
                    var nestedDefinition = GetDefinition(reference);
                    if (nestedDefinition is T match2)
                    {
                        yield return match2;
                    }
                }
            }
        }

        internal Definition GetDefinition(String type)
        {
            if (Definitions.TryGetValue(type, out Definition value))
            {
                return value;
            }

            throw new ArgumentException($"Schema definition '{type}' not found");
        }

        internal Boolean HasProperties(MappingDefinition definition)
        {
            for (int i = 0; i < 10; i++)
            {
                if (definition.Properties.Count > 0)
                {
                    return true;
                }

                if (String.IsNullOrEmpty(definition.Inherits))
                {
                    return false;
                }

                definition = GetDefinition(definition.Inherits) as MappingDefinition;
            }

            throw new InvalidOperationException("Inheritance depth exceeded 10");
        }

        internal Boolean TryGetProperty(
            MappingDefinition definition,
            String name,
            out String type)
        {
            for (int i = 0; i < 10; i++)
            {
                if (definition.Properties.TryGetValue(name, out PropertyValue property))
                {
                    type = property.Type;
                    return true;
                }

                if (String.IsNullOrEmpty(definition.Inherits))
                {
                    type = default;
                    return false;
                }

                definition = GetDefinition(definition.Inherits) as MappingDefinition;
            }

            throw new InvalidOperationException("Inheritance depth exceeded 10");
        }

        internal Boolean TryMatchKey(
            List<MappingDefinition> definitions,
            String key,
            out String valueType)
        {
            valueType = null;

            // Check for a matching well known property
            var notFoundInSome = false;
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];

                if (TryGetProperty(definition, key, out String t))
                {
                    if (valueType == null)
                    {
                        valueType = t;
                    }
                }
                else
                {
                    notFoundInSome = true;
                }
            }

            // Check if found
            if (valueType != null)
            {
                // Filter the matched definitions if needed
                if (notFoundInSome)
                {
                    for (var i = 0; i < definitions.Count;)
                    {
                        if (TryGetProperty(definitions[i], key, out _))
                        {
                            i++;
                        }
                        else
                        {
                            definitions.RemoveAt(i);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// The built-in schema for reading schema files
        /// </summary>
        private static TemplateSchema Schema
        {
            get
            {
                if (s_schema == null)
                {
                    var schema = new TemplateSchema();

                    ScalarDefinition scalarDefinition;
                    SequenceDefinition sequenceDefinition;
                    MappingDefinition mappingDefinition;
                    OneOfDefinition oneOfDefinition;

                    // template-schema
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Version, new PropertyValue(TemplateConstants.NonEmptyScalar));
                    mappingDefinition.Properties.Add(TemplateConstants.Definitions, new PropertyValue(TemplateConstants.Definitions));
                    schema.Definitions.Add(TemplateConstants.TemplateSchema, mappingDefinition);

                    // definitions
                    mappingDefinition = new MappingDefinition { LooseKeyType = TemplateConstants.NonEmptyScalar, LooseValueType = TemplateConstants.Definition };
                    schema.Definitions.Add(TemplateConstants.Definitions, mappingDefinition);

                    // definition
                    oneOfDefinition = new OneOfDefinition();
                    oneOfDefinition.OneOf.Add(TemplateConstants.ScalarDefinition);
                    oneOfDefinition.OneOf.Add(TemplateConstants.SequenceDefinition);
                    oneOfDefinition.OneOf.Add(TemplateConstants.MappingDefinition);
                    oneOfDefinition.OneOf.Add(TemplateConstants.OneOfDefinition);
                    schema.Definitions.Add(TemplateConstants.Definition, oneOfDefinition);

                    // scalar-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(TemplateConstants.SequenceOfNonEmptyScalar));
                    mappingDefinition.Properties.Add(TemplateConstants.Scalar, new PropertyValue(TemplateConstants.ScalarDefinitionProperties));
                    schema.Definitions.Add(TemplateConstants.ScalarDefinition, mappingDefinition);

                    // scalar-definition-properties
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Constant, new PropertyValue(TemplateConstants.NonEmptyScalar));
                    mappingDefinition.Properties.Add(TemplateConstants.IgnoreCase, new PropertyValue(TemplateConstants.Boolean));
                    mappingDefinition.Properties.Add(TemplateConstants.RequireNonEmpty, new PropertyValue(TemplateConstants.Boolean));
                    schema.Definitions.Add(TemplateConstants.ScalarDefinitionProperties, mappingDefinition);

                    // sequence-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(TemplateConstants.SequenceOfNonEmptyScalar));
                    mappingDefinition.Properties.Add(TemplateConstants.Sequence, new PropertyValue(TemplateConstants.SequenceDefinitionProperties));
                    schema.Definitions.Add(TemplateConstants.SequenceDefinition, mappingDefinition);

                    // sequence-definition-properties
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.ItemType, new PropertyValue(TemplateConstants.NonEmptyScalar));
                    schema.Definitions.Add(TemplateConstants.SequenceDefinitionProperties, mappingDefinition);

                    // mapping-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(TemplateConstants.SequenceOfNonEmptyScalar));
                    mappingDefinition.Properties.Add(TemplateConstants.Mapping, new PropertyValue(TemplateConstants.MappingDefinitionProperties));
                    schema.Definitions.Add(TemplateConstants.MappingDefinition, mappingDefinition);

                    // mapping-definition-properties
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Properties, new PropertyValue(TemplateConstants.Properties));
                    mappingDefinition.Properties.Add(TemplateConstants.LooseKeyType, new PropertyValue(TemplateConstants.NonEmptyScalar));
                    mappingDefinition.Properties.Add(TemplateConstants.LooseValueType, new PropertyValue(TemplateConstants.NonEmptyScalar));
                    schema.Definitions.Add(TemplateConstants.MappingDefinitionProperties, mappingDefinition);

                    // properties
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.LooseKeyType = TemplateConstants.NonEmptyScalar;
                    mappingDefinition.LooseValueType = TemplateConstants.NonEmptyScalar;
                    schema.Definitions.Add(TemplateConstants.Properties, mappingDefinition);

                    // one-of-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(TemplateConstants.SequenceOfNonEmptyScalar));
                    mappingDefinition.Properties.Add(TemplateConstants.OneOf, new PropertyValue(TemplateConstants.SequenceOfNonEmptyScalar));
                    schema.Definitions.Add(TemplateConstants.OneOfDefinition, mappingDefinition);

                    // scalar-constant
                    scalarDefinition = new ScalarDefinition();
                    scalarDefinition.Constant = TemplateConstants.Scalar;
                    schema.Definitions.Add(TemplateConstants.ScalarConstant, scalarDefinition);

                    // sequence-constant
                    scalarDefinition = new ScalarDefinition();
                    scalarDefinition.Constant = TemplateConstants.Sequence;
                    schema.Definitions.Add(TemplateConstants.SequenceConstant, scalarDefinition);

                    // mapping-constant
                    scalarDefinition = new ScalarDefinition();
                    scalarDefinition.Constant = TemplateConstants.Mapping;
                    schema.Definitions.Add(TemplateConstants.MappingConstant, scalarDefinition);

                    // one-of-constant
                    scalarDefinition = new ScalarDefinition();
                    scalarDefinition.Constant = TemplateConstants.OneOf;
                    schema.Definitions.Add(TemplateConstants.OneOfConstant, scalarDefinition);

                    // boolean
                    oneOfDefinition = new OneOfDefinition();
                    oneOfDefinition.OneOf.Add(TemplateConstants.TrueConstant);
                    oneOfDefinition.OneOf.Add(TemplateConstants.FalseConstant);
                    schema.Definitions.Add(TemplateConstants.Boolean, oneOfDefinition);

                    // true-constant
                    scalarDefinition = new ScalarDefinition();
                    scalarDefinition.Constant = TemplateConstants.True;
                    schema.Definitions.Add(TemplateConstants.TrueConstant, scalarDefinition);

                    // false-constant
                    scalarDefinition = new ScalarDefinition();
                    scalarDefinition.Constant = TemplateConstants.False;
                    schema.Definitions.Add(TemplateConstants.FalseConstant, scalarDefinition);

                    // non-empty-scalar
                    scalarDefinition = new ScalarDefinition();
                    scalarDefinition.RequireNonEmpty = true;
                    schema.Definitions.Add(TemplateConstants.NonEmptyScalar, scalarDefinition);

                    // sequence-of-non-empty-scalar
                    sequenceDefinition = new SequenceDefinition();
                    sequenceDefinition.ItemType = TemplateConstants.NonEmptyScalar;
                    schema.Definitions.Add(TemplateConstants.SequenceOfNonEmptyScalar, sequenceDefinition);

                    schema.Validate();

                    Interlocked.CompareExchange(ref s_schema, schema, null);
                }

                return s_schema;
            }
        }

        private void Validate()
        {
            var oneOfPairs = new List<KeyValuePair<String, OneOfDefinition>>();

            foreach (var pair in Definitions)
            {
                var name = pair.Key;

                if (!s_definitionNameRegex.IsMatch(name ?? String.Empty))
                {
                    throw new ArgumentException($"Invalid definition name '{name}'");
                }

                var definition = pair.Value;

                // Delay validation for 'one-of' definitions
                if (definition is OneOfDefinition oneOf)
                {
                    oneOfPairs.Add(new KeyValuePair<String, OneOfDefinition>(name, oneOf));
                }
                // Otherwise validate now
                else
                {
                    definition.Validate(this, name);
                }
            }

            // Validate 'one-of' definitions
            foreach (var pair in oneOfPairs)
            {
                var name = pair.Key;
                var oneOf = pair.Value;
                oneOf.Validate(this, name);
            }
        }

        private static readonly Regex s_definitionNameRegex = new Regex("^[a-zA-Z_][a-zA-Z0-9_-]*$", RegexOptions.Compiled);
        private static TemplateSchema s_schema;
    }
}