using System.Globalization;

namespace GitHub.Services.Content.Common
{
    public static class ContentResources
    {

        public static string InvalidHexString(object arg0)
        {
            const string Format = @"Invalid hex string.  The string value provided {0} is not a valid hex string.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ArtifactBillingException()
        {
            const string Format = @"Artifact cannot be uploaded because max quantity has been exceeded or the payment instrument is invalid. https://aka.ms/artbilling for details.";
            return Format;
        }
    }
}
