using System;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating.Schema
{
    /// <summary>
    /// Defines the allowable schema for a user defined type
    /// </summary>
    internal abstract class Definition
    {
        protected Definition()
        {
        }

        protected Definition(MappingToken definition)
        {
            for (var i = 0; i < definition.Count; )
            {
                var definitionKey = definition[i].Key.AssertString($"{TemplateConstants.Definition} key");
                if (String.Equals(definitionKey.Value, TemplateConstants.Context, StringComparison.Ordinal))
                {
                    var context = definition[i].Value.AssertSequence($"{TemplateConstants.Context}");
                    definition.RemoveAt(i);
                    Context = context
                        .Select(x => x.AssertString($"{TemplateConstants.Context} item").Value)
                        .Distinct()
                        .ToArray();
                }
                else if (String.Equals(definitionKey.Value, TemplateConstants.Description, StringComparison.Ordinal))
                {
                    definition.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        internal abstract DefinitionType DefinitionType { get; }

        internal String[] Context { get; private set; } = new String[0];

        internal abstract void Validate(
            TemplateSchema schema,
            String name);
    }
}
