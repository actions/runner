﻿using System.Globalization;

namespace GitHub.Services.Common.Internal
{
    public static class CommonResources
    {

        public static string EmptyCollectionNotAllowed()
        {
            const string Format = @"The collection must contain at least one element.";
            return Format;
        }

        public static string EmptyStringNotAllowed()
        {
            const string Format = @"The string must have at least one character.";
            return Format;
        }

        public static string StringLengthNotAllowed(object arg0, object arg1, object arg2)
        {
            const string Format = @"Length of '{0}' is invalid. It must be between {1} and {2} characters.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string EmptyGuidNotAllowed(object arg0)
        {
            const string Format = @"The guid specified for parameter {0} must not be Guid.Empty.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidPropertyName(object arg0)
        {
            const string Format = @"TF400458: Invalid property name: '{0}'. Property names cannot contain leading or trailing whitespace.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidPropertyValueSize(object arg0, object arg1, object arg2)
        {
            const string Format = @"TF20509: The value of property '{0}' exceeds the maximum size allowed. '{1}' values must not exceed '{2}' bytes.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string DateTimeKindMustBeSpecified()
        {
            const string Format = @"The DateTimeKind (Local, UTC) must be specified for DateTime arguments.";
            return Format;
        }

        public static string PropertyArgumentExceededMaximumSizeAllowed(object arg0, object arg1)
        {
            const string Format = @"TF20508: The argument '{0}' is too long. It must not contain more than '{1}' characters.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string InvalidStringPropertyValueNullAllowed(object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            const string Format = @"""{0}"" is an invalid value for the {1} of a {2}. The text must be null or between {3} and {4} characters long.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3, arg4);
        }

        public static string InvalidStringPropertyValueNullForbidden(object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            const string Format = @"""{0}"" is an invalid value for the {1} of a {2}. The text must be between {3} and {4} characters long and cannot be null.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3, arg4);
        }

        public static string ValueTypeOutOfRange(object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            const string Format = @"{0} is out of range for the {1} of a {2}. The value must be between {3} and {4}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3, arg4);
        }

        public static string VssPropertyValueOutOfRange(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"Property '{0}' with value '{1}' is out of range for the Properties service. The value must be between {2} and {3}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string VssInvalidUnicodeCharacter(object arg0)
        {
            const string Format = @"TF20507: The string argument contains a character that is not valid:'u{0:X4}'. Correct the argument, and then try the operation again.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorReadingFile(object arg0, object arg1)
        {
            const string Format = @"Error reading file: {0} ({1}).";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string IllegalBase64String()
        {
            const string Format = @"Illegal attempt to decode a malformed Base64-encoded string.";
            return Format;
        }

        public static string CannotPromptIfNonInteractive()
        {
            const string Format = @"The prompt option is invalid because the process is not interactive.";
            return Format;
        }

        public static string StringContainsInvalidCharacters(object arg0)
        {
            const string Format = @"The string argument contains a character that is not valid:'{0}'. Correct the argument, and then try the operation again.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string DoubleValueOutOfRange(object arg0, object arg1)
        {
            const string Format = @"Property '{0}' with value '{1}' is out of range for the Team Foundation Properties service. Double values must be 0, within -1.79E+308 to -2.23E-308, or within 2.23E-308 to 1.79E+308.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string HttpRequestTimeout(object arg0)
        {
            const string Format = @"The HTTP request timed out after {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string VssUnauthorized(object arg0)
        {
            const string Format = @"VS30063: You are not authorized to access {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string VssUnauthorizedUnknownServer()
        {
            const string Format = @"VS30064: You are not authorized to access the server.";
            return Format;
        }

        public static string XmlAttributeEmpty(object arg0, object arg1)
        {
            const string Format = @"The attribute '{0}' on node '{1}' cannot be empty";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string XmlAttributeNull(object arg0, object arg1)
        {
            const string Format = @"The node '{0}' must only have the attribute '{1}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string XmlNodeEmpty(object arg0, object arg1)
        {
            const string Format = @"The xml node '{0}' under node '{1}' cannot be empty";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string XmlNodeMissing(object arg0, object arg1)
        {
            const string Format = @"The mandatory xml node '{0}' is missing under '{1}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string VssUnsupportedPropertyValueType(object arg0, object arg1)
        {
            const string Format = @"Property '{0}' of type '{1}' is not supported by the Properties service. Convert the value to an Int32, DateTime, Double, String or Byte array for storage.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ErrorDependencyOptionNotProvided(object arg0, object arg1)
        {
            const string Format = @"Option '{0}' requires that option '{1}' be provided as well";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ErrorInvalidEnumValueTypeConversion(object arg0)
        {
            const string Format = @"Invalid enumeration data type '{0}'.  The type must be a valid enumeration.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorInvalidResponseFileOption(object arg0)
        {
            const string Format = @"The value provided {0} does not represent a valid response file option.  A response file option must be a valid path that begins with the '@' sign (ex:  @C:\Folder\ResponseFile.txt)";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorInvalidValueTypeConversion(object arg0, object arg1)
        {
            const string Format = @"The value '{0}' is not a valid value for argument of type '{1}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ErrorOptionArgumentsNotDefined()
        {
            const string Format = @"Option arguments are not defined";
            return Format;
        }

        public static string ErrorOptionMultiplesNotAllowed(object arg0)
        {
            const string Format = @"Option '{0}' does not allow multiples/duplicates";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionMustExist(object arg0)
        {
            const string Format = @"Option '{0}' is required";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionNotRecognized(object arg0)
        {
            const string Format = @"Invalid option usage.  Option '{0}' is not a recognized argument.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionRequired(object arg0)
        {
            const string Format = @"Option '{0}' is required.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionRequiresValue(object arg0)
        {
            const string Format = @"Option '{0}' requires a value";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionRunsDoNotSupportValues()
        {
            const string Format = @"Option runs do not support values";
            return Format;
        }

        public static string ErrorOptionsAreMutuallyExclusive(object arg0)
        {
            const string Format = @"The following options are mutually exclusive.  Only 1 may be defined at a time with respect to the others:  {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionsAreMutuallyInclusive(object arg0)
        {
            const string Format = @"The following options are mutually inclusive.  If one or more are defined, then all must be defined:  {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionValueConverterNotFound(object arg0)
        {
            const string Format = @"Option value conversion failed.  A value converter to handle converting arguments of type '{0}' was not found in the set of converters provided.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionValueNotAllowed(object arg0)
        {
            const string Format = @"Option '{0}' does not require or allow a value";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionValuesDoNotMatchExpected(object arg0, object arg1)
        {
            const string Format = @"The value for option {0} does not match any of the expected values:  {1}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ErrorPositionalArgumentsNotAllowed()
        {
            const string Format = @"Positional arguments are not allowed";
            return Format;
        }

        public static string ErrorRequiredOptionDoesNotExist(object arg0)
        {
            const string Format = @"Option '{0}' is a required option but was not provided";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorResponseFileNotFound(object arg0)
        {
            const string Format = @"Response file not found at path '{0}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorResponseFileOptionNotSupported()
        {
            const string Format = @"A response file option was provided, but the parser does not support the usage of response files.";
            return Format;
        }

        public static string ErrorValueCannotBeConvertedToEnum(object arg0, object arg1)
        {
            const string Format = @"The value '{0}' cannot be converted to a valid '{1}' enumeration value.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string OperationHandlerNotFound(object arg0)
        {
            const string Format = @"Operation handler not found for the set of arguments provided:  {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorInvalidValueConverterOrNoDefaultFound(object arg0)
        {
            const string Format = @"A valid value converter was not defined for the class member '{0}' option definition and no default value converter could be found.  Define the Converter property on the option to supply the value converter.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOperationHandlerConstructorNotFound(object arg0)
        {
            const string Format = @"Operation handler creation failed.  A valid constructor taking the parameters provided was not found on handler of type '{0}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOperationHandlerNotFound()
        {
            const string Format = @"Operation handler not found.  An operation mode handler was not found for the arguments provided.";
            return Format;
        }

        public static string ErrorDuplicateDefaultOperationModeHandlerFound()
        {
            const string Format = @"Duplicate default operation handler found.  A distinct operation handler could not be determined because no handler matched the mode provided on the command-line and more than 1 handler marked as default was found.";
            return Format;
        }

        public static string ErrorDuplicateOperationModeHandlerFound()
        {
            const string Format = @"Duplicate operation handler found.  A distinct operation handler could not be determined because more than 1 matched the operation mode provided on the command-line.";
            return Format;
        }

        public static string ErrorInvalidValueConverterDataType(object arg0, object arg1)
        {
            const string Format = @"Invalid value converter data type.  The type {0} is not a valid {1} implementation.  Value converters must implement this interface.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string ErrorMembersContainingPositionalsRequireCollection()
        {
            const string Format = @"Invalid backing field or property for positional arguments.  Class members containing the values for positional arguments must be a collection type having an 'Add' method.";
            return Format;
        }

        public static string ErrorDuplicatePositionalOptionAttributes(object arg0)
        {
            const string Format = @"Duplicate {0} attribute definition.  Only a single member (including inherited members) may be decorated with a {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionsAllowingMultiplesRequireCollection(object arg0)
        {
            const string Format = @"Invalid backing field or property for option '{0}'.  Class members containing the values for options that allow multiples must be a collection type having an 'Add' method.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionNotFound(object arg0)
        {
            const string Format = @"Option not found or is case-sensitive:  '{0}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ErrorOptionFlagRequiresBooleanMember(object arg0)
        {
            const string Format = @"Option '{0}' must have a boolean member type.  Options that do not take arguments (i.e. used as flags, ex: /v /f) must have a System.Boolean member type.  This member is set to true when the flag exists and false if not.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ContentIdCalculationBlockSizeError(object arg0)
        {
            const string Format = @"All blocks except the final block must be {0} bytes.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string BasicAuthenticationRequiresSsl()
        {
            const string Format = @"Basic authentication requires a secure connection to the server.";
            return Format;
        }

        public static string ValueOutOfRange(object arg0, object arg1, object arg2, object arg3)
        {
            const string Format = @"The value {0} is out of range of valid values for parameter {1}. Valid values must be between {2} and {3}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2, arg3);
        }

        public static string OutOfRange(object arg0)
        {
            const string Format = @"The value {0} is outside of the allowed range.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string ValueMustBeGreaterThanZero()
        {
            const string Format = @"The value must be greater than zero.";
            return Format;
        }

        public static string NullValueNecessary(object arg0)
        {
            const string Format = @"The value specified for the following variable must be null: {0}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string LowercaseStringRequired(object arg0)
        {
            const string Format = @"The string argument '{0}' must only consist of lowercase characters.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UppercaseStringRequired(object arg0)
        {
            const string Format = @"The string argument '{0}' must only consist of uppercase characters.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string EmptyArrayNotAllowed()
        {
            const string Format = @"The array must contain at least one element.";
            return Format;
        }

        public static string EmptyOrWhiteSpaceStringNotAllowed()
        {
            const string Format = @"The string must have at least one non-white-space character.";
            return Format;
        }

        public static string StringLengthNotMatch(object arg0)
        {
            const string Format = @"Length of the string does not match with '{0}'";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string BothStringsCannotBeNull(object arg0, object arg1)
        {
            const string Format = @"One of the following values must not be null or String.Empty: {0}, {1}.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string WhiteSpaceNotAllowed()
        {
            const string Format = @"The string cannot contain any whitespace characters.";
            return Format;
        }

        public static string UnexpectedType(object arg0, object arg1)
        {
            const string Format = @"Expecting '{0}' to be of type '{1}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string InvalidEmailAddressError()
        {
            const string Format = @"The supplied email address is invalid.";
            return Format;
        }

        public static string AbsoluteVirtualPathNotAllowed(object arg0)
        {
            const string Format = @"An absolute virtual path is not allowed. Remove the leading slash: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UriUtility_AbsoluteUriRequired(object arg0)
        {
            const string Format = @"TF205013: The following URL is not valid: {0}. You must specify an absolute path.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UriUtility_RelativePathInvalid(object arg0)
        {
            const string Format = @"TF205014: The following relative path is not valid: {0}. It must be both well formed and relative. It might be an absolute path.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UriUtility_UriNotAllowed(object arg0)
        {
            const string Format = @"TF205012: The following URL is not valid: {0}. It must begin with http or https.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string UriUtility_MustBeAuthorityOnlyUri(object arg0, object arg1)
        {
            const string Format = @"TF253018: The following URL is not valid: {0}. Try removing any relative path information from the URL (for example, {1}).";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string UrlNotValid()
        {
            const string Format = @"TF249010: The URL that you specified is not valid. The URL must begin with either HTTP or HTTPS and be a valid address.";
            return Format;
        }

        public static string MalformedArtifactId(object arg0)
        {
            const string Format = @"The artifact is not understood by this application. Either the artifact supplied is invalid or the application doesn't have the required software updates. Artifact Id: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string MalformedUri(object arg0)
        {
            const string Format = @"Malformed Artifact URI: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string MalformedUrl(object arg0)
        {
            const string Format = @"Malformed Artifact URL: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string NullArtifactUrl()
        {
            const string Format = @"Null Artifact Url";
            return Format;
        }

        public static string FailureGetArtifact()
        {
            const string Format = @"Unable to get artifacts from tool.";
            return Format;
        }

        public static string NullArtifactUriRoot()
        {
            const string Format = @"ArtifactUriRoot is Null";
            return Format;
        }

        public static string UnknownTypeForSerialization(object arg0)
        {
            const string Format = @"Unknown object type '{0}' for serialization.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string StringContainsIllegalChars()
        {
            const string Format = @"The value contains characters that are not allowed (control characters, 0xFFFE, or 0xFFFF). Please remove those characters.";
            return Format;
        }

        public static string ValueEqualsToInfinity()
        {
            const string Format = @"The value must be a finite value.";
            return Format;
        }

        public static string SingleBitRequired(object arg0)
        {
            const string Format = @"The value {0} must contain a single bit flag.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string InvalidEnumArgument(object arg0, object arg1, object arg2)
        {
            const string Format = @"The value of argument '{0}' ({1}) is invalid for Enum type '{2}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string ConflictingPathSeparatorForVssFileStorage(object arg0, object arg1, object arg2)
        {
            const string Format = @"There is a conflict with the path separator character '{0}' requested for VssFileStorage at file path: {1}  A previous instance was created with a path separator of '{2}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string ConflictingStringComparerForVssFileStorage(object arg0, object arg1, object arg2)
        {
            const string Format = @"There is a conflict with the string comparer '{0}' requested for VssFileStorage at file path: {1}  A previous instance was created with a string comparer of '{2}'.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1, arg2);
        }

        public static string InvalidClientStoragePath(object arg0, object arg1)
        {
            const string Format = @"The storage path specified is invalid: '{0}'  This storage path cannot be null or empty. It should begin with the '{1}' path separator character, and have no empty path segments.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string CollectionSizeLimitExceeded(object arg0, object arg1)
        {
            const string Format = @"Collection '{0}' can have maximum '{1}' elements.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0, arg1);
        }

        public static string DefaultValueNotAllowed(object arg0)
        {
            const string Format = @"The value {0} must not be set to the default.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string NullElementNotAllowedInCollection()
        {
            const string Format = @"Null elements are not allowed in the collection.";
            return Format;
        }

        public static string InvalidUriError(object arg0)
        {
            const string Format = @"Supplied URI is invalid. The URI should match {0} URI kind format.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string SubjectDescriptorEmpty(object arg0)
        {
            const string Format = @"The subject descriptor specified for parameter {0} must not be empty.";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }

        public static string EUIILeakException(object arg0)
        {
            const string Format = @"Event payload contains EUII. Message: {0}";
            return string.Format(CultureInfo.CurrentCulture, Format, arg0);
        }
    }
}
