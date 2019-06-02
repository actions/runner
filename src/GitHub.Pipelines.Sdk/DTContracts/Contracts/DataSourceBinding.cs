using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Common.Contracts
{
    /// <summary>
    /// Represents binding of data source for the service endpoint request.
    /// </summary>
    [DataContract]
    public class DataSourceBindingBase : BaseSecuredObject
    {
        public DataSourceBindingBase()
        {
        }

        protected DataSourceBindingBase(DataSourceBindingBase inputDefinitionToClone)
            : this(inputDefinitionToClone, null)
        {
        }

        protected DataSourceBindingBase(DataSourceBindingBase inputDefinitionToClone, ISecuredObject securedObject)
            : base(securedObject)
        {
            this.DataSourceName = inputDefinitionToClone.DataSourceName;
            this.EndpointId = inputDefinitionToClone.EndpointId;
            this.Target = inputDefinitionToClone.Target;
            this.ResultTemplate = inputDefinitionToClone.ResultTemplate;
            this.EndpointUrl = inputDefinitionToClone.EndpointUrl;
            this.ResultSelector = inputDefinitionToClone.ResultSelector;
            this.RequestVerb = inputDefinitionToClone.RequestVerb;
            this.RequestContent = inputDefinitionToClone.RequestContent;
            this.CallbackContextTemplate = inputDefinitionToClone.CallbackContextTemplate;
            this.CallbackRequiredTemplate = inputDefinitionToClone.CallbackRequiredTemplate;
            this.InitialContextTemplate = inputDefinitionToClone.InitialContextTemplate;
            inputDefinitionToClone.Parameters.Copy(this.Parameters);
            this.CloneHeaders(inputDefinitionToClone.Headers);
        }

        /// <summary>
        /// Gets or sets the name of the data source.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string DataSourceName { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the data source.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> Parameters
        {
            get
            {
                if (m_parameters == null)
                {
                    m_parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                return m_parameters;
            }
        }

        public DataSourceBindingBase Clone(ISecuredObject securedObject)
        {
            return new DataSourceBindingBase(this, securedObject);
        }

        private void CloneHeaders(List<AuthorizationHeader> headers)
        {
            if (headers == null)
            {
                return;
            }

            this.Headers = headers.Select(header => new AuthorizationHeader { Name = header.Name, Value = header.Value }).ToList();
        }

        /// <summary>
        /// Gets or sets the endpoint Id.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String EndpointId { get; set; }

        /// <summary>
        /// Gets or sets the target of the data source.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Target { get; set; }

        /// <summary>
        /// Gets or sets the result template.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String ResultTemplate { get; set; }

        /// <summary>
        /// Gets or sets http request verb
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String RequestVerb { get; set; }

        /// <summary>
        /// Gets or sets http request body
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String RequestContent { get; set; }

        /// <summary>
        /// Gets or sets the url of the service endpoint.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the result selector.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String ResultSelector { get; set; }

        /// <summary>
        /// Pagination format supported by this data source(ContinuationToken/SkipTop).
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String CallbackContextTemplate { get; set; }

        /// <summary>
        /// Subsequent calls needed?
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String CallbackRequiredTemplate { get; set; }

        /// <summary>
        /// Defines the initial value of the query params
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String InitialContextTemplate { get; set; }

        /// <summary>
        /// Gets or sets the authorization headers.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<AuthorizationHeader> Headers { get; set; }

        private Dictionary<String, String> m_parameters;
    }
}