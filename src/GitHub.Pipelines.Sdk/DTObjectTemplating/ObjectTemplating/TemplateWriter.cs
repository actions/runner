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
            if (value is LiteralToken literal)
            {
                objectWriter.WriteString(literal.Value);
            }
            else if (value is SequenceToken sequence)
            {
                objectWriter.WriteSequenceStart();
                foreach (var item in sequence)
                {
                    WriteValue(objectWriter, item);
                }
                objectWriter.WriteSequenceEnd();
            }
            else if (value is MappingToken mapping)
            {
                objectWriter.WriteMappingStart();
                foreach (var pair in mapping)
                {
                    WriteValue(objectWriter, pair.Key);
                    WriteValue(objectWriter, pair.Value);
                }
                objectWriter.WriteMappingEnd();
            }
            else if (value is ExpressionToken expr)
            {
                objectWriter.WriteString(expr.ToString());
            }
            else
            {
                throw new NotSupportedException($"Unexpected type '{value.GetType()}'");
            }
        }
    }
}
