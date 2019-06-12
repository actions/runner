using System;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Interface for reading a source object (or file).
    /// This interface is used by TemplateReader to build a TemplateToken DOM.
    /// </summary>
    internal interface IObjectReader
    {
        Boolean AllowScalar(
            out Int32? line,
            out Int32? column,
            out String scalar);

        Boolean AllowSequenceStart(
            out Int32? line,
            out Int32? column);

        Boolean AllowSequenceEnd();

        Boolean AllowMappingStart(
            out Int32? line,
            out Int32? column);

        Boolean AllowMappingEnd();

        void ValidateStart();

        void ValidateEnd();
    }
}
