using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class LocationResources
    {

        public static string CannotChangeParentDefinition(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"TF401225: Cannot change parent definition. Service type {0}, identifier {1}, parent service type {2}, identifier {3}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string ParentDefinitionNotFound(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"TF401226: Cannot save service definition with type {0} identifier {1} because parent definition with type {2} identifier {3} could not be found.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }
    }
}
