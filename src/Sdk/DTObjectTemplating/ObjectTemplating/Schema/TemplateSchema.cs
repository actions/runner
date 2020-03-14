using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
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
            // Add built-in type: null
            var nullDefinition = new NullDefinition();
            Definitions.Add(TemplateConstants.Null, nullDefinition);

            // Add built-in type: boolean
            var booleanDefinition = new BooleanDefinition();
            Definitions.Add(TemplateConstants.Boolean, booleanDefinition);

            // Add built-in type: number
            var numberDefinition = new NumberDefinition();
            Definitions.Add(TemplateConstants.Number, numberDefinition);

            // Add built-in type: string
            var stringDefinition = new StringDefinition();
            Definitions.Add(TemplateConstants.String, stringDefinition);

            // Add built-in type: sequence
            var sequenceDefinition = new SequenceDefinition { ItemType = TemplateConstants.Any };
            Definitions.Add(TemplateConstants.Sequence, sequenceDefinition);

            // Add built-in type: mapping
            var mappingDefinition = new MappingDefinition { LooseKeyType = TemplateConstants.String, LooseValueType = TemplateConstants.Any };
            Definitions.Add(TemplateConstants.Mapping, mappingDefinition);

            // Add built-in type: any
            var anyDefinition = new OneOfDefinition();
            anyDefinition.OneOf.Add(TemplateConstants.Null);
            anyDefinition.OneOf.Add(TemplateConstants.Boolean);
            anyDefinition.OneOf.Add(TemplateConstants.Number);
            anyDefinition.OneOf.Add(TemplateConstants.String);
            anyDefinition.OneOf.Add(TemplateConstants.Sequence);
            anyDefinition.OneOf.Add(TemplateConstants.Mapping);
            Definitions.Add(TemplateConstants.Any, anyDefinition);

            if (mapping != null)
            {
                foreach (var pair in mapping)
                {
                    var key = pair.Key.AssertString($"{TemplateConstants.TemplateSchema} key");
                    switch (key.Value)
                    {
                        case TemplateConstants.Version:
                            var version = pair.Value.AssertString(TemplateConstants.Version);
                            Version = version.Value;
                            break;

                        case TemplateConstants.Definitions:
                            var definitions = pair.Value.AssertMapping(TemplateConstants.Definitions);
                            foreach (var definitionsPair in definitions)
                            {
                                var definitionsKey = definitionsPair.Key.AssertString($"{TemplateConstants.Definitions} key");
                                var definitionsValue = definitionsPair.Value.AssertMapping(TemplateConstants.Definition);
                                var definition = default(Definition);
                                foreach (var definitionPair in definitionsValue)
                                {
                                    var definitionKey = definitionPair.Key.AssertString($"{TemplateConstants.Definition} key");
                                    switch (definitionKey.Value)
                                    {
                                        case TemplateConstants.Null:
                                            definition = new NullDefinition(definitionsValue);
                                            break;

                                        case TemplateConstants.Boolean:
                                            definition = new BooleanDefinition(definitionsValue);
                                            break;

                                        case TemplateConstants.Number:
                                            definition = new NumberDefinition(definitionsValue);
                                            break;

                                        case TemplateConstants.String:
                                            definition = new StringDefinition(definitionsValue);
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
                                        case TemplateConstants.Description:
                                            continue;

                                        default:
                                            definitionKey.AssertUnexpectedValue("definition mapping key"); // throws
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
                            key.AssertUnexpectedValue($"{TemplateConstants.TemplateSchema} key"); // throws
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

            var mapping = value.AssertMapping(TemplateConstants.TemplateSchema);
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

                    StringDefinition stringDefinition;
                    SequenceDefinition sequenceDefinition;
                    MappingDefinition mappingDefinition;
                    OneOfDefinition oneOfDefinition;

                    // template-schema
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Version, new PropertyValue(new StringToken(null, null, null, TemplateConstants.NonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.Definitions, new PropertyValue(new StringToken(null, null, null, TemplateConstants.Definitions)));
                    schema.Definitions.Add(TemplateConstants.TemplateSchema, mappingDefinition);

                    // definitions
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.LooseKeyType = TemplateConstants.NonEmptyString;
                    mappingDefinition.LooseValueType = TemplateConstants.Definition;
                    schema.Definitions.Add(TemplateConstants.Definitions, mappingDefinition);

                    // definition
                    oneOfDefinition = new OneOfDefinition();
                    oneOfDefinition.OneOf.Add(TemplateConstants.NullDefinition);
                    oneOfDefinition.OneOf.Add(TemplateConstants.BooleanDefinition);
                    oneOfDefinition.OneOf.Add(TemplateConstants.NumberDefinition);
                    oneOfDefinition.OneOf.Add(TemplateConstants.StringDefinition);
                    oneOfDefinition.OneOf.Add(TemplateConstants.SequenceDefinition);
                    oneOfDefinition.OneOf.Add(TemplateConstants.MappingDefinition);
                    oneOfDefinition.OneOf.Add(TemplateConstants.OneOfDefinition);
                    schema.Definitions.Add(TemplateConstants.Definition, oneOfDefinition);

                    // null-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Description, new PropertyValue(new StringToken(null, null, null, TemplateConstants.String)));
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(new StringToken(null, null, null, TemplateConstants.SequenceOfNonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.Null, new PropertyValue(new StringToken(null, null, null, TemplateConstants.NullDefinitionProperties)));
                    schema.Definitions.Add(TemplateConstants.NullDefinition, mappingDefinition);

                    // null-definition-properties
                    mappingDefinition = new MappingDefinition();
                    schema.Definitions.Add(TemplateConstants.NullDefinitionProperties, mappingDefinition);

                    // boolean-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Description, new PropertyValue(new StringToken(null, null, null, TemplateConstants.String)));
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(new StringToken(null, null, null, TemplateConstants.SequenceOfNonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.Boolean, new PropertyValue(new StringToken(null, null, null, TemplateConstants.BooleanDefinitionProperties)));
                    schema.Definitions.Add(TemplateConstants.BooleanDefinition, mappingDefinition);

                    // boolean-definition-properties
                    mappingDefinition = new MappingDefinition();
                    schema.Definitions.Add(TemplateConstants.BooleanDefinitionProperties, mappingDefinition);

                    // number-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Description, new PropertyValue(new StringToken(null, null, null, TemplateConstants.String)));
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(new StringToken(null, null, null, TemplateConstants.SequenceOfNonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.Number, new PropertyValue(new StringToken(null, null, null, TemplateConstants.NumberDefinitionProperties)));
                    schema.Definitions.Add(TemplateConstants.NumberDefinition, mappingDefinition);

                    // number-definition-properties
                    mappingDefinition = new MappingDefinition();
                    schema.Definitions.Add(TemplateConstants.NumberDefinitionProperties, mappingDefinition);

                    // string-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Description, new PropertyValue(new StringToken(null, null, null, TemplateConstants.String)));
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(new StringToken(null, null, null, TemplateConstants.SequenceOfNonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.String, new PropertyValue(new StringToken(null, null, null, TemplateConstants.StringDefinitionProperties)));
                    schema.Definitions.Add(TemplateConstants.StringDefinition, mappingDefinition);

                    // string-definition-properties
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Constant, new PropertyValue(new StringToken(null, null, null, TemplateConstants.NonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.IgnoreCase, new PropertyValue(new StringToken(null, null, null,TemplateConstants.Boolean)));
                    mappingDefinition.Properties.Add(TemplateConstants.RequireNonEmpty, new PropertyValue(new StringToken(null, null, null, TemplateConstants.Boolean)));
                    schema.Definitions.Add(TemplateConstants.StringDefinitionProperties, mappingDefinition);

                    // sequence-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Description, new PropertyValue(new StringToken(null, null, null, TemplateConstants.String)));
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(new StringToken(null, null, null, TemplateConstants.SequenceOfNonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.Sequence, new PropertyValue(new StringToken(null, null, null, TemplateConstants.SequenceDefinitionProperties)));
                    schema.Definitions.Add(TemplateConstants.SequenceDefinition, mappingDefinition);

                    // sequence-definition-properties
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.ItemType, new PropertyValue(new StringToken(null, null, null, TemplateConstants.NonEmptyString)));
                    schema.Definitions.Add(TemplateConstants.SequenceDefinitionProperties, mappingDefinition);

                    // mapping-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Description, new PropertyValue(new StringToken(null, null, null, TemplateConstants.String)));
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(new StringToken(null, null, null, TemplateConstants.SequenceOfNonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.Mapping, new PropertyValue(new StringToken(null, null, null, TemplateConstants.MappingDefinitionProperties)));
                    schema.Definitions.Add(TemplateConstants.MappingDefinition, mappingDefinition);

                    // mapping-definition-properties
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Properties, new PropertyValue(new StringToken(null, null, null, TemplateConstants.Properties)));
                    mappingDefinition.Properties.Add(TemplateConstants.LooseKeyType, new PropertyValue(new StringToken(null, null, null, TemplateConstants.NonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.LooseValueType, new PropertyValue(new StringToken(null, null, null, TemplateConstants.NonEmptyString)));
                    schema.Definitions.Add(TemplateConstants.MappingDefinitionProperties, mappingDefinition);

                    // properties
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.LooseKeyType = TemplateConstants.NonEmptyString;
                    mappingDefinition.LooseValueType = TemplateConstants.PropertyValue;
                    schema.Definitions.Add(TemplateConstants.Properties, mappingDefinition);

                    // property-value
                    oneOfDefinition = new OneOfDefinition();
                    oneOfDefinition.OneOf.Add(TemplateConstants.NonEmptyString);
                    oneOfDefinition.OneOf.Add(TemplateConstants.MappingPropertyValue);
                    schema.Definitions.Add(TemplateConstants.PropertyValue, oneOfDefinition);

                    // mapping-property-value
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Type, new PropertyValue(new StringToken(null, null, null, TemplateConstants.NonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.Required, new PropertyValue(new StringToken(null, null, null, TemplateConstants.Boolean)));
                    schema.Definitions.Add(TemplateConstants.MappingPropertyValue, mappingDefinition);


                    // one-of-definition
                    mappingDefinition = new MappingDefinition();
                    mappingDefinition.Properties.Add(TemplateConstants.Description, new PropertyValue(new StringToken(null, null, null, TemplateConstants.String)));
                    mappingDefinition.Properties.Add(TemplateConstants.Context, new PropertyValue(new StringToken(null, null, null, TemplateConstants.SequenceOfNonEmptyString)));
                    mappingDefinition.Properties.Add(TemplateConstants.OneOf, new PropertyValue(new StringToken(null, null, null, TemplateConstants.SequenceOfNonEmptyString)));
                    schema.Definitions.Add(TemplateConstants.OneOfDefinition, mappingDefinition);

                    // non-empty-string
                    stringDefinition = new StringDefinition();
                    stringDefinition.RequireNonEmpty = true;
                    schema.Definitions.Add(TemplateConstants.NonEmptyString, stringDefinition);

                    // sequence-of-non-empty-string
                    sequenceDefinition = new SequenceDefinition();
                    sequenceDefinition.ItemType = TemplateConstants.NonEmptyString;
                    schema.Definitions.Add(TemplateConstants.SequenceOfNonEmptyString, sequenceDefinition);

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