using System;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
{
    internal sealed class NullDefinition : ScalarDefinition
    {
        internal NullDefinition()
        {
        }

        internal NullDefinition(MappingToken definition)
            : base(definition)
        {
            foreach (var definitionPair in definition)
            {
                var definitionKey = definitionPair.Key.AssertString($"{TemplateConstants.Definition} key");
                switch (definitionKey.Value)
                {
                    case TemplateConstants.Null:
                        var mapping = definitionPair.Value.AssertMapping($"{TemplateConstants.Definition} {TemplateConstants.Null}");
                        foreach (var mappingPair in mapping)
                        {
                            var mappingKey = mappingPair.Key.AssertString($"{TemplateConstants.Definition} {TemplateConstants.Null} key");
                            switch (mappingKey.Value)
                            {
                                default:
                                    mappingKey.AssertUnexpectedValue($"{TemplateConstants.Definition} {TemplateConstants.Null} key");
                                    break;
                            }
                        }
                        break;

                    default:
                        definitionKey.AssertUnexpectedValue($"{TemplateConstants.Definition} key");
                        break;
                }
            }
        }

        internal override DefinitionType DefinitionType => DefinitionType.Null;

        internal override Boolean IsMatch(LiteralToken literal)
        {
            return literal is NullToken;
        }

        internal override void Validate(
            TemplateSchema schema,
            String name)
        {
        }
    }
}
