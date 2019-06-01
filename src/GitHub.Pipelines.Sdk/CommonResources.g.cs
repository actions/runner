using System.Globalization;

namespace Microsoft.VisualStudio.Services.Common.Internal
{
    public static class CommonResources
    {
        public static string EmptyCollectionNotAllowed(params object[] args)
        {
            const string Format = @"The collection must contain at least one element.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string EmptyStringNotAllowed(params object[] args)
        {
            const string Format = @"The string must have at least one character.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StringLengthNotAllowed(params object[] args)
        {
            const string Format = @"Length of '{0}' is invalid. It must be between {1} and {2} characters.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string EmptyGuidNotAllowed(params object[] args)
        {
            const string Format = @"The guid specified for parameter {0} must not be Guid.Empty.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidPropertyName(params object[] args)
        {
            const string Format = @"TF400458: Invalid property name: '{0}'. Property names cannot contain leading or trailing whitespace.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidPropertyValueSize(params object[] args)
        {
            const string Format = @"TF20509: The value of property '{0}' exceeds the maximum size allowed. '{1}' values must not exceed '{2}' bytes.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DateTimeKindMustBeSpecified(params object[] args)
        {
            const string Format = @"The DateTimeKind (Local, UTC) must be specified for DateTime arguments.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string PropertyArgumentExceededMaximumSizeAllowed(params object[] args)
        {
            const string Format = @"TF20508: The argument '{0}' is too long. It must not contain more than '{1}' characters.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidStringPropertyValueNullAllowed(params object[] args)
        {
            const string Format = @"""{0}"" is an invalid value for the {1} of a {2}. The text must be null or between {3} and {4} characters long.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidStringPropertyValueNullForbidden(params object[] args)
        {
            const string Format = @"""{0}"" is an invalid value for the {1} of a {2}. The text must be between {3} and {4} characters long and cannot be null.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ValueTypeOutOfRange(params object[] args)
        {
            const string Format = @"{0} is out of range for the {1} of a {2}. The value must be between {3} and {4}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string VssPropertyValueOutOfRange(params object[] args)
        {
            const string Format = @"Property '{0}' with value '{1}' is out of range for the Properties service. The value must be between {2} and {3}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string VssInvalidUnicodeCharacter(params object[] args)
        {
            const string Format = @"TF20507: The string argument contains a character that is not valid:'u{0:X4}'. Correct the argument, and then try the operation again.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorReadingFile(params object[] args)
        {
            const string Format = @"Error reading file: {0} ({1}).";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string IllegalBase64String(params object[] args)
        {
            const string Format = @"Illegal attempt to decode a malformed Base64-encoded string.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string CannotPromptIfNonInteractive(params object[] args)
        {
            const string Format = @"The prompt option is invalid because the process is not interactive.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StringContainsInvalidCharacters(params object[] args)
        {
            const string Format = @"The string argument contains a character that is not valid:'{0}'. Correct the argument, and then try the operation again.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DoubleValueOutOfRange(params object[] args)
        {
            const string Format = @"Property '{0}' with value '{1}' is out of range for the Team Foundation Properties service. Double values must be 0, within -1.79E+308 to -2.23E-308, or within 2.23E-308 to 1.79E+308.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string HttpRequestTimeout(params object[] args)
        {
            const string Format = @"The HTTP request timed out after {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string VssUnauthorized(params object[] args)
        {
            const string Format = @"VS30063: You are not authorized to access {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string VssUnauthorizedUnknownServer(params object[] args)
        {
            const string Format = @"VS30064: You are not authorized to access the server.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string XmlAttributeEmpty(params object[] args)
        {
            const string Format = @"The attribute '{0}' on node '{1}' cannot be empty";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string XmlAttributeNull(params object[] args)
        {
            const string Format = @"The node '{0}' must only have the attribute '{1}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string XmlNodeEmpty(params object[] args)
        {
            const string Format = @"The xml node '{0}' under node '{1}' cannot be empty";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string XmlNodeMissing(params object[] args)
        {
            const string Format = @"The mandatory xml node '{0}' is missing under '{1}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string VssUnsupportedPropertyValueType(params object[] args)
        {
            const string Format = @"Property '{0}' of type '{1}' is not supported by the Properties service. Convert the value to an Int32, DateTime, Double, String or Byte array for storage.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorDependencyOptionNotProvided(params object[] args)
        {
            const string Format = @"Option '{0}' requires that option '{1}' be provided as well";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorInvalidEnumValueTypeConversion(params object[] args)
        {
            const string Format = @"Invalid enumeration data type '{0}'.  The type must be a valid enumeration.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorInvalidResponseFileOption(params object[] args)
        {
            const string Format = @"The value provided {0} does not represent a valid response file option.  A response file option must be a valid path that begins with the '@' sign (ex:  @C:\Folder\ResponseFile.txt)";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorInvalidValueTypeConversion(params object[] args)
        {
            const string Format = @"The value '{0}' is not a valid value for argument of type '{1}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionArgumentsNotDefined(params object[] args)
        {
            const string Format = @"Option arguments are not defined";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionMultiplesNotAllowed(params object[] args)
        {
            const string Format = @"Option '{0}' does not allow multiples/duplicates";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionMustExist(params object[] args)
        {
            const string Format = @"Option '{0}' is required";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionNotRecognized(params object[] args)
        {
            const string Format = @"Invalid option usage.  Option '{0}' is not a recognized argument.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionRequired(params object[] args)
        {
            const string Format = @"Option '{0}' is required.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionRequiresValue(params object[] args)
        {
            const string Format = @"Option '{0}' requires a value";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionRunsDoNotSupportValues(params object[] args)
        {
            const string Format = @"Option runs do not support values";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionsAreMutuallyExclusive(params object[] args)
        {
            const string Format = @"The following options are mutually exclusive.  Only 1 may be defined at a time with respect to the others:  {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionsAreMutuallyInclusive(params object[] args)
        {
            const string Format = @"The following options are mutually inclusive.  If one or more are defined, then all must be defined:  {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionValueConverterNotFound(params object[] args)
        {
            const string Format = @"Option value conversion failed.  A value converter to handle converting arguments of type '{0}' was not found in the set of converters provided.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionValueNotAllowed(params object[] args)
        {
            const string Format = @"Option '{0}' does not require or allow a value";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionValuesDoNotMatchExpected(params object[] args)
        {
            const string Format = @"The value for option {0} does not match any of the expected values:  {1}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorPositionalArgumentsNotAllowed(params object[] args)
        {
            const string Format = @"Positional arguments are not allowed";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorRequiredOptionDoesNotExist(params object[] args)
        {
            const string Format = @"Option '{0}' is a required option but was not provided";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorResponseFileNotFound(params object[] args)
        {
            const string Format = @"Response file not found at path '{0}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorResponseFileOptionNotSupported(params object[] args)
        {
            const string Format = @"A response file option was provided, but the parser does not support the usage of response files.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorValueCannotBeConvertedToEnum(params object[] args)
        {
            const string Format = @"The value '{0}' cannot be converted to a valid '{1}' enumeration value.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string OperationHandlerNotFound(params object[] args)
        {
            const string Format = @"Operation handler not found for the set of arguments provided:  {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorInvalidValueConverterOrNoDefaultFound(params object[] args)
        {
            const string Format = @"A valid value converter was not defined for the class member '{0}' option definition and no default value converter could be found.  Define the Converter property on the option to supply the value converter.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOperationHandlerConstructorNotFound(params object[] args)
        {
            const string Format = @"Operation handler creation failed.  A valid constructor taking the parameters provided was not found on handler of type '{0}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOperationHandlerNotFound(params object[] args)
        {
            const string Format = @"Operation handler not found.  An operation mode handler was not found for the arguments provided.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorDuplicateDefaultOperationModeHandlerFound(params object[] args)
        {
            const string Format = @"Duplicate default operation handler found.  A distinct operation handler could not be determined because no handler matched the mode provided on the command-line and more than 1 handler marked as default was found.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorDuplicateOperationModeHandlerFound(params object[] args)
        {
            const string Format = @"Duplicate operation handler found.  A distinct operation handler could not be determined because more than 1 matched the operation mode provided on the command-line.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorInvalidValueConverterDataType(params object[] args)
        {
            const string Format = @"Invalid value converter data type.  The type {0} is not a valid {1} implementation.  Value converters must implement this interface.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorMembersContainingPositionalsRequireCollection(params object[] args)
        {
            const string Format = @"Invalid backing field or property for positional arguments.  Class members containing the values for positional arguments must be a collection type having an 'Add' method.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorDuplicatePositionalOptionAttributes(params object[] args)
        {
            const string Format = @"Duplicate {0} attribute definition.  Only a single member (including inherited members) may be decorated with a {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionsAllowingMultiplesRequireCollection(params object[] args)
        {
            const string Format = @"Invalid backing field or property for option '{0}'.  Class members containing the values for options that allow multiples must be a collection type having an 'Add' method.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionNotFound(params object[] args)
        {
            const string Format = @"Option not found or is case-sensitive:  '{0}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ErrorOptionFlagRequiresBooleanMember(params object[] args)
        {
            const string Format = @"Option '{0}' must have a boolean member type.  Options that do not take arguments (i.e. used as flags, ex: /v /f) must have a System.Boolean member type.  This member is set to true when the flag exists and false if not.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ContentIdCalculationBlockSizeError(params object[] args)
        {
            const string Format = @"All blocks except the final block must be {0} bytes.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string BasicAuthenticationRequiresSsl(params object[] args)
        {
            const string Format = @"Basic authentication requires a secure connection to the server.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ValueOutOfRange(params object[] args)
        {
            const string Format = @"The value {0} is out of range of valid values for parameter {1}. Valid values must be between {2} and {3}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string OutOfRange(params object[] args)
        {
            const string Format = @"The value {0} is outside of the allowed range.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ValueMustBeGreaterThanZero(params object[] args)
        {
            const string Format = @"The value must be greater than zero.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string NullValueNecessary(params object[] args)
        {
            const string Format = @"The value specified for the following variable must be null: {0}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string LowercaseStringRequired(params object[] args)
        {
            const string Format = @"The string argument '{0}' must only consist of lowercase characters.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UppercaseStringRequired(params object[] args)
        {
            const string Format = @"The string argument '{0}' must only consist of uppercase characters.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string EmptyArrayNotAllowed(params object[] args)
        {
            const string Format = @"The array must contain at least one element.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string EmptyOrWhiteSpaceStringNotAllowed(params object[] args)
        {
            const string Format = @"The string must have at least one non-white-space character.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StringLengthNotMatch(params object[] args)
        {
            const string Format = @"Length of the string does not match with '{0}'";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string BothStringsCannotBeNull(params object[] args)
        {
            const string Format = @"One of the following values must not be null or String.Empty: {0}, {1}.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string WhiteSpaceNotAllowed(params object[] args)
        {
            const string Format = @"The string cannot contain any whitespace characters.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnexpectedType(params object[] args)
        {
            const string Format = @"Expecting '{0}' to be of type '{1}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidEmailAddressError(params object[] args)
        {
            const string Format = @"The supplied email address is invalid.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string AbsoluteVirtualPathNotAllowed(params object[] args)
        {
            const string Format = @"An absolute virtual path is not allowed. Remove the leading slash: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UriUtility_AbsoluteUriRequired(params object[] args)
        {
            const string Format = @"TF205013: The following URL is not valid: {0}. You must specify an absolute path.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UriUtility_RelativePathInvalid(params object[] args)
        {
            const string Format = @"TF205014: The following relative path is not valid: {0}. It must be both well formed and relative. It might be an absolute path.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UriUtility_UriNotAllowed(params object[] args)
        {
            const string Format = @"TF205012: The following URL is not valid: {0}. It must begin with http or https.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UriUtility_MustBeAuthorityOnlyUri(params object[] args)
        {
            const string Format = @"TF253018: The following URL is not valid: {0}. Try removing any relative path information from the URL (for example, {1}).";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UrlNotValid(params object[] args)
        {
            const string Format = @"TF249010: The URL that you specified is not valid. The URL must begin with either HTTP or HTTPS and be a valid address.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MalformedArtifactId(params object[] args)
        {
            const string Format = @"The artifact is not understood by this application. Either the artifact supplied is invalid or the application doesn't have the required software updates. Artifact Id: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MalformedUri(params object[] args)
        {
            const string Format = @"Malformed Artifact URI: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string MalformedUrl(params object[] args)
        {
            const string Format = @"Malformed Artifact URL: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string NullArtifactUrl(params object[] args)
        {
            const string Format = @"Null Artifact Url";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string FailureGetArtifact(params object[] args)
        {
            const string Format = @"Unable to get artifacts from tool.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string NullArtifactUriRoot(params object[] args)
        {
            const string Format = @"ArtifactUriRoot is Null";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string UnknownTypeForSerialization(params object[] args)
        {
            const string Format = @"Unknown object type '{0}' for serialization.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string StringContainsIllegalChars(params object[] args)
        {
            const string Format = @"The value contains characters that are not allowed (control characters, 0xFFFE, or 0xFFFF). Please remove those characters.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ValueEqualsToInfinity(params object[] args)
        {
            const string Format = @"The value must be a finite value.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SingleBitRequired(params object[] args)
        {
            const string Format = @"The value {0} must contain a single bit flag.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidEnumArgument(params object[] args)
        {
            const string Format = @"The value of argument '{0}' ({1}) is invalid for Enum type '{2}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ConflictingPathSeparatorForVssFileStorage(params object[] args)
        {
            const string Format = @"There is a conflict with the path separator character '{0}' requested for VssFileStorage at file path: {1}  A previous instance was created with a path separator of '{2}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string ConflictingStringComparerForVssFileStorage(params object[] args)
        {
            const string Format = @"There is a conflict with the string comparer '{0}' requested for VssFileStorage at file path: {1}  A previous instance was created with a string comparer of '{2}'.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidClientStoragePath(params object[] args)
        {
            const string Format = @"The storage path specified is invalid: '{0}'  This storage path cannot be null or empty. It should begin with the '{1}' path separator character, and have no empty path segments.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string CollectionSizeLimitExceeded(params object[] args)
        {
            const string Format = @"Collection '{0}' can have maximum '{1}' elements.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string DefaultValueNotAllowed(params object[] args)
        {
            const string Format = @"The value {0} must not be set to the default.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string NullElementNotAllowedInCollection(params object[] args)
        {
            const string Format = @"Null elements are not allowed in the collection.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string InvalidUriError(params object[] args)
        {
            const string Format = @"Supplied URI is invalid. The URI should match {0} URI kind format.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string SubjectDescriptorEmpty(params object[] args)
        {
            const string Format = @"The subject descriptor specified for parameter {0} must not be empty.";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
        public static string EUIILeakException(params object[] args)
        {
            const string Format = @"Event payload contains EUII. Message: {0}";
            if (args == null || args.Length == 0)
            {
                return Format;
            }
            return string.Format(CultureInfo.CurrentCulture, Format, args);
        }
    }
}
