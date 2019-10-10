using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Pipelines;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Represents the context of variables and vectors for a job request.
    /// </summary>
    [DataContract]
    public sealed class JobEnvironment : ICloneable
    {
        /// <summary>
        /// Initializes a new <c>JobEnvironment</c> with empty collections of repositories, vectors, 
        /// and variables.
        /// </summary>
        public JobEnvironment()
        {
        }

        public JobEnvironment(
            IDictionary<String, VariableValue> variables,
            List<MaskHint> maskhints,
            JobResources resources)
        {
            if (resources!= null)
            {
                this.Endpoints.AddRange(resources.Endpoints.Where(x => !String.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase)));
                this.SystemConnection = resources.Endpoints.FirstOrDefault(x => String.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
                this.SecureFiles.AddRange(resources.SecureFiles);
            }

            if (maskhints != null)
            {
                this.MaskHints.AddRange(maskhints);
            }

            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    this.Variables[variable.Key] = variable.Value?.Value;

                    if (variable.Value?.IsSecret == true)
                    {
                        // Make sure we propagate secret variables into the mask hints
                        this.MaskHints.Add(new MaskHint { Type = MaskType.Variable, Value = variable.Key });
                    }
                }
            }
        }

        public void Extract(
            Dictionary<String, VariableValue> variables, 
            HashSet<MaskHint> maskhints, 
            JobResources jobResources)
        {
            // construct variables
            HashSet<String> secretVariables = new HashSet<string>(this.MaskHints.Where(t => t.Type == MaskType.Variable).Select(v => v.Value), StringComparer.OrdinalIgnoreCase);
            foreach (var variable in this.Variables)
            {
                variables[variable.Key] = new VariableValue(variable.Value, secretVariables.Contains(variable.Key));
            }

            // construct maskhints
            maskhints.AddRange(this.MaskHints.Where(x => !(x.Type == MaskType.Variable && secretVariables.Contains(x.Value))).Select(x => x.Clone()));

            // constuct job resources (endpoints, securefiles and systemconnection)
            jobResources.SecureFiles.AddRange(this.SecureFiles.Select(x => x.Clone()));
            jobResources.Endpoints.AddRange(this.Endpoints.Select(x => x.Clone()));

            if (this.SystemConnection != null)
            {
                jobResources.Endpoints.Add(this.SystemConnection.Clone());
            }
        }

        public JobEnvironment(PlanEnvironment environment)
        {
            ArgumentUtility.CheckForNull(environment, nameof(environment));

            if (environment.MaskHints.Count > 0)
            {
                m_maskHints = new List<MaskHint>(environment.MaskHints.Select(x => x.Clone()));
            }

            if (environment.Options.Count > 0)
            {
                m_options = environment.Options.ToDictionary(x => x.Key, x => x.Value.Clone());
            }

            if (environment.Variables.Count > 0)
            {
                m_variables = new Dictionary<String, String>(environment.Variables, StringComparer.OrdinalIgnoreCase);
            }
        }

        private JobEnvironment(JobEnvironment environmentToClone)
        {
            if (environmentToClone.SystemConnection != null)
            {
                this.SystemConnection = environmentToClone.SystemConnection.Clone();
            }

            if (environmentToClone.m_maskHints != null)
            {
                m_maskHints = environmentToClone.m_maskHints.Select(x => x.Clone()).ToList();
            }

            if (environmentToClone.m_endpoints != null)
            {
                m_endpoints = environmentToClone.m_endpoints.Select(x => x.Clone()).ToList();
            }

            if (environmentToClone.m_secureFiles != null)
            {
                m_secureFiles = environmentToClone.m_secureFiles.Select(x => x.Clone()).ToList();
            }

            if (environmentToClone.m_options != null)
            {
                m_options = environmentToClone.m_options.ToDictionary(x => x.Key, x => x.Value.Clone());
            }

            if (environmentToClone.m_variables != null)
            {
                m_variables = new Dictionary<String, String>(environmentToClone.m_variables, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets or sets the endpoint used for communicating back to the calling service.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public ServiceEndpoint SystemConnection
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the collection of mask hints
        /// </summary>
        public List<MaskHint> MaskHints
        {
            get
            {
                if (m_maskHints == null)
                {
                    m_maskHints = new List<MaskHint>();
                }
                return m_maskHints;
            }
        }

        /// <summary>
        /// Gets the collection of endpoints associated with the current context.
        /// </summary>
        public List<ServiceEndpoint> Endpoints
        {
            get
            {
                if (m_endpoints == null)
                {
                    m_endpoints = new List<ServiceEndpoint>();
                }
                return m_endpoints;
            }
        }

        /// <summary>
        /// Gets the collection of secure files associated with the current context
        /// </summary>
        public List<SecureFile> SecureFiles
        {
            get
            {
                if (m_secureFiles == null)
                {
                    m_secureFiles = new List<SecureFile>();
                }
                return m_secureFiles;
            }
        }

        /// <summary>
        /// Gets the collection of options associated with the current context. (Deprecated, use by 1.x agent)
        /// </summary>
        public IDictionary<Guid, JobOption> Options
        {
            get
            {
                if (m_options == null)
                {
                    m_options = new Dictionary<Guid, JobOption>();
                }
                return m_options;
            }
        }

        /// <summary>
        /// Gets the collection of variables associated with the current context.
        /// </summary>
        public IDictionary<String, String> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_variables;
            }
        }

        Object ICloneable.Clone()
        {
            return this.Clone();
        }

        /// <summary>
        /// Creates a deep copy of the job environment.
        /// </summary>
        /// <returns>A deep copy of the job environment</returns>
        public JobEnvironment Clone()
        {
            return new JobEnvironment(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (m_serializedMaskHints != null && m_serializedMaskHints.Count > 0)
            {
                m_maskHints = new List<MaskHint>(m_serializedMaskHints.Distinct());
            }

            m_serializedMaskHints = null;

            SerializationHelper.Copy(ref m_serializedVariables, ref m_variables, true);
            SerializationHelper.Copy(ref m_serializedEndpoints, ref m_endpoints, true);
            SerializationHelper.Copy(ref m_serializedSecureFiles, ref m_secureFiles, true);
            SerializationHelper.Copy(ref m_serializedOptions, ref m_options, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (this.m_maskHints != null && this.m_maskHints.Count > 0)
            {
                m_serializedMaskHints = new List<MaskHint>(this.m_maskHints.Distinct());
            }

            SerializationHelper.Copy(ref m_variables, ref m_serializedVariables);
            SerializationHelper.Copy(ref m_endpoints, ref m_serializedEndpoints);
            SerializationHelper.Copy(ref m_secureFiles, ref m_serializedSecureFiles);
            SerializationHelper.Copy(ref m_options, ref m_serializedOptions);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedMaskHints = null;
            m_serializedVariables = null;
            m_serializedEndpoints = null;
            m_serializedSecureFiles = null;
            m_serializedOptions = null;
        }

        private List<MaskHint> m_maskHints;
        private List<ServiceEndpoint> m_endpoints;
        private List<SecureFile> m_secureFiles;
        private IDictionary<Guid, JobOption> m_options;
        private IDictionary<String, String> m_variables;

        [DataMember(Name = "Endpoints", EmitDefaultValue = false)]
        private List<ServiceEndpoint> m_serializedEndpoints;

        [DataMember(Name = "SecureFiles", EmitDefaultValue = false)]
        private List<SecureFile> m_serializedSecureFiles;

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        private IDictionary<Guid, JobOption> m_serializedOptions;

        [DataMember(Name = "Mask", EmitDefaultValue = false)]
        private List<MaskHint> m_serializedMaskHints;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private IDictionary<String, String> m_serializedVariables;
    }
}
