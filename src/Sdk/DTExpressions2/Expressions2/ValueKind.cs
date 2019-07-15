using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions2
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum ValueKind
    {
        Array,
        Boolean,
        Null,
        Number,
        Object,
        String,
    }
}
