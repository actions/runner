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
                var definitionKey = TemplateUtil.AssertLiteral(definitionPair.Key, $"{TemplateConstants.Definition} key");

                switch (definitionKey.Value)
                {
                    case TemplateConstants.Sequence:
                        var mapping = TemplateUtil.AssertMapping(definitionPair.Value, $"{TemplateConstants.Definition} {TemplateConstants.Sequence}");
                        foreach (var mappingPair in mapping)
                        {
                            var mappingKey = TemplateUtil.AssertLiteral(mappingPair.Key, $"{TemplateConstants.Definition} {TemplateConstants.Sequence} key");
                            switch (mappingKey.Value)
                            {
                                case TemplateConstants.ItemType:
                                    var itemType = TemplateUtil.AssertLiteral(mappingPair.Value, $"{TemplateConstants.Definition} {TemplateConstants.Sequence} {TemplateConstants.ItemType}");
                                    ItemType = itemType.Value;
                                    break;

                                default:
                                    TemplateUtil.AssertUnexpectedValue(mappingKey, $"{TemplateConstants.Definition} {TemplateConstants.Sequence} key");
                                    break;
                            }
                        }
                        break;

                    default:
                        TemplateUtil.AssertUnexpectedValue(definitionKey, $"{TemplateConstants.Definition} key");
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
