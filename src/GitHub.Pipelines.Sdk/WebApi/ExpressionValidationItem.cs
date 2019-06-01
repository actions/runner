using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class ExpressionValidationItem : ValidationItem
    {
        public ExpressionValidationItem()
            : base(InputValidationTypes.Expression)
        {
        }
    }
}
