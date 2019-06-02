using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Services.WebApi.Internal;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens
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
    }
}