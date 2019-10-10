using System;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
{
    internal abstract class ScalarDefinition : Definition
    {
        internal ScalarDefinition()
        {
        }

        internal ScalarDefinition(MappingToken definition)
            : base(definition)
        {
        }

        internal abstract Boolean IsMatch(LiteralToken literal);
    }
}
