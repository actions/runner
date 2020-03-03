using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    /// <summary>
    /// Base class for all template tokens
    /// </summary>
    [DataContract]
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

        [DataMember(Name = "file", EmitDefaultValue = false)]
        internal Int32? FileId { get; private set; }

        [DataMember(Name = "line", EmitDefaultValue = false)]
        internal Int32? Line { get; private set; }

        [DataMember(Name = "col", EmitDefaultValue = false)]
        internal Int32? Column { get; private set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        internal Int32 Type { get; }

        public TemplateToken Clone()
        {
            return Clone(false);
        }

        public abstract TemplateToken Clone(Boolean omitSource);

        protected StringToken EvaluateStringToken(
            TemplateContext context,
            String expression,
            out Int32 bytes)
        {
            var originalBytes = context.Memory.CurrentBytes;
            try
            {
                var tree = new ExpressionParser().CreateTree(expression, null, context.GetExpressionNamedValues(), context.ExpressionFunctions);
                var options = new EvaluationOptions
                {
                    MaxMemory = context.Memory.MaxBytes,
                };
                var result = tree.Evaluate(context.TraceWriter.ToExpressionTraceWriter(), null, context, options);

                if (result.Raw is LiteralToken literalToken)
                {
                    var stringToken = new StringToken(FileId, Line, Column, literalToken.ToString());
                    context.Memory.AddBytes(stringToken);
                    return stringToken;
                }

                if (!result.IsPrimitive)
                {
                    context.Error(this, "Expected a string");
                    return CreateStringToken(context, expression);
                }

                var stringValue = result.Kind == ValueKind.Null ? String.Empty : result.ConvertToString();
                return CreateStringToken(context, stringValue);
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
                var tree = new ExpressionParser().CreateTree(expression, null, context.GetExpressionNamedValues(), context.ExpressionFunctions);
                var options = new EvaluationOptions
                {
                    MaxMemory = context.Memory.MaxBytes,
                };
                var result = tree.Evaluate(context.TraceWriter.ToExpressionTraceWriter(), null, context, options);
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
                var tree = new ExpressionParser().CreateTree(expression, null, context.GetExpressionNamedValues(), context.ExpressionFunctions);
                var options = new EvaluationOptions
                {
                    MaxMemory = context.Memory.MaxBytes,
                };
                var result = tree.Evaluate(context.TraceWriter.ToExpressionTraceWriter(), null, context, options);
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
            out Int32 bytes)
        {
            var originalBytes = context.Memory.CurrentBytes;
            try
            {
                var tree = new ExpressionParser().CreateTree(expression, null, context.GetExpressionNamedValues(), context.ExpressionFunctions);
                var options = new EvaluationOptions
                {
                    MaxMemory = context.Memory.MaxBytes,
                };
                var result = tree.Evaluate(context.TraceWriter.ToExpressionTraceWriter(), null, context, options);
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

                    foreach (KeyValuePair<String, Object> pair in dictionary)
                    {
                        var keyToken = CreateStringToken(context, pair.Key);
                        var valueResult = EvaluationResult.CreateIntermediateResult(null, pair.Value);
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
                        var itemResult = EvaluationResult.CreateIntermediateResult(null, item);
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
            if (result.Raw is LiteralToken literal2)
            {
                context.Memory.AddBytes(literal2);
                literal = literal2;
                return true;
            }

            switch (result.Kind)
            {
                case ValueKind.Null:
                    literal = new NullToken(FileId, Line, Column);
                    break;

                case ValueKind.Boolean:
                    literal = new BooleanToken(FileId, Line, Column, (Boolean)result.Value);
                    break;

                case ValueKind.Number:
                    literal = new NumberToken(FileId, Line, Column, (Double)result.Value);
                    break;

                case ValueKind.String:
                    literal = new StringToken(FileId, Line, Column, (String)result.Value);
                    break;

                default:
                    literal = null;
                    return false;
            }

            context.Memory.AddBytes(literal);
            return true;
        }

        private StringToken CreateStringToken(
            TemplateContext context,
            String value)
        {
            var result = new StringToken(FileId, Line, Column, value);
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
