using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    [DataContract]
    public class Country
    {
        //TODO: Rename field to Name since we are now (S49 onwards) passing localized country name in this contract class
        [DataMember]
        public string EnglishName { get; set; }

        [DataMember]
        public string Code { get; set; }
    }
}
