using System;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Result object from loading a template file.
    /// </summary>
    internal class LoadTemplateResult
    {
        internal LoadTemplateResult(
            String type,
            TemplateToken value,
            Int32 valueBytes,
            Int32? fileId)
        {
            Type = type;
            Value = value;
            ValueBytes = valueBytes;
            FileId = fileId;
        }

        internal Int32? FileId { get; }

        internal String Type { get; }

        internal TemplateToken Value { get; }

        /// <summary>
        /// Amount of bytes to subtract, after the template is evaluated. After evaluation, the size of the
        /// original-unevaluated DOM is no longer required, and will be garbage collected. Since the size is
        /// computing when originally building the DOM, flowing this value through allows the size to be quickly
        /// subtracted, without computing the value (i.e. traversing the object again).
        /// </summary>
        internal Int32 ValueBytes { get; set; }
    }
}
