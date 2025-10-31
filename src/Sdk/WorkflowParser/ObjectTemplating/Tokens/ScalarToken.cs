#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens
{
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