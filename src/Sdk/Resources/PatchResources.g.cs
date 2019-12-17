using System.Globalization;

namespace GitHub.Services.WebApi
{
    public static class PatchResources
    {
        public static string CannotReplaceNonExistantValue(object arg0)
        {
            const string Format = @"Attempted to replace a value that does not exist at path {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string IndexOutOfRange(object arg0)
        {
            const string Format = @"Index out of range for path {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InsertNotSupported(object arg0)
        {
            const string Format = @"{0} does not support insert.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidOperation()
        {
            const string Format = @"Unrecognized operation type.";
            return Format;
        }

        public static string InvalidValue(object arg0, object arg1)
        {
            const string Format = @"Value {0} does not match the expected type {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string MoveCopyNotImplemented()
        {
            const string Format = @"Move/Copy is not implemented.";
            return Format;
        }

        public static string PathCannotBeNull()
        {
            const string Format = @"Path cannot be null.";
            return Format;
        }

        public static string PathInvalidEndValue()
        {
            const string Format = @"Path cannot end with /.";
            return Format;
        }

        public static string PathInvalidStartValue()
        {
            const string Format = @"Path is required to start with a / or be """".";
            return Format;
        }

        public static string TargetCannotBeNull()
        {
            const string Format = @"Evaluated target should not be null.";
            return Format;
        }

        public static string TestFailed(object arg0, object arg1, object arg2)
        {
            const string Format = @"Test Operation for path {0} failed, value {1} was not equal to test value {2}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string TestNotImplementedForDictionary()
        {
            const string Format = @"Test is not implemented for Dictionary.";
            return Format;
        }

        public static string TestNotImplementedForList()
        {
            const string Format = @"Test is not implemented for List.";
            return Format;
        }

        public static string UnableToEvaluatePath(object arg0)
        {
            const string Format = @"Unable to evaluate path {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ValueCannotBeNull()
        {
            const string Format = @"Value cannot be null.";
            return Format;
        }

        public static string ValueNotNull()
        {
            const string Format = @"Remove requires Value to be null.";
            return Format;
        }

        public static string InvalidFieldName(object arg0)
        {
            const string Format = @"Replace requires {0} to have existing value. Try Add operation instead.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
