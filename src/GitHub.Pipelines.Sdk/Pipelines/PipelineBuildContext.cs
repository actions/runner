using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PipelineBuildContext : PipelineContextBase
    {
        public PipelineBuildContext()
            : this(new BuildOptions())
        {
        }

        public PipelineBuildContext(
            IPipelineContext context,
            BuildOptions options)
            : base(context)
        {
            m_buildOptions = options ?? new BuildOptions();
        }

        public PipelineBuildContext(
            BuildOptions buildOptions,
            ICounterStore counterStore = null,
            IResourceStore resourceStore = null,
            IList<IStepProvider> stepProviders = null,
            ITaskStore taskStore = null,
            IPackageStore packageStore = null,
            IInputValidator inputValidator = null,
            IPipelineTraceWriter trace = null,
            EvaluationOptions expressionOptions = null,
            IList<IPhaseProvider> phaseProviders = null)
            : base(counterStore, packageStore, resourceStore, taskStore, stepProviders, null, trace, expressionOptions)
        {
            m_buildOptions = buildOptions ?? new BuildOptions();
            m_inputValidator = inputValidator;
            m_phaseProviders = phaseProviders;
        }

        public BuildOptions BuildOptions
        {
            get
            {
                return m_buildOptions;
            }
        }

        public IInputValidator InputValidator
        {
            get
            {
                return m_inputValidator;
            }
        }

        public IReadOnlyList<IPhaseProvider> PhaseProviders
        {
            get
            {
                return m_phaseProviders.ToList();
            }
        }

        internal ValidationResult Validate(PipelineProcess process)
        {
            var result = new ValidationResult();

            // If requested to do so, validate the container resource to ensure it was specified properly. This will 
            // also handle endpoint authorization if the container requires access to a docker registry.
            if (this.ResourceStore != null)
            {
                foreach (var container in this.ResourceStore.Containers.GetAll().Where(x => x.Endpoint != null))
                {
                    result.ReferencedResources.AddEndpointReference(container.Endpoint);

                    if (this.BuildOptions.ValidateResources)
                    {
                        var endpoint = this.ResourceStore.GetEndpoint(container.Endpoint);
                        if (endpoint == null)
                        {
                            result.UnauthorizedResources.AddEndpointReference(container.Endpoint);
                            result.Errors.Add(new PipelineValidationError(PipelineStrings.ServiceEndpointNotFound(container.Endpoint)));
                        }
                        else
                        {
                            if (!endpoint.Type.Equals(ServiceEndpointTypes.Docker, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Errors.Add(new PipelineValidationError(PipelineStrings.ContainerResourceInvalidRegistryEndpointType(container.Alias, endpoint.Type, endpoint.Name)));
                            }
                            else
                            {
                                container.Endpoint = new ServiceEndpointReference
                                {
                                    Id = endpoint.Id,
                                };
                            }
                        }
                    }
                }

                foreach (var repository in this.ResourceStore.Repositories.GetAll())
                {
                    var expandedProperties = new Dictionary<String, JToken>(StringComparer.OrdinalIgnoreCase);
                    foreach (var property in repository.Properties.GetItems())
                    {
                        expandedProperties[property.Key] = this.ExpandVariables(property.Value);
                    }

                    foreach (var expandedProperty in expandedProperties)
                    {
                        repository.Properties.Set<JToken>(expandedProperty.Key, expandedProperty.Value);
                    }
                }

                if (this.EnvironmentVersion > 1)
                {
                    // always add self or designer repo to repository list
                    RepositoryResource defaultRepo = null;
                    var selfRepo = this.ResourceStore.Repositories.Get(PipelineConstants.SelfAlias);
                    if (selfRepo == null)
                    {
                        var designerRepo = this.ResourceStore.Repositories.Get(PipelineConstants.DesignerRepo);
                        if (designerRepo != null)
                        {
                            defaultRepo = designerRepo;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Fail("Repositories are not filled in.");
                        }
                    }
                    else
                    {
                        defaultRepo = selfRepo;
                    }

                    if (defaultRepo != null)
                    {
                        result.ReferencedResources.Repositories.Add(defaultRepo);

                        // Add the endpoint
                        if (defaultRepo.Endpoint != null)
                        {
                            result.ReferencedResources.AddEndpointReference(defaultRepo.Endpoint);

                            if (this.BuildOptions.ValidateResources)
                            {
                                var repositoryEndpoint = this.ResourceStore.GetEndpoint(defaultRepo.Endpoint);
                                if (repositoryEndpoint == null)
                                {
                                    result.UnauthorizedResources?.AddEndpointReference(defaultRepo.Endpoint);
                                    result.Errors.Add(new PipelineValidationError(PipelineStrings.ServiceEndpointNotFound(defaultRepo.Endpoint)));
                                }
                                else
                                {
                                    defaultRepo.Endpoint = new ServiceEndpointReference() { Id = repositoryEndpoint.Id };
                                }
                            }
                        }
                    }
                }
            }

            // Validate the graph of stages
            GraphValidator.Validate(this, result, (input) => PipelineStrings.StageNameWhenNoNameIsProvided(input), null, process.Stages, Stage.GetErrorMessage);

            return result;
        }

        private IInputValidator m_inputValidator;
        private IList<IPhaseProvider> m_phaseProviders;
        private BuildOptions m_buildOptions;
    }
}
