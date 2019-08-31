using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    using GitHub.DistributedTask.ObjectTemplating;

    internal sealed class TemplateReference
    {
        private TemplateReference(
            String scope,
            String id,
            String generatedId,
            StringToken templatePath,
            MappingToken inputs)
        {
            Scope = scope;
            TemplatePath = templatePath;
            Inputs = inputs;

            if (!String.IsNullOrEmpty(generatedId))
            {
                Id = generatedId;
                m_isGeneratedId = true;
            }
            else
            {
                Id = id;
            }
        }

        internal String Id { get; }

        internal MappingToken Inputs { get; }

        internal String Scope { get; }

        internal StringToken TemplatePath { get; }

        internal String TemplateScope
        {
            get
            {
                return !String.IsNullOrEmpty(Scope) ? $"{Scope}.{Id}" : Id;
            }
        }

        internal MappingToken CreateScope(
            TemplateContext context,
            TemplateToken template)
        {
            var mapping = template.AssertMapping("template file");

            // Get the inputs and outputs from the template
            var inputs = default(MappingToken);
            var outputs = default(MappingToken);
            foreach (var pair in mapping)
            {
                var propertyName = pair.Key.AssertString("template file property name");
                switch (propertyName.Value)
                {
                    case PipelineTemplateConstants.Inputs:
                        inputs = pair.Value.AssertMapping("template file inputs");
                        break;

                    case PipelineTemplateConstants.Outputs:
                        if (!m_isGeneratedId)
                        {
                            outputs = pair.Value.AssertMapping("template file outputs");
                        }
                        break;
                }
            }

            // Determine allowed input names
            var allowedInputNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            if (inputs?.Count > 0)
            {
                foreach (var pair in inputs)
                {
                    var inputPropertyName = pair.Key.AssertString("template file inputs property");
                    allowedInputNames.Add(inputPropertyName.Value);
                }
            }

            // Validate override inputs names
            var overrideInputs = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            var mergedInputs = new MappingToken(null, null, null);
            if (Inputs?.Count > 0)
            {
                foreach (var pair in Inputs)
                {
                    var inputPropertyName = pair.Key.AssertString("template reference inputs property");
                    if (!allowedInputNames.Contains(inputPropertyName.Value))
                    {
                        context.Error(inputPropertyName, $"Input '{inputPropertyName.Value}' is not allowed");
                        continue;
                    }

                    overrideInputs.Add(inputPropertyName.Value);
                    mergedInputs.Add(pair.Key, pair.Value);
                }
            }

            // Merge defaults
            if (inputs?.Count > 0)
            {
                foreach (var pair in inputs)
                {
                    var inputPropertyName = pair.Key.AssertString("template file inputs property");
                    if (!overrideInputs.Contains(inputPropertyName.Value))
                    {
                        mergedInputs.Add(pair.Key, pair.Value);
                    }
                }
            }

            // Build the scope object
            var result = new MappingToken(null, null, null);
            var namePropertyName = new StringToken(null, null, null, PipelineTemplateConstants.Name);
            var namePropertyValue = new StringToken(null, null, null, TemplateScope);
            result.Add(namePropertyName, namePropertyValue);
            if (mergedInputs.Count > 0)
            {
                var inputsPropertyName = new StringToken(null, null, null, PipelineTemplateConstants.Inputs);
                result.Add(inputsPropertyName, mergedInputs);
            }

            if (outputs?.Count > 0)
            {
                var outputsPropertyName = new StringToken(null, null, null, PipelineTemplateConstants.Outputs);
                result.Add(outputsPropertyName, outputs);
            }

            return result;
        }

        internal static Boolean TryCreate(
            MappingToken mapping,
            out TemplateReference reference)
        {
            var scope = default(String);
            var id = default(String);
            var generatedId = default(String);
            var templatePath = default(StringToken);
            var inputs = default(MappingToken);
            foreach (var property in mapping)
            {
                var propertyName = property.Key.AssertString("candidate template reference property name");
                switch (propertyName.Value)
                {
                    case PipelineTemplateConstants.Scope:
                        var scopeStringToken = property.Value.AssertString("step scope");
                        scope = scopeStringToken.Value;
                        break;

                    case PipelineTemplateConstants.Id:
                        var idStringToken = property.Value.AssertString("step id");
                        id = idStringToken.Value;
                        break;

                    case PipelineTemplateConstants.GeneratedId:
                        var generatedIdStringToken = property.Value.AssertString("step generated id");
                        generatedId = generatedIdStringToken.Value;
                        break;

                    case PipelineTemplateConstants.Template:
                        templatePath = property.Value.AssertString("step template reference");
                        break;

                    case PipelineTemplateConstants.Inputs:
                        inputs = property.Value.AssertMapping("step template reference inputs");
                        break;
                }
            }

            if (templatePath != null)
            {
                reference = new TemplateReference(scope, id, generatedId, templatePath, inputs);
                return true;
            }
            else
            {
                reference = null;
                return false;
            }
        }

        private Boolean m_isGeneratedId;
    }
}
