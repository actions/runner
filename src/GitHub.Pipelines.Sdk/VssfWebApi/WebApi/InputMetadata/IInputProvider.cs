using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.FormInput
{
    public interface IFormInputProvider
    {
        IList<InputDescriptor> InputDescriptors { get; set; }
    }

    public interface IFormInputValuesProvider
    {
       
    }

}
