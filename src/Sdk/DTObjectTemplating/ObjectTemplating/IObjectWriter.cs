using System;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Interface for building an object. This interface is used by
    /// TemplateWriter to convert a TemplateToken DOM to another format.
    /// </summary>
    internal interface IObjectWriter
    {
        void WriteNull();

        void WriteBoolean(Boolean value);

        void WriteNumber(Double value);

        void WriteString(String value);

        void WriteSequenceStart();

        void WriteSequenceEnd();

        void WriteMappingStart();

        void WriteMappingEnd();

        void WriteStart();

        void WriteEnd();
    }
}
