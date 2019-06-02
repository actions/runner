using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Schema
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
                var definitionKey = TemplateUtil.AssertLiteral(definition[i].Key, $"{TemplateConstants.Definition} key");
                if (String.Equals(definitionKey.Value, TemplateConstants.Context, StringComparison.Ordinal))
                {
                    var context = TemplateUtil.AssertSequence(definition[i].Value, $"{TemplateConstants.Context}");
                    definition.RemoveAt(i);
                    Context = context
                        .Select(x => TemplateUtil.AssertLiteral(x, $"{TemplateConstants.Context} item").Value)
                        .Distinct()
                        .ToArray();
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