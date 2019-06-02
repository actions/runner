using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Schema
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