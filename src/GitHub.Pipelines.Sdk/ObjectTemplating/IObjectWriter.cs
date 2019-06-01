using System;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Interface for building an object. This interface is used by
    /// TemplateWriter to convert a TemplateToken DOM to another format.
    /// </summary>
    internal interface IObjectWriter
    {
        void WriteString(String str);

        void WriteSequenceStart();

        void WriteSequenceEnd();

        void WriteMappingStart();

        void WriteMappingEnd();

        void WriteStart();

        void WriteEnd();
    }
}
