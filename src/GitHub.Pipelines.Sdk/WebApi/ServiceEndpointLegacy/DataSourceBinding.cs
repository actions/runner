using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommonContracts = Microsoft.TeamFoundation.DistributedTask.Common.Contracts;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    public class DataSourceBinding : CommonContracts.DataSourceBindingBase
    {
        public DataSourceBinding()
            : base()
        {
        }

        private DataSourceBinding(DataSourceBinding inputDefinitionToClone)
            : base(inputDefinitionToClone)
        {

        }

        public DataSourceBinding Clone()
        {
            return new DataSourceBinding(this);
        }
    }
}
