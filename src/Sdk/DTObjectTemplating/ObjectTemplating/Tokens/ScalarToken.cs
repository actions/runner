using System;
using System.ComponentModel;
using GitHub.Services.WebApi.Internal;

namespace GitHub.DistributedTask.ObjectTemplating.Tokens
{
    [ClientIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ScalarToken : TemplateToken
    {
        protected ScalarToken(
            Int32 type,
            Int32? fileId,
            Int32? line,
            Int32? column)
            : base(type, fileId, line, column)
        {
        }

        public virtual String ToDisplayString()
        {
            return TrimDisplayString(ToString());
        }

        protected String TrimDisplayString(String displayString)
        {
            var firstLine = displayString.TrimStart(' ', '\t', '\r', '\n');
            var firstNewLine = firstLine.IndexOfAny(new[] { '\r', '\n' });
            if (firstNewLine >= 0)
            {
                firstLine = firstLine.Substring(0, firstNewLine);
            }
            return firstLine;
        }
    }
}
