using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Profile
{
    [DataContract]
    public class CreateProfileContext
    {
        [DataMember(IsRequired = true)]
        public string DisplayName { get; set; }

        [DataMember(IsRequired = true)]
        public string CountryName { get; set; }

        [DataMember(IsRequired = true)]
        public string EmailAddress { get; set; }

        [DataMember(IsRequired = true)]
        public bool ContactWithOffers { get; set; }
        
        [DataMember(IsRequired = false)]
        public IDictionary<String, object> CIData { get; set; }

        [DataMember(IsRequired = false)]
        public string Language { get; set; }

        [DataMember(IsRequired = false)]
        public bool HasAccount { get; set; }

        [DataMember(IsRequired = false)]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// The current state of the profile.
        /// </summary>
        [DataMember(IsRequired = false)]
        public ProfileState ProfileState { get; set; }

        /// <summary>
        /// Try get CIData item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetCIData<T>(String key, out T value)
        {
            Object valueObject = null;
            if (CIData != null && CIData.TryGetValue(key, out valueObject))
            {
                if (valueObject is T)
                {
                    value = (T)valueObject;
                    return true;
                }
                else
                {
                    value = default(T);
                    return false;
                }
            }
            else
            {
                value = default(T);
                return false;
            }
        }
    }
}
