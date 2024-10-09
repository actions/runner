using System;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Interface for reading a source object (or file).
    /// This interface is used by TemplateReader to build a TemplateToken DOM.
    /// </summary>
    internal interface IObjectReader
    {
        Boolean AllowLiteral(out LiteralToken token);

        Boolean AllowSequenceStart(out SequenceToken token);

        Boolean AllowSequenceEnd(SequenceToken token = null);

        Boolean AllowMappingStart(out MappingToken token);

        Boolean AllowMappingEnd(MappingToken token = null);

        void ValidateStart();

        void ValidateEnd();
    }
}
