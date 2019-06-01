namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    using Microsoft.VisualStudio.Services.Common;
    using System.Linq;

    [DataContract]
    public class DataSourceDetails
    {
        public DataSourceDetails()
        {
        }

        private DataSourceDetails(DataSourceDetails dataSourceDetailsToClone)
        {
            this.DataSourceName = dataSourceDetailsToClone.DataSourceName;
            this.DataSourceUrl = dataSourceDetailsToClone.DataSourceUrl;
            this.ResourceUrl = dataSourceDetailsToClone.ResourceUrl;
            this.ResultSelector = dataSourceDetailsToClone.ResultSelector;
            dataSourceDetailsToClone.Parameters?.Copy(this.Parameters);
            this.CloneHeaders(dataSourceDetailsToClone.Headers);
        }

        [DataMember(EmitDefaultValue = false)]
        public String DataSourceName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String DataSourceUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String ResourceUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String ResultSelector
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AuthorizationHeader> Headers
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> Parameters
        {
            get
            {
                if (this.parameters == null)
                {
                    this.parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                return this.parameters;
            }
        }

        public DataSourceDetails Clone()
        {
            return new DataSourceDetails(this);
        }

        private void CloneHeaders(IList<AuthorizationHeader> headers)
        {
            if (headers == null)
            {
                return;
            }

            this.Headers = headers.Select(header => new AuthorizationHeader { Name = header.Name, Value = header.Value }).ToList();
        }

        private Dictionary<String, String> parameters;
    }
}
