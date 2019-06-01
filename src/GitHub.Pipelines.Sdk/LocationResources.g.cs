using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class LocationResources
    {
        public static string CannotChangeParentDefinition(params object[] args)
        {
            const string Format = @"TF401225: Cannot change parent definition. Service type {0}, identifier {1}, parent service type {2}, identifier {3}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ParentDefinitionNotFound(params object[] args)
        {
            const string Format = @"TF401226: Cannot save service definition with type {0} identifier {1} because parent definition with type {2} identifier {3} could not be found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
