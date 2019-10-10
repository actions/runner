using System;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
{
    internal sealed class BooleanDefinition : ScalarDefinition
    {
        internal BooleanDefinition()
        {
        }

        internal BooleanDefinition(MappingToken definition)
            : base(definition)
        {
            foreach (var definitionPair in definition)
            {
                var definitionKey = definitionPair.Key.AssertString($"{TemplateConstants.Definition} key");
                switch (definitionKey.Value)
                {
                    case TemplateConstants.Boolean:
                        var mapping = definitionPair.Value.AssertMapping($"{TemplateConstants.Definition} {TemplateConstants.Boolean}");
                        foreach (var mappingPair in mapping)
                        {
                            var mappingKey = mappingPair.Key.AssertString($"{TemplateConstants.Definition} {TemplateConstants.Boolean} key");
                            switch (mappingKey.Value)
                            {
                                default:
                                    mappingKey.AssertUnexpectedValue($"{TemplateConstants.Definition} {TemplateConstants.Boolean} key");
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

        internal override DefinitionType DefinitionType => DefinitionType.Boolean;

        internal override Boolean IsMatch(LiteralToken literal)
        {
            return literal is BooleanToken;
        }

        internal override void Validate(
            TemplateSchema schema,
            String name)
        {
        }
    }
}
