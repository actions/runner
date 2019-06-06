using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommonContracts = GitHub.DistributedTask.Common.Contracts;

namespace GitHub.DistributedTask.WebApi
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
