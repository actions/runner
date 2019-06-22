using System;
using System.ComponentModel;

namespace GitHub.Services.Content.Common
{
    [Flags]
    [DefaultValue(None)]
    public enum PathOptions
    {
        None = 0,
        Target = 1,
        ImmediateChildren = 2,
        DeepChildren = 4,
        AllChildren = ImmediateChildren | DeepChildren,
        TargetAndAllChildren = Target | AllChildren
    }
}
