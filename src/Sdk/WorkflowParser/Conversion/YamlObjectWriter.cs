using System;
using System.Globalization;
using System.IO;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using YamlDotNet.Core.Events;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    /// <summary>
    /// Converts a TemplateToken into YAML
    /// </summary>
    internal sealed class YamlObjectWriter : IObjectWriter
    {
        internal YamlObjectWriter(StringWriter writer)
        {
            m_emitter = new YamlDotNet.Core.Emitter(writer);
        }

        public void WriteString(String value)
        {
            m_emitter.Emit(new Scalar(value ?? String.Empty));
        }

        public void WriteBoolean(Boolean value)
        {
            m_emitter.Emit(new Scalar(value ? "true" : "false"));
        }

        public void WriteNumber(Double value)
        {
            m_emitter.Emit(new Scalar(value.ToString("G15", CultureInfo.InvariantCulture)));
        }

        public void WriteNull()
        {
            m_emitter.Emit(new Scalar("null"));
        }

        public void WriteSequenceStart()
        {
            m_emitter.Emit(new SequenceStart(null, null, true, SequenceStyle.Block));
        }

        public void WriteSequenceEnd()
        {
            m_emitter.Emit(new SequenceEnd());
        }

        public void WriteMappingStart()
        {
            m_emitter.Emit(new MappingStart());
        }

        public void WriteMappingEnd()
        {
            m_emitter.Emit(new MappingEnd());
        }

        public void WriteStart()
        {
            m_emitter.Emit(new StreamStart());
            m_emitter.Emit(new DocumentStart());
        }

        public void WriteEnd()
        {
            m_emitter.Emit(new DocumentEnd(isImplicit: true));
            m_emitter.Emit(new StreamEnd());
        }

        private readonly YamlDotNet.Core.IEmitter m_emitter;
    }
}