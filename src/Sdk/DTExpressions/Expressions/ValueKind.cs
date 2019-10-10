using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions
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
