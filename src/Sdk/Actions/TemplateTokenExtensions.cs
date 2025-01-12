using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace Sdk.Actions
{
    public static class TemplateTokenExtensions
    {
        class Equality : IEqualityComparer<TemplateToken>
        {
            public bool PartialMatch { get; set; }

            public bool Equals(TemplateToken x, TemplateToken y)
            {
                return DeepEquals(x, y, PartialMatch);
            }

            public int GetHashCode(TemplateToken obj)
            {
                throw new NotImplementedException();
            }
        }
        private static Exception UnexpectedTemplateTokenType(TemplateToken token) {
            return new NotSupportedException($"Unexpected {nameof(TemplateToken)} type '{token.Type}'");
        }
        public static bool DeepEquals(this TemplateToken token, TemplateToken other, bool partialMatch = false) {
            switch(token.Type) {
            case TokenType.Null:
            case TokenType.Boolean:
            case TokenType.Number:
            case TokenType.String:
                switch(other.Type) {
                case TokenType.Null:
                case TokenType.Boolean:
                case TokenType.Number:
                case TokenType.String:
                    return EvaluationResult.CreateIntermediateResult(null, token).AbstractEqual(EvaluationResult.CreateIntermediateResult(null, other));
                case TokenType.Mapping:
                case TokenType.Sequence:
                    return false;
                default:
                    throw UnexpectedTemplateTokenType(other);
                }
            case TokenType.Mapping:
                switch(other.Type) {
                case TokenType.Mapping:
                    break;
                case TokenType.Null:
                case TokenType.Boolean:
                case TokenType.Number:
                case TokenType.String:
                case TokenType.Sequence:
                    return false;
                default:
                    throw UnexpectedTemplateTokenType(other);
                }
                var mapping = token as MappingToken;
                var othermapping = other as MappingToken;
                if(partialMatch ? mapping.Count < othermapping.Count : mapping.Count != othermapping.Count) {
                    return false;
                }
                Dictionary<string, TemplateToken> dictionary = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                if (mapping.Count > 0)
                {
                    foreach (var pair in mapping)
                    {
                        var keyLiteral = pair.Key.AssertString("dictionary context data key");
                        var key = keyLiteral.Value;
                        var value = pair.Value;
                        dictionary.Add(key, value);
                    }
                    foreach (var pair in othermapping)
                    {
                        var keyLiteral = pair.Key.AssertString("dictionary context data key");
                        var key = keyLiteral.Value;
                        var otherv = pair.Value;
                        TemplateToken value;
                        if(!dictionary.TryGetValue(key, out value) || !DeepEquals(value, otherv, partialMatch)) {
                            return false;
                        }
                    }
                }
                return true;

            case TokenType.Sequence:
                switch(other.Type) {
                case TokenType.Sequence:
                    break;
                case TokenType.Null:
                case TokenType.Boolean:
                case TokenType.Number:
                case TokenType.String:
                case TokenType.Mapping:
                    return false;
                default:
                    throw UnexpectedTemplateTokenType(other);
                }
                var sequence = token as SequenceToken;
                var otherseq = other as SequenceToken;
                if(partialMatch ? sequence.Count < otherseq.Count : sequence.Count != otherseq.Count) {
                    return false;
                }
                return (partialMatch ? sequence.Take(otherseq.Count) : sequence).SequenceEqual(otherseq, new Equality() { PartialMatch = partialMatch });

            default:
                throw UnexpectedTemplateTokenType(token);
            }
        }
    }
}