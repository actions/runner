using System;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
{
    internal sealed class ScalarDefinition : Definition
    {
        internal ScalarDefinition()
        {
        }

        internal ScalarDefinition(MappingToken definition)
            : base(definition)
        {
            foreach (var definitionPair in definition)
            {
                var definitionKey = TemplateUtil.AssertLiteral(definitionPair.Key, $"{TemplateConstants.Definition} key");
                switch (definitionKey.Value)
                {
                    case TemplateConstants.Scalar:
                        var mapping = TemplateUtil.AssertMapping(definitionPair.Value, $"{TemplateConstants.Definition} {TemplateConstants.Scalar}");
                        foreach (var mappingPair in mapping)
                        {
                            var mappingKey = TemplateUtil.AssertLiteral(mappingPair.Key, $"{TemplateConstants.Definition} {TemplateConstants.Scalar} key");
                            switch (mappingKey.Value)
                            {
                                case TemplateConstants.Constant:
                                    var constant = TemplateUtil.AssertLiteral(mappingPair.Value, $"{TemplateConstants.Definition} {TemplateConstants.Scalar} {TemplateConstants.Constant}");
                                    Constant = constant.Value;
                                    break;

                                case TemplateConstants.IgnoreCase:
                                    IgnoreCase = ConvertToBoolean(mappingPair.Value, $"{TemplateConstants.Definition} {TemplateConstants.Scalar} {TemplateConstants.IgnoreCase}");
                                    break;

                                case TemplateConstants.RequireNonEmpty:
                                    RequireNonEmpty = ConvertToBoolean(mappingPair.Value, $"{TemplateConstants.Definition} {TemplateConstants.Scalar} {TemplateConstants.RequireNonEmpty}");
                                    break;

                                default:
                                    TemplateUtil.AssertUnexpectedValue(mappingKey, $"{TemplateConstants.Definition} key");
                                    break;
                            }
                        }
                        break;

                    default:
                        TemplateUtil.AssertUnexpectedValue(definitionKey, $"{TemplateConstants.Definition} key");
                        break;
                }
            }
        }

        internal override DefinitionType DefinitionType => DefinitionType.Scalar;

        internal String Constant { get; set; }

        internal Boolean IgnoreCase { get; set; }

        internal Boolean RequireNonEmpty { get; set; }

        internal Boolean IsMatch(LiteralToken literal)
        {
            if (!String.IsNullOrEmpty(Constant))
            {
                var comparison = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                if (String.Equals(Constant, literal.Value, comparison))
                {
                    return true;
                }
            }
            else if (RequireNonEmpty)
            {
                if (!String.IsNullOrEmpty(literal.Value))
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        internal override void Validate(
            TemplateSchema schema,
            String name)
        {
            if (!String.IsNullOrEmpty(Constant) && RequireNonEmpty)
            {
                throw new ArgumentException($"Properties '{Constant}' and '{RequireNonEmpty}' cannot both be set");
            }
        }

        private static Boolean ConvertToBoolean(
            TemplateToken value,
            String objectDescription)
        {
            var literal = TemplateUtil.AssertLiteral(value, objectDescription);
            switch (literal.Value)
            {
                case TemplateConstants.True:
                    return true;

                case TemplateConstants.False:
                    return false;

                default:
                    TemplateUtil.AssertUnexpectedValue(literal, objectDescription);
                    throw new ArgumentException();
            }
        }
    }
}
