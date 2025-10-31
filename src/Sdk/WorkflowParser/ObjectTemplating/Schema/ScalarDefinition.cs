using System;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating.Schema
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