using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class InputValidationItem : ValidationItem
    {
        public InputValidationItem()
            : base(InputValidationTypes.Input)
        {
        }

        /// <summary>
        /// Provides binding context for the expression to evaluate
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public InputBindingContext Context
        {
            get;
            set;
        }
    }
}
