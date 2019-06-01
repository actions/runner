using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum ValueKind
    {
        Array,
        Boolean,
        DateTime,
        Null,
        Number,
        Object,
        String,
        Version,
    }
}
