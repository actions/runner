using System;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating
{
    /// <summary>
    /// Interface for reading a source object (or file).
    /// This interface is used by TemplateReader to build a TemplateToken DOM.
    /// </summary>
    internal interface IObjectReader
    {
        Boolean AllowLiteral(out LiteralToken token);

        Boolean AllowSequenceStart(out SequenceToken token);

        Boolean AllowSequenceEnd();

        Boolean AllowMappingStart(out MappingToken token);

        Boolean AllowMappingEnd();

        void ValidateStart();

        void ValidateEnd();
    }
}
