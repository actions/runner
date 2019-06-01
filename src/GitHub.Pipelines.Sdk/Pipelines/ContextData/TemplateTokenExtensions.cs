using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.ContextData
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TemplateTokenExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static StringContextData ToContextData(this LiteralToken literal)
        {
            var token = literal as TemplateToken;
            var contextData = token.ToContextData();
            return contextData.AssertString("converted literal token");
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ArrayContextData ToContextData(this SequenceToken sequence)
        {
            var token = sequence as TemplateToken;
            var contextData = token.ToContextData();
            return contextData.AssertArray("converted sequence token");
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PipelineContextData ToContextData(this TemplateToken token)
        {
            switch (token.Type)
            {
                case TokenType.Mapping:
                    var mapping = token as MappingToken;
                    var dictionary = new DictionaryContextData();
                    if (mapping.Count > 0)
                    {
                        foreach (var pair in mapping)
                        {
                            var keyLiteral = TemplateUtil.AssertLiteral(pair.Key, "dictionary context data key");
                            var key = keyLiteral.Value;
                            var value = pair.Value.ToContextData();
                            dictionary.Add(key, value);
                        }
                    }
                    return dictionary;

                case TokenType.Sequence:
                    var sequence = token as SequenceToken;
                    var array = new ArrayContextData();
                    if (sequence.Count > 0)
                    {
                        foreach (var item in sequence)
                        {
                            array.Add(item.ToContextData());
                        }
                    }
                    return array;

                case TokenType.Literal:
                    var literal = token as LiteralToken;
                    return new StringContextData(literal.Value);

                default:
                    throw new NotSupportedException($"Unexpected {nameof(TemplateToken)} type '{token.Type}'");
            }
        }
    }
}