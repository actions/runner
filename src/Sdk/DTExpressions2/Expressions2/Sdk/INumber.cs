using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface INumber
    {
        Double GetNumber();
    }
}
