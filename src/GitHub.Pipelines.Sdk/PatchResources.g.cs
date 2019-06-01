using System.Globalization;

namespace Microsoft.VisualStudio.Services.WebApi
{
    public static class PatchResources
    {
        public static string CannotReplaceNonExistantValue(params object[] args)
        {
            const string Format = @"Attempted to replace a value that does not exist at path {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IndexOutOfRange(params object[] args)
        {
            const string Format = @"Index out of range for path {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InsertNotSupported(params object[] args)
        {
            const string Format = @"{0} does not support insert.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidOperation(params object[] args)
        {
            const string Format = @"Unrecognized operation type.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidValue(params object[] args)
        {
            const string Format = @"Value {0} does not match the expected type {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MoveCopyNotImplemented(params object[] args)
        {
            const string Format = @"Move/Copy is not implemented.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string NullOrEmptyOperations(params object[] args)
        {
            const string Format = @"At least one operation is required for Apply.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PathCannotBeNull(params object[] args)
        {
            const string Format = @"Path cannot be null.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PathInvalidEndValue(params object[] args)
        {
            const string Format = @"Path cannot end with /.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PathInvalidStartValue(params object[] args)
        {
            const string Format = @"Path is required to start with a / or be """".";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TargetCannotBeNull(params object[] args)
        {
            const string Format = @"Evaluated target should not be null.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TestFailed(params object[] args)
        {
            const string Format = @"Test Operation for path {0} failed, value {1} was not equal to test value {2}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TestNotImplementedForDictionary(params object[] args)
        {
            const string Format = @"Test is not implemented for Dictionary.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string TestNotImplementedForList(params object[] args)
        {
            const string Format = @"Test is not implemented for List.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnableToEvaluatePath(params object[] args)
        {
            const string Format = @"Unable to evaluate path {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ValueCannotBeNull(params object[] args)
        {
            const string Format = @"Value cannot be null.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ValueNotNull(params object[] args)
        {
            const string Format = @"Remove requires Value to be null.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string JsonPatchNull(params object[] args)
        {
            const string Format = @"You must pass a valid patch document in the body of the request.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidFieldName(params object[] args)
        {
            const string Format = @"Replace requires {0} to have existing value. Try Add operation instead.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
