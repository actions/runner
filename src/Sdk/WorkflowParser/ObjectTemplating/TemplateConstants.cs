using System;
using System.Linq;

﻿namespace GitHub.Actions.WorkflowParser.ObjectTemplating
{
    internal static class TemplateConstants
    {
        internal const String AllowedValues = "allowed-values";
        internal const String Any = "any";
        internal const String Boolean = "boolean";
        internal const String BooleanDefinition = "boolean-definition";
        internal const String BooleanDefinitionProperties = "boolean-definition-properties";
        internal const String CloseExpression = "}}";
        internal const String CoerceRaw = "coerce-raw";
        internal const String Constant = "constant";
        internal const String Context = "context";
        internal const String Definition = "definition";
        internal const String Definitions = "definitions";
        internal const String Description = "description";
        internal const String IgnoreCase = "ignore-case";
        internal const String InsertDirective = "insert";
        internal const String IsExpression = "is-expression";
        internal const String ItemType = "item-type";
        internal const String LooseKeyType = "loose-key-type";
        internal const String LooseValueType = "loose-value-type";
        internal const String MaxConstant = "MAX";
        internal const String Mapping = "mapping";
        internal const String MappingDefinition = "mapping-definition";
        internal const String MappingDefinitionProperties = "mapping-definition-properties";
        internal const String MappingPropertyValue = "mapping-property-value";
        internal const String NonEmptyString = "non-empty-string";
        internal const String Null = "null";
        internal const String NullDefinition = "null-definition";
        internal const String NullDefinitionProperties = "null-definition-properties";
        internal const String Number = "number";
        internal const String NumberDefinition = "number-definition";
        internal const String NumberDefinitionProperties = "number-definition-properties";
        internal const String OneOf = "one-of";
        internal const String OneOfDefinition = "one-of-definition";
        internal const String OpenExpression = "${{";
        internal const String PropertyValue = "property-value";
        internal const String Properties = "properties";
        internal const String Required = "required";
        internal const String RequireNonEmpty = "require-non-empty";
        internal const String Scalar = "scalar";
        internal const String Sequence = "sequence";
        internal const String SequenceDefinition = "sequence-definition";
        internal const String SequenceDefinitionProperties = "sequence-definition-properties";
        internal const String Type = "type";
        internal const String SequenceOfNonEmptyString = "sequence-of-non-empty-string";
        internal const String String = "string";
        internal const String StringDefinition = "string-definition";
        internal const String StringDefinitionProperties = "string-definition-properties";
        internal const String Structure = "structure";
        internal const String TemplateSchema = "template-schema";
        internal const String Version = "version";
    }
}