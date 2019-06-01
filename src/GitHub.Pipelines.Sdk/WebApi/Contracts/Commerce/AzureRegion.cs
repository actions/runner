using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// Represents an azure region, used by ibiza for linking accounts
    /// </summary>
    public class AzureRegion
    {
        /// <summary>
        /// Unique Identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display Name of the azure region. Ex: North Central US.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Region code of the azure region. Ex: NCUS.
        /// </summary>
        public string RegionCode { get; set; }

        /// <summary>
        /// Returns unique hash code associated with this object. Calls base object.GetHashCode().
        /// </summary>
        /// <returns>Unique integer</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Returns true if all fields of the target AzureRegion are equal to this AzureRegion object
        /// </summary>
        /// <param name="obj">Target Azure Region</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (obj is AzureRegion)
            {
                var target = obj as AzureRegion;
                if (obj != null)
                {
                    return target.Id.Equals(this.Id, StringComparison.OrdinalIgnoreCase)
                        && target.DisplayName.Equals(this.DisplayName, StringComparison.OrdinalIgnoreCase)
                        && target.RegionCode.Equals(this.RegionCode, StringComparison.OrdinalIgnoreCase);
                }
            }
            return base.Equals(obj);
        }
    }
}
