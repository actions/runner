using System;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
{
    internal sealed class SequenceDefinition : Definition
    {
        internal SequenceDefinition()
        {
        }

        internal SequenceDefinition(MappingToken definition)
            : base(definition)
        {
            foreach (var definitionPair in definition)
            {
                var definitionKey = definitionPair.Key.AssertString($"{TemplateConstants.Definition} key");

                switch (definitionKey.Value)
                {
                    case TemplateConstants.Sequence:
                        var mapping = definitionPair.Value.AssertMapping($"{TemplateConstants.Definition} {TemplateConstants.Sequence}");
                        foreach (var mappingPair in mapping)
                        {
                            var mappingKey = mappingPair.Key.AssertString($"{TemplateConstants.Definition} {TemplateConstants.Sequence} key");
                            switch (mappingKey.Value)
                            {
                                case TemplateConstants.ItemType:
                                    var itemType = mappingPair.Value.AssertString($"{TemplateConstants.Definition} {TemplateConstants.Sequence} {TemplateConstants.ItemType}");
                                    ItemType = itemType.Value;
                                    break;

                                default:
                                    mappingKey.AssertUnexpectedValue($"{TemplateConstants.Definition} {TemplateConstants.Sequence} key");
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

        internal override DefinitionType DefinitionType => DefinitionType.Sequence;

        internal String ItemType { get; set; }

        internal override void Validate(
            TemplateSchema schema,
            String name)
        {
            if (String.IsNullOrEmpty(ItemType))
            {
                throw new ArgumentException($"'{name}' does not define '{TemplateConstants.ItemType}'");
            }

            // Lookup item type
            schema.GetDefinition(ItemType);
        }
    }
}
