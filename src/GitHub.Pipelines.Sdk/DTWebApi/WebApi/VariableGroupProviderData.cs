using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Defines provider data of the variable group.
    /// </summary>
    [KnownType(typeof(AzureKeyVaultVariableGroupProviderData))]
    [DataContract]
    public class VariableGroupProviderData
    {
    }
}
