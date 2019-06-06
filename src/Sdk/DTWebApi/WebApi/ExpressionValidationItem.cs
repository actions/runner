using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
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
