using System.Runtime.Serialization;

namespace GitHub.Services.Licensing
{
    [DataContract]
    public class AccountUserLicense
    {
        public AccountUserLicense(LicensingSource source, int license)
        {
            Source = source;
            License = license;
        }

        [DataMember]
        public virtual int License { get; set; }

        [DataMember]
        public virtual LicensingSource Source { get; set; }

        public override string ToString()
        {
            return Licensing.License.GetLicense(Source, License).ToString();
        }
    }
}
