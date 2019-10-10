using System;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
{
    internal sealed class PropertyValue
    {
        internal PropertyValue()
        {
        }

        internal PropertyValue(String type)
        {
            Type = type;
        }

        internal String Type { get; set; }
    }
}
