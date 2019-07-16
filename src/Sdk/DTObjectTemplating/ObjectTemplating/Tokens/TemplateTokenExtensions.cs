using System;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    public static class TemplateTokenExtensions
    {
        internal static BooleanToken AssertBoolean(
            this TemplateToken value,
            String objectDescription)
        {
            if (value is BooleanToken booleanToken)
            {
                return booleanToken;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(BooleanToken)}' was expected.");
        }

        internal static NullToken AssertNull(
            this TemplateToken value,
            String objectDescription)
        {
            if (value is NullToken nullToken)
            {
                return nullToken;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(NullToken)}' was expected.");
        }

        internal static NumberToken AssertNumber(
            this TemplateToken value,
            String objectDescription)
        {
            if (value is NumberToken numberToken)
            {
                return numberToken;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(NumberToken)}' was expected.");
        }

        internal static StringToken AssertString(
            this TemplateToken value,
            String objectDescription)
        {
            if (value is StringToken stringToken)
            {
                return stringToken;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(StringToken)}' was expected.");
        }

        internal static MappingToken AssertMapping(
            this TemplateToken value,
            String objectDescription)
        {
            if (value is MappingToken mapping)
            {
                return mapping;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(MappingToken)}' was expected.");
        }

        internal static void AssertNotEmpty(
            this MappingToken mapping,
            String objectDescription)
        {
            if (mapping.Count == 0)
            {
                throw new ArgumentException($"Unexpected empty mapping when reading '{objectDescription}'");
            }
        }

        internal static ScalarToken AssertScalar(
            this TemplateToken value,
            String objectDescription)
        {
            if (value is ScalarToken scalar)
            {
                return scalar;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(ScalarToken)}' was expected.");
        }

        internal static SequenceToken AssertSequence(
            this TemplateToken value,
            String objectDescription)
        {
            if (value is SequenceToken sequence)
            {
                return sequence;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(SequenceToken)}' was expected.");
        }

        internal static void AssertUnexpectedValue(
            this LiteralToken literal,
            String objectDescription)
        {
            throw new ArgumentException($"Error while reading '{objectDescription}'. Unexpected value '{literal.ToString()}'");
        }
    }
}
