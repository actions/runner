using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.VisualStudio.Services.WebApi.Internal;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens
{
    /// <summary>
    /// Base class for all template tokens
    /// </summary>
    [DataContract]
    [KnownType(typeof(LiteralToken))]
    [KnownType(typeof(ExpressionToken))]
    [KnownType(typeof(SequenceToken))]
    [KnownType(typeof(MappingToken))]
    [JsonConverter(typeof(TemplateTokenJsonConverter))]
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class TemplateToken
    {
        protected TemplateToken(
            Int32 type,
            Int32? fileId,
            Int32? line,
            Int32? column)
        {
            Type = type;
            FileId = fileId;
            Line = line;
            Column = column;
        }

        internal Int32? FileId { get; set; }

        internal ExpressionParserOptions ParserOptions { get; set; }

        [DataMember(Name = "line", EmitDefaultValue = false)]
        internal Int32? Line { get; }

        [DataMember(Name = "col", EmitDefaultValue = false)]
        internal Int32? Column { get; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        internal Int32 Type { get; }

        public abstract TemplateToken Clone();

        public abstract TemplateToken Clone(Boolean omitSource);

        protected LiteralToken EvaluateLiteralToken(
            TemplateContext context,
            String expression,
            out Int32 bytes)
        {
            var originalBytes = context.Memory.CurrentBytes;
            try
            {
                var tree = new ExpressionParser(ParserOptions).CreateTree(expression, null, context.GetExpressionNamedValues(), context.ExpressionFunctions);
                var options = new EvaluationOptions
                {
                    Converters = context.ExpressionConverters,
                    MaxMemory = context.Memory.MaxBytes,
                    UseCollectionInterfaces = true,
                };
                var result = tree.EvaluateResult(context.TraceWriter.ToExpressionTraceWriter(), null, context, options);
                if (TryConvertToLiteralToken(context, result, out LiteralToken literal))
                {
                    return literal;
                }

                context.Error(this, TemplateStrings.ExpectedScalar());
                return CreateLiteralToken(context, expression);
            }
            finally
            {
                bytes = context.Memory.CurrentBytes - originalBytes;
            }
        }

        protected SequenceToken EvaluateSequenceToken(
            TemplateContext context,
            String expression,
            out Int32 bytes)
        {
            var originalBytes = context.Memory.CurrentBytes;
            try
            {
                var tree = new ExpressionParser(ParserOptions).CreateTree(expression, null, context.GetExpressionNamedValues(), context.ExpressionFunctions);
                var options = new EvaluationOptions
                {
                    Converters = context.ExpressionConverters,
                    MaxMemory = context.Memory.MaxBytes,
                    UseCollectionInterfaces = true,
                };
                var result = tree.EvaluateResult(context.TraceWriter.ToExpressionTraceWriter(), null, context, options);
                var templateToken = ConvertToTemplateToken(context, result);
                if (templateToken is SequenceToken sequence)
                {
                    return sequence;
                }

                context.Error(this, TemplateStrings.ExpectedSequence());
                return CreateSequenceToken(context);
            }
            finally
            {
                bytes = context.Memory.CurrentBytes - originalBytes;
            }
        }

        protected MappingToken EvaluateMappingToken(
            TemplateContext context,
            String expression,
            out Int32 bytes)
        {
            var originalBytes = context.Memory.CurrentBytes;
            try
            {
                var tree = new ExpressionParser(ParserOptions).CreateTree(expression, null, context.GetExpressionNamedValues(), context.ExpressionFunctions);
                var options = new EvaluationOptions
                {
                    Converters = context.ExpressionConverters,
                    MaxMemory = context.Memory.MaxBytes,
                    UseCollectionInterfaces = true,
                };
                var result = tree.EvaluateResult(context.TraceWriter.ToExpressionTraceWriter(), null, context, options);
                var templateToken = ConvertToTemplateToken(context, result);
                if (templateToken is MappingToken mapping)
                {
                    return mapping;
                }

                context.Error(this, TemplateStrings.ExpectedMapping());
                return CreateMappingToken(context);
            }
            finally
            {
                bytes = context.Memory.CurrentBytes - originalBytes;
            }
        }

        protected TemplateToken EvaluateTemplateToken(
            TemplateContext context,
            String expression,
            Boolean coerceNull,
            out Int32 bytes)
        {
            var originalBytes = context.Memory.CurrentBytes;
            try
            {
                var tree = new ExpressionParser(ParserOptions).CreateTree(expression, null, context.GetExpressionNamedValues(), context.ExpressionFunctions);
                var options = new EvaluationOptions
                {
                    Converters = context.ExpressionConverters,
                    MaxMemory = context.Memory.MaxBytes,
                    UseCollectionInterfaces = true,
                };
                var result = tree.EvaluateResult(context.TraceWriter.ToExpressionTraceWriter(), null, context, options);

                if (!coerceNull && Object.ReferenceEquals(result.Value, null))
                {
                    return null;
                }

                return ConvertToTemplateToken(context, result);
            }
            finally
            {
                bytes = context.Memory.CurrentBytes - originalBytes;
            }
        }

        private TemplateToken ConvertToTemplateToken(
            TemplateContext context,
            EvaluationResult result)
        {
            // Literal
            if (TryConvertToLiteralToken(context, result, out LiteralToken literal))
            {
                return literal;
            }
            // Known raw types
            else if (!Object.ReferenceEquals(result.Raw, null))
            {
                if (result.Raw is SequenceToken sequence)
                {
                    context.Memory.AddBytes(sequence, true);
                    return sequence;
                }
                else if (result.Raw is MappingToken mapping)
                {
                    context.Memory.AddBytes(mapping, true);
                    return mapping;
                }
            }

            // Leverage the expression SDK to traverse the object
            if (result.TryGetCollectionInterface(out Object collection))
            {
                if (collection is IReadOnlyObject dictionary)
                {
                    var mapping = CreateMappingToken(context);

                    foreach (var pair in dictionary)
                    {
                        var keyToken = CreateLiteralToken(context, pair.Key);
                        var valueResult = EvaluationResult.CreateIntermediateResult(null, pair.Value, out _);
                        var valueToken = ConvertToTemplateToken(context, valueResult);
                        mapping.Add(keyToken, valueToken);
                    }

                    return mapping;
                }
                else if (collection is IReadOnlyArray list)
                {
                    var sequence = CreateSequenceToken(context);

                    foreach (var item in list)
                    {
                        var itemResult = EvaluationResult.CreateIntermediateResult(null, item, out _);
                        var itemToken = ConvertToTemplateToken(context, itemResult);
                        sequence.Add(itemToken);
                    }

                    return sequence;
                }
            }

            throw new ArgumentException(TemplateStrings.UnableToConvertToTemplateToken(result.Value?.GetType().FullName));
        }

        private Boolean TryConvertToLiteralToken(
            TemplateContext context,
            EvaluationResult result,
            out LiteralToken literal)
        {
            if (!Object.ReferenceEquals(result.Raw, null))
            {
                if (result.Raw is LiteralToken literal2)
                {
                    context.Memory.AddBytes(literal2);
                    literal = literal2;
                    return true;
                }

                literal = null;
                return false;
            }

            // Leverage the expression SDK to convert to string
            if (result.TryConvertToString(null, out String str))
            {
                literal = CreateLiteralToken(context, str);
                return true;
            }

            literal = null;
            return false;
        }

        private LiteralToken CreateLiteralToken(
            TemplateContext context,
            String value)
        {
            var result = new LiteralToken(FileId, Line, Column, value);
            context.Memory.AddBytes(result);
            return result;
        }

        private SequenceToken CreateSequenceToken(TemplateContext context)
        {
            var result = new SequenceToken(FileId, Line, Column);
            context.Memory.AddBytes(result);
            return result;
        }

        private MappingToken CreateMappingToken(TemplateContext context)
        {
            var result = new MappingToken(FileId, Line, Column);
            context.Memory.AddBytes(result);
            return result;
        }
    }
}