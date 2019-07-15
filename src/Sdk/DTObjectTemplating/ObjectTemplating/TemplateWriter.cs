using System;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Converts from a TemplateToken into another object format
    /// </summary>
    internal sealed class TemplateWriter
    {
        internal static void Write(
            IObjectWriter objectWriter,
            TemplateToken value)
        {
            objectWriter.WriteStart();
            WriteValue(objectWriter, value);
            objectWriter.WriteEnd();
        }

        private static void WriteValue(
            IObjectWriter objectWriter,
            TemplateToken value)
        {
            switch (value?.Type ?? TokenType.Null)
            {
                case TokenType.Null:
                    objectWriter.WriteNull();
                    break;

                case TokenType.Boolean:
                    var booleanToken = value as BooleanToken;
                    objectWriter.WriteBoolean(booleanToken.Value);
                    break;

                case TokenType.Number:
                    var numberToken = value as NumberToken;
                    objectWriter.WriteNumber(numberToken.Value);
                    break;

                case TokenType.String:
                case TokenType.BasicExpression:
                case TokenType.InsertExpression:
                    objectWriter.WriteString(value.ToString());
                    break;

                case TokenType.Mapping:
                    var mappingToken = value as MappingToken;
                    objectWriter.WriteMappingStart();
                    foreach (var pair in mappingToken)
                    {
                        WriteValue(objectWriter, pair.Key);
                        WriteValue(objectWriter, pair.Value);
                    }
                    objectWriter.WriteMappingEnd();
                    break;

                case TokenType.Sequence:
                    var sequenceToken = value as SequenceToken;
                    objectWriter.WriteSequenceStart();
                    foreach (var item in sequenceToken)
                    {
                        WriteValue(objectWriter, item);
                    }
                    objectWriter.WriteSequenceEnd();
                    break;

                default:
                    throw new NotSupportedException($"Unexpected type '{value.GetType()}'");
            }
        }
    }
}
