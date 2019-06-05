using System;
using System.Collections.Generic;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

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
