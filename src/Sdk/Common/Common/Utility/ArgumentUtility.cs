using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.Common
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ArgumentUtility
    {
        /// <summary>
        /// Throw an exception if the object is null.
        /// </summary>
        /// <param name="var">the object to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForNull(Object var, String varName)
        {
            CheckForNull(var, varName, null);
        }

        /// <summary>
        /// Throw an exception if the object is null.
        /// </summary>
        /// <param name="var">the object to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForNull(Object var, String varName, String expectedServiceArea)
        {
            if (var == null)
            {
                throw new ArgumentNullException(varName).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Throw an exception if a string is null or empty.
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        public static void CheckStringForNullOrEmpty(String stringVar, String stringVarName)
        {
            CheckStringForNullOrEmpty(stringVar, stringVarName, null);
        }

        /// <summary>
        /// Throw an exception if a string is null or empty.
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckStringForNullOrEmpty(String stringVar, String stringVarName, String expectedServiceArea)
        {
            CheckStringForNullOrEmpty(stringVar, stringVarName, false, expectedServiceArea);
        }

        public static void CheckForNonnegativeInt(int var, String varName)
        {
            CheckForNonnegativeInt(var, varName, null);
        }

        public static void CheckForNonnegativeInt(int var, String varName, String expectedServiceArea)
        {
            if (var < 0)
            {
                throw new ArgumentOutOfRangeException(varName).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Throws and exception if an integer is less than 1
        /// </summary>
        /// <param name="var">integer to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        public static void CheckForNonPositiveInt(int var, String varName)
        {
            CheckForNonPositiveInt(var, varName, null);
        }

        /// <summary>
        /// Throws and exception if an integer is less than 1
        /// </summary>
        /// <param name="var">integer to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckForNonPositiveInt(int var, String varName, String expectedServiceArea)
        {
            if (var <= 0)
            {
                throw new ArgumentOutOfRangeException(varName).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Throw an exception if a string is null or empty.
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        /// <param name="trim">If true, will trim the string after it is determined not to be null</param>
        public static void CheckStringForNullOrEmpty(String stringVar, String stringVarName, bool trim)
        {
            CheckStringForNullOrEmpty(stringVar, stringVarName, trim, null);
        }

        /// <summary>
        /// Throw an exception if a string is null or empty.
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        /// <param name="trim">If true, will trim the string after it is determined not to be null</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckStringForNullOrEmpty(String stringVar, String stringVarName, bool trim, String expectedServiceArea)
        {
            CheckForNull(stringVar, stringVarName, expectedServiceArea);
            if (trim == true)
            {
                stringVar = stringVar.Trim();
            }
            if (stringVar.Length == 0)
            {
                throw new ArgumentException(CommonResources.EmptyStringNotAllowed(), stringVarName).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Throw an exception if a string is null, too short, or too long.
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        /// <param name="maxLength">Maximum allowed string length</param>
        /// <param name="minLength">Minimum allowed string length</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckStringLength(
            string stringVar,
            string stringVarName,
            int maxLength,
            int minLength = 0,
            string expectedServiceArea = null)
        {
            CheckForNull(stringVar, stringVarName, expectedServiceArea);

            if (stringVar.Length < minLength || stringVar.Length > maxLength)
            {
                throw new ArgumentException(
                    CommonResources.StringLengthNotAllowed(stringVarName, minLength, maxLength),
                    stringVarName)
                    .Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Check a Collection for the Max Length
        /// </summary>
        /// <param name="collection">enumerable to check</param>
        /// <param name="collectionName">the variable or parameter name to display</param>
        /// <param name="maxLength">Max allowed Length</param>
        public static void CheckCollectionForMaxLength<T>(ICollection<T> collection, string collectionName, int maxLength)
        {
            if (collection?.Count > maxLength)
            {
                throw new ArgumentException(CommonResources.CollectionSizeLimitExceeded(collectionName, maxLength));
            }
        }

        /// <summary>
        /// Throw an exception if IEnumerable is null or empty.
        /// </summary>
        /// <param name="enumerable">enumerable to check</param>
        /// <param name="enumerableName">the variable or parameter name to display</param>
        public static void CheckEnumerableForNullOrEmpty(IEnumerable enumerable, String enumerableName)
        {
            CheckEnumerableForNullOrEmpty(enumerable, enumerableName, null);
        }

        /// <summary>
        /// Throw an exception if IEnumerable is null or empty.
        /// </summary>
        /// <param name="enumerable">enumerable to check</param>
        /// <param name="enumerableName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckEnumerableForNullOrEmpty(IEnumerable enumerable, String enumerableName, String expectedServiceArea)
        {
            CheckForNull(enumerable, enumerableName, expectedServiceArea);

            IEnumerator enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException(CommonResources.EmptyCollectionNotAllowed(), enumerableName).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Throw an exception if IEnumerable contains a null element.
        /// </summary>
        /// <param name="enumerable">enumerable to check</param>
        /// <param name="enumerableName">the variable or parameter name to display</param>
        public static void CheckEnumerableForNullElement(IEnumerable enumerable, String enumerableName)
        {
            CheckEnumerableForNullElement(enumerable, enumerableName, null);
        }

        /// <summary>
        /// Throw an exception if IEnumerable contains a null element.
        /// </summary>
        /// <param name="enumerable">enumerable to check</param>
        /// <param name="enumerableName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckEnumerableForNullElement(IEnumerable enumerable, String enumerableName, String expectedServiceArea)
        {
            CheckForNull(enumerable, enumerableName, expectedServiceArea);

            IEnumerator enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current == null)
                {
                    throw new ArgumentException(CommonResources.NullElementNotAllowedInCollection(), enumerableName).Expected(expectedServiceArea);
                }
            }
        }

        /// <summary>
        /// Throw an exception if the guid is equal to Guid.Empty.
        /// </summary>
        /// <param name="guid">the guid to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        public static void CheckForEmptyGuid(Guid guid, String varName)
        {
            CheckForEmptyGuid(guid, varName, null);
        }

        /// <summary>
        /// Throw an exception if the guid is equal to Guid.Empty.
        /// </summary>
        /// <param name="guid">the guid to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckForEmptyGuid(Guid guid, String varName, String expectedServiceArea)
        {
            if (guid.Equals(Guid.Empty))
            {
                throw new ArgumentException(CommonResources.EmptyGuidNotAllowed(varName), varName).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Throw an exception if the value contains more than one bit set.
        /// </summary>
        /// <param name="value">the value to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        public static void CheckForMultipleBits(int value, String varName)
        {
            CheckForMultipleBits(value, varName, null);
        }

        /// <summary>
        /// Throw an exception if the value contains more than one bit set.
        /// </summary>
        /// <param name="value">the value to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckForMultipleBits(int value, String varName, String expectedServiceArea)
        {
            if (0 == value ||
                (value & (value - 1)) != 0)
            {
                throw new ArgumentException(CommonResources.SingleBitRequired(varName), varName).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Throw an exception if the value equals the default for the type.
        /// </summary>
        /// <param name="value">the value to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        public static void CheckForDefault<T>(T value, String varName)
        {
            if (EqualityComparer<T>.Default.Equals(value, default(T)))
            {
                throw new ArgumentException(CommonResources.DefaultValueNotAllowed(varName), varName);
            }
        }

        /// <summary>
        /// Checks if character is not displayable.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="allowCrLf">Carriage return and line-feed is considered legal if the allowCrLf parameter is set to true.</param>
        /// <remarks>A character is "not displayable" if it's UnicodeCategory is in the set {LineSeparator, ParagraphSeparator, Control, Format, OtherNotAssigned}.</remarks>
        public static bool IsIllegalInputCharacter(char c, Boolean allowCrLf = false)
        {
            if (allowCrLf && (c == '\r' || c == '\n'))
            {
                return false;
            }

            UnicodeCategory cat = Char.GetUnicodeCategory(c);

            // see http://www.w3.org/TR/REC-xml/#charsets
            return (cat == UnicodeCategory.LineSeparator
                || cat == UnicodeCategory.ParagraphSeparator
                || cat == UnicodeCategory.Control
                || cat == UnicodeCategory.Format
                || cat == UnicodeCategory.OtherNotAssigned);
        }

        /// <summary>
        /// Replace illegal characters with specified character. A character is considered illegal as per definition of <see cref="IsIllegalInputCharacter"/>
        /// </summary>
        public static string ReplaceIllegalCharacters(string str, char replaceWith, bool allowCrLf = false)
        {
            if (IsIllegalInputCharacter(replaceWith, allowCrLf))
            {
                throw new ArgumentException(CommonResources.VssInvalidUnicodeCharacter((int)replaceWith), nameof(replaceWith));
            }

            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            char[] strArray = str.ToCharArray();
            for (int i = 0; i < strArray.Length; i++)
            {
                if (IsIllegalInputCharacter(strArray[i], allowCrLf: allowCrLf))
                {
                    strArray[i] = replaceWith;
                }
            }

            return new string(strArray);
        }

        /// <summary>
        /// Checks for invalid unicode characters
        /// </summary>
        /// <param name="stringVar"></param>
        /// <param name="stringVarName"></param>
        public static void CheckStringForInvalidCharacters(String stringVar, String stringVarName)
        {
            CheckStringForInvalidCharacters(stringVar, stringVarName, false, null);
        }

        /// <summary>
        /// Checks for invalid unicode characters
        /// </summary>
        /// <param name="stringVar"></param>
        /// <param name="stringVarName"></param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckStringForInvalidCharacters(String stringVar, String stringVarName, String expectedServiceArea)
        {
            CheckStringForInvalidCharacters(stringVar, stringVarName, false, expectedServiceArea);
        }

        /// <summary>
        /// Checks for invalid unicode characters
        /// </summary>
        /// <param name="stringVar"></param>
        /// <param name="stringVarName"></param>
        /// <param name="allowCrLf"></param>
        public static void CheckStringForInvalidCharacters(String stringVar, String stringVarName, Boolean allowCrLf)
        {
            CheckStringForInvalidCharacters(stringVar, stringVarName, allowCrLf, null);
        }

        /// <summary>
        /// Checks for invalid unicode characters
        /// </summary>
        /// <param name="stringVar"></param>
        /// <param name="stringVarName"></param>
        /// <param name="allowCrLf"></param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckStringForInvalidCharacters(String stringVar, String stringVarName, Boolean allowCrLf, String expectedServiceArea)
        {
            Debug.Assert(!String.IsNullOrEmpty(stringVarName), "!String.IsNullOrEmpty(stringVarName)");

            ArgumentUtility.CheckForNull(stringVar, stringVarName);

            for (int i = 0; i < stringVar.Length; i++)
            {
                if (IsIllegalInputCharacter(stringVar[i], allowCrLf))
                {
                    throw new ArgumentException(CommonResources.VssInvalidUnicodeCharacter((int)stringVar[i]), stringVarName).Expected(expectedServiceArea);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stringVar"></param>
        /// <param name="stringVarName"></param>
        /// <param name="invalidCharacters"></param>
        public static void CheckStringForInvalidCharacters(String stringVar, String stringVarName, Char[] invalidCharacters)
        {
            CheckStringForInvalidCharacters(stringVar, stringVarName, invalidCharacters, null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stringVar"></param>
        /// <param name="stringVarName"></param>
        /// <param name="invalidCharacters"></param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckStringForInvalidCharacters(String stringVar, String stringVarName, Char[] invalidCharacters, String expectedServiceArea)
        {
            Debug.Assert(null != stringVar, "null != stringVar");
            Debug.Assert(!String.IsNullOrEmpty(stringVarName), "!String.IsNullOrEmpty(stringVarName)");
            Debug.Assert(invalidCharacters != null, "invalidCharacters != null");

            ArgumentUtility.CheckForNull(stringVar, stringVarName);

            for (int i = 0; i < invalidCharacters.Length; i++)
            {
                if (stringVar.IndexOf(invalidCharacters[i]) >= 0)
                {
                    throw new ArgumentException(CommonResources.StringContainsInvalidCharacters(invalidCharacters[i]), stringVarName).Expected(expectedServiceArea);
                }
            }
        }

        /// <summary>
        /// Checks for escape sequences that are invalid in SQL
        /// </summary>
        /// <param name="stringVar">The value to be checked</param>
        /// <param name="stringVarName">The name of the value to be checked</param>
        /// <param name="expectedServiceArea">The service area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckStringForInvalidSqlEscapeCharacters(String stringVar, String stringVarName, String expectedServiceArea = null)
        {
            Debug.Assert(!String.IsNullOrEmpty(stringVar), "null != stringVar");
            Debug.Assert(!String.IsNullOrEmpty(stringVarName), "!String.IsNullOrEmpty(stringVarName)");

            ArgumentUtility.CheckStringForNullOrEmpty(stringVar, stringVarName);

            for (int i = 0; i < stringVar.Length - 1; i++)
            {
                if (stringVar[i] == '\\')
                {
                    // Make sure the next character after the slash is a valid escape character
                    char escapedCharacter = stringVar[++i];
                    if (escapedCharacter != '*' && escapedCharacter != '?' && escapedCharacter != '\\')
                    {
                        throw new ArgumentException(CommonResources.StringContainsInvalidCharacters('\\'), stringVarName).Expected(expectedServiceArea);
                    }
                }
            }
        }

        public static void CheckBoundsInclusive(Int32 value, Int32 minValue, Int32 maxValue, String varName)
        {
            CheckBoundsInclusive(value, minValue, maxValue, varName, null);
        }

        public static void CheckBoundsInclusive(Int32 value, Int32 minValue, Int32 maxValue, String varName, String expectedServiceArea)
        {
            Debug.Assert(!String.IsNullOrEmpty(varName), "!String.IsNullOrEmpty(stringVarName)");

            if (value < minValue || value > maxValue)
            {
                throw new ArgumentOutOfRangeException(varName, CommonResources.ValueOutOfRange(value, varName, minValue, maxValue)).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the value is out of range.
        /// </summary>
        /// <typeparam name="T">The comparable type</typeparam>
        /// <param name="var">the value to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange<T>(T var, string varName, T minimum)
            where T : IComparable<T>
        {
            CheckForOutOfRange(var, varName, minimum, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the value is out of range.
        /// </summary>
        /// <typeparam name="T">The comparable type</typeparam>
        /// <param name="var">the value to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange<T>(T var, string varName, T minimum, String expectedServiceArea)
            where T : IComparable<T>
        {
            ArgumentUtility.CheckForNull(var, varName, expectedServiceArea);
            if (var.CompareTo(minimum) < 0)
            {
                throw new ArgumentOutOfRangeException(varName, var, CommonResources.OutOfRange(var)).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the value is out of range.
        /// </summary>
        /// <typeparam name="T">The comparable type</typeparam>
        /// <param name="var">the value to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="maximum">maximum legal value</param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange<T>(T var, string varName, T minimum, T maximum)
            where T : IComparable<T>
        {
            CheckForOutOfRange(var, varName, minimum, maximum, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the value is out of range.
        /// </summary>
        /// <typeparam name="T">The comparable type</typeparam>
        /// <param name="var">the value to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="maximum">maximum legal value</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange<T>(T var, string varName, T minimum, T maximum, String expectedServiceArea)
            where T : IComparable<T>
        {
            CheckForNull(var, varName, expectedServiceArea);
            if (var.CompareTo(minimum) < 0 || var.CompareTo(maximum) > 0)
            {
                throw new ArgumentOutOfRangeException(varName, var, CommonResources.OutOfRange(var)).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the integer is out of range.
        /// </summary>
        /// <param name="var">the int to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange(int var, String varName, int minimum)
        {
            CheckForOutOfRange(var, varName, minimum, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the integer is out of range.
        /// </summary>
        /// <param name="var">the int to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange(int var, String varName, int minimum, String expectedServiceArea)
        {
            if (var < minimum)
            {
                throw new ArgumentOutOfRangeException(varName, var, CommonResources.OutOfRange(var)).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the integer is out of range.
        /// </summary>
        /// <param name="var">the int to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="maximum">maximum legal value</param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange(int var, String varName, int minimum, int maximum)
        {
            CheckForOutOfRange(var, varName, minimum, maximum, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the integer is out of range.
        /// </summary>
        /// <param name="var">the int to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="maximum">maximum legal value</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange(int var, String varName, int minimum, int maximum, String expectedServiceArea)
        {
            if (var < minimum || var > maximum)
            {
                throw new ArgumentOutOfRangeException(varName, var, CommonResources.OutOfRange(var)).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the integer is out of range.
        /// </summary>
        /// <param name="var">the int to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange(long var, String varName, long minimum)
        {
            CheckForOutOfRange(var, varName, minimum, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the integer is out of range.
        /// </summary>
        /// <param name="var">the int to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange(long var, String varName, long minimum, String expectedServiceArea)
        {
            if (var < minimum)
            {
                throw new ArgumentOutOfRangeException(varName, var, CommonResources.OutOfRange(var)).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the integer is out of range.
        /// </summary>
        /// <param name="var">the int to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="maximum">maximum legal value</param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange(long var, String varName, long minimum, long maximum)
        {
            CheckForOutOfRange(var, varName, minimum, maximum, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the integer is out of range.
        /// </summary>
        /// <param name="var">the int to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="maximum">maximum legal value</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckForOutOfRange(long var, String varName, long minimum, long maximum, String expectedServiceArea)
        {
            if (var < minimum || var > maximum)
            {
                throw new ArgumentOutOfRangeException(varName, var, CommonResources.OutOfRange(var)).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the date is not in the range.
        /// </summary>
        /// <param name="var">the DateTime to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="maximum"></param>
        //********************************************************************************************
        public static void CheckForDateTimeRange(DateTime var, String varName, DateTime minimum, DateTime maximum)
        {
            CheckForDateTimeRange(var, varName, minimum, maximum, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the date is not in the range.
        /// </summary>
        /// <param name="var">the DateTime to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="minimum">minimum legal value</param>
        /// <param name="maximum"></param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        public static void CheckForDateTimeRange(DateTime var, String varName, DateTime minimum, DateTime maximum, String expectedServiceArea)
        {
            if (var < minimum || var > maximum)
            {
                throw new ArgumentOutOfRangeException(varName, var, CommonResources.OutOfRange(var)).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throws an exception if the provided value is less than zero.
        /// </summary>
        /// <param name="value">value to check</param>
        /// <param name="valueName">the variable or parameter name to display</param>
        //********************************************************************************************
        public static void CheckGreaterThanOrEqualToZero(float value, string valueName)
        {
            CheckGreaterThanOrEqualToZero(value, valueName, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throws an exception if the provided value is less than zero.
        /// </summary>
        /// <param name="value">value to check</param>
        /// <param name="valueName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        public static void CheckGreaterThanOrEqualToZero(float value, string valueName, String expectedServiceArea)
        {
            if (value < 0)
            {
                throw new ArgumentException(CommonResources.ValueMustBeGreaterThanZero(), valueName).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throws an exception if the provided value is less than or equal to zero.
        /// </summary>
        /// <param name="value">value to check</param>
        /// <param name="enumerableName">the variable or parameter name to display</param>
        //********************************************************************************************
        public static void CheckGreaterThanZero(float value, string valueName)
        {
            CheckGreaterThanZero(value, valueName, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throws an exception if the provided value is less than or equal to zero.
        /// </summary>
        /// <param name="value">value to check</param>
        /// <param name="enumerableName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        public static void CheckGreaterThanZero(float value, string valueName, String expectedServiceArea)
        {
            if (value <= 0)
            {
                throw new ArgumentException(CommonResources.ValueMustBeGreaterThanZero(), valueName).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the object is not null.
        /// </summary>
        /// <param name="var">the object to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        //********************************************************************************************
        public static void EnsureIsNull(Object var, String varName)
        {
            EnsureIsNull(var, varName, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the object is not null.
        /// </summary>
        /// <param name="var">the object to check</param>
        /// <param name="varName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        public static void EnsureIsNull(Object var, String varName, String expectedServiceArea)
        {
            if (var != null)
            {
                throw new ArgumentException(CommonResources.NullValueNecessary(varName)).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the string is not entirely of a specified casing (lowercase, uppercase).
        /// </summary>
        /// <param name="stringVar">The string to check.</param>
        /// <param name="varName">The variable or parameter name to display.</param>
        /// <param name="checkForLowercase">Indicates whether the check should require
        /// lowercase characters, as opposed to uppercase characters.</param>
        //********************************************************************************************
        public static void CheckStringCasing(String stringVar, String varName, Boolean checkForLowercase = true)
        {
            CheckStringCasing(stringVar, varName, checkForLowercase, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if the string is not entirely of a specified casing (lowercase, uppercase).
        /// </summary>
        /// <param name="stringVar">The string to check.</param>
        /// <param name="varName">The variable or parameter name to display.</param>
        /// <param name="checkForLowercase">Indicates whether the check should require
        /// lowercase characters, as opposed to uppercase characters.</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        public static void CheckStringCasing(String stringVar, String varName, Boolean checkForLowercase = true, String expectedServiceArea = null)
        {
            foreach (Char c in stringVar)
            {
                if (Char.IsLetter(c) == true &&
                    Char.IsLower(c) == !checkForLowercase)
                {
                    throw new ArgumentException(
                            checkForLowercase ?
                            CommonResources.LowercaseStringRequired(varName) :
                            CommonResources.UppercaseStringRequired(varName))
                         .Expected(expectedServiceArea);
                }
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if IEnumerable is empty.
        /// </summary>
        /// <param name="enumerable">enumerable to check</param>
        /// <param name="enumerableName">the variable or parameter name to display</param>
        //********************************************************************************************
        public static void CheckEnumerableForEmpty(IEnumerable enumerable, String enumerableName)
        {
            CheckEnumerableForEmpty(enumerable, enumerableName, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if IEnumerable is empty.
        /// </summary>
        /// <param name="enumerable">enumerable to check</param>
        /// <param name="enumerableName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        public static void CheckEnumerableForEmpty(IEnumerable enumerable, String enumerableName, String expectedServiceArea)
        {
            if (enumerable != null)
            {
                IEnumerator enumerator = enumerable.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    throw new ArgumentException(CommonResources.EmptyArrayNotAllowed(), enumerableName).Expected(expectedServiceArea);
                }
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if a string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        //********************************************************************************************
        public static void CheckStringForNullOrWhiteSpace(String stringVar, String stringVarName)
        {
            CheckStringForNullOrWhiteSpace(stringVar, stringVarName, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if a string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        public static void CheckStringForNullOrWhiteSpace(String stringVar, String stringVarName, String expectedServiceArea)
        {
            CheckForNull(stringVar, stringVarName, expectedServiceArea);
            if (String.IsNullOrWhiteSpace(stringVar) == true)
            {
                throw new ArgumentException(CommonResources.EmptyOrWhiteSpaceStringNotAllowed(), stringVarName).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if a string length is not given value.
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="length">length to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        //********************************************************************************************
        public static void CheckStringExactLength(String stringVar, int length, String stringVarName)
        {
            CheckStringExactLength(stringVar, length, stringVarName, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if a string length is not given value.
        /// </summary>
        /// <param name="stringVar">string to check</param>
        /// <param name="length">length to check</param>
        /// <param name="stringVarName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        public static void CheckStringExactLength(String stringVar, int length, String stringVarName, String expectedServiceArea)
        {
            CheckForNull(stringVar, stringVarName, expectedServiceArea);

            if (stringVar.Length != length)
            {
                throw new ArgumentException(CommonResources.StringLengthNotMatch(length), stringVarName).Expected(expectedServiceArea);
            }
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if one of the strings is not null or empty.
        /// </summary>
        /// <param name="var1">the first object to check</param>
        /// <param name="varName1">the variable or parameter name to display for the first object</param>
        /// <param name="var2">the second object to check</param>
        /// <param name="varName2">the variable or parameter name to display for the second object</param>
        //********************************************************************************************
        public static void CheckForBothStringsNullOrEmpty(String var1, String varName1, String var2, String varName2)
        {
            CheckForBothStringsNullOrEmpty(var1, varName1, var2, varName2, null);
        }

        //********************************************************************************************
        /// <summary>
        /// Throw an exception if one of the strings is not null or empty.
        /// </summary>
        /// <param name="var1">the first object to check</param>
        /// <param name="varName1">the variable or parameter name to display for the first object</param>
        /// <param name="var2">the second object to check</param>
        /// <param name="varName2">the variable or parameter name to display for the second object</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        //********************************************************************************************
        public static void CheckForBothStringsNullOrEmpty(String var1, String varName1, String var2, String varName2, String expectedServiceArea)
        {
            if (String.IsNullOrEmpty(var1) && String.IsNullOrEmpty(var2))
            {
                throw new ArgumentException(CommonResources.BothStringsCannotBeNull(varName1, varName2)).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Checks if a string contains any whitespace characters. Throws an exception if it does.
        /// </summary>
        /// <param name="stringVar"></param>
        /// <param name="stringVarName"></param>
        public static void CheckStringForAnyWhiteSpace(string stringVar, string stringVarName)
        {
            CheckStringForAnyWhiteSpace(stringVar, stringVarName, null);
        }

        /// <summary>
        /// Checks if a string contains any whitespace characters. Throws an exception if it does.
        /// </summary>
        /// <param name="stringVar"></param>
        /// <param name="stringVarName"></param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckStringForAnyWhiteSpace(string stringVar, string stringVarName, String expectedServiceArea)
        {
            if (stringVar != null)
            {
                for (Int32 i = 0; i < stringVar.Length; i++)
                {
                    if (Char.IsWhiteSpace(stringVar[i]))
                    {
                        throw new ArgumentException(CommonResources.WhiteSpaceNotAllowed(), stringVarName).Expected(expectedServiceArea);
                    }
                }
            }
        }

        /// <summary>
        /// Performs a type check on the variable, and throws if there is a mismatch
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="var"></param>
        /// <param name="varName"></param>
        /// <param name="typeName"></param>
        public static void CheckType<T>(object var, string varName, string typeName)
        {
            CheckType<T>(var, varName, typeName, null);
        }

        /// <summary>
        /// Performs a type check on the variable, and throws if there is a mismatch
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="var"></param>
        /// <param name="varName"></param>
        /// <param name="typeName"></param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckType<T>(object var, string varName, string typeName, String expectedServiceArea)
        {
            if (!(var is T))
            {
                throw new ArgumentException(CommonResources.UnexpectedType(varName, typeName)).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Checks if an enum value is defined on the enum type
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum</typeparam>
        /// <param name="value">The enum value</param>
        /// <param name="enumVarName">The name of the enum argument</param>
        public static void CheckForDefinedEnum<TEnum>(TEnum value, string enumVarName)
            where TEnum : struct
        {
            CheckForDefinedEnum(value, enumVarName, null);
        }

        /// <summary>
        /// Checks if an enum value is defined on the enum type
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum</typeparam>
        /// <param name="value">The enum value</param>
        /// <param name="enumVarName">The name of the enum argument</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckForDefinedEnum<TEnum>(TEnum value, string enumVarName, String expectedServiceArea)
            where TEnum : struct
        {
            // IsEnumDefined throws ArgumentException if TEnum is not an enum type
            if (!typeof(TEnum).IsEnumDefined(value))
            {
                throw new global::System.ComponentModel.InvalidEnumArgumentException(enumVarName, (int)(object)value, typeof(TEnum)).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Determines if a string value is a valid email address. Does NOT throw.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        public static Boolean IsValidEmailAddress(string emailAddress)
        {
            // WARNING: If you switch this to code to use the MailAddress class for validation,
            // you need to evaluate all callers to see if they handle inputs like these:
            // "John Smith <jsmith@hotmail.com>"
            // "<jsmith@hotmail.com>"
            //
            // The MailAddress constructor supports those strings.

            return s_emailPattern.IsMatch(emailAddress);
        }

        /// <summary>
        /// Checks if a string is a valid email address. Throws an exception otherwise.
        /// </summary>
        /// <param name="stringVar"></param>
        /// <param name="stringVarName"></param>
        public static void CheckEmailAddress(string stringVar, string stringVarName)
        {
            CheckEmailAddress(stringVar, stringVarName, null);
        }

        /// <summary>
        /// Checks if a string is a valid email address. Throws an exception otherwise.
        /// </summary>
        /// <param name="stringVar"></param>
        /// <param name="stringVarName"></param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckEmailAddress(string stringVar, string stringVarName, String expectedServiceArea)
        {
            if (!IsValidEmailAddress(stringVar))
            {
                throw new ArgumentException(CommonResources.InvalidEmailAddressError(), stringVarName).Expected(expectedServiceArea);
            }
        }

        /// <summary>
        /// Checks if a string value is a valid URI in accordance with RFC 3986 and RFC 3987. Throws an exception otherwise.
        /// </summary>
        /// <param name="uriString"></param>
        /// <param name="uriKind"></param>
        /// <param name="stringVarName"></param>
        public static void CheckIsValidURI(string uriString, UriKind uriKind, string stringVarName)
        {
            if (!Uri.IsWellFormedUriString(uriString, uriKind))
            {
                throw new ArgumentException(CommonResources.InvalidUriError(uriKind), stringVarName);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stringArrayVar"></param>
        /// <param name="stringArrayVarName"></param>
        public static void CheckStringForInvalidCharacters(string[] stringArrayVar, string stringArrayVarName)
        {
            CheckStringForInvalidCharacters(stringArrayVar, stringArrayVarName, null);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stringArrayVar"></param>
        /// <param name="stringArrayVarName"></param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckStringForInvalidCharacters(string[] stringArrayVar, string stringArrayVarName, String expectedServiceArea)
        {
            CheckStringForInvalidCharacters(stringArrayVar, stringArrayVarName, false, expectedServiceArea);
        }

        /// <summary>
        /// Checks for invalid unicode characters
        /// </summary>
        /// <param name="stringArrayVar"></param>
        /// <param name="stringArrayVarName"></param>
        /// <param name="allowCrLf"></param>
        public static void CheckStringForInvalidCharacters(string[] stringArrayVar, string stringArrayVarName, Boolean allowCrLf)
        {
            CheckStringForInvalidCharacters(stringArrayVar, stringArrayVarName, allowCrLf, null);
        }

        /// <summary>
        /// Checks for invalid unicode characters
        /// </summary>
        /// <param name="stringArrayVar"></param>
        /// <param name="stringArrayVarName"></param>
        /// <param name="allowCrLf"></param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckStringForInvalidCharacters(string[] stringArrayVar, string stringArrayVarName, Boolean allowCrLf, String expectedServiceArea)
        {
            Debug.Assert(null != stringArrayVar, "null != stringArrayVar");
            Debug.Assert(stringArrayVar.Length > 0, "stringArrayVar.Length > 0");
            Debug.Assert(!String.IsNullOrEmpty(stringArrayVarName), "!String.IsNullOrEmpty(stringArrayVarName)");

            for (int i = 0; i < stringArrayVar.Length; i++)
            {
                CheckStringForInvalidCharacters(stringArrayVar[i], String.Format(CultureInfo.InvariantCulture, "{0}[{1}]", stringArrayVarName, i), allowCrLf, expectedServiceArea);
            }
        }

        /// <summary>
        /// Throws an exception if the provided value equals to infinity.
        /// </summary>
        /// <param name="value">value to check</param>
        /// <param name="valueName">the variable or parameter name to display</param>
        public static void CheckValueEqualsToInfinity(float value, string valueName)
        {
            CheckValueEqualsToInfinity(value, valueName, null);
        }

        /// <summary>
        /// Throws an exception if the provided value equals to infinity.
        /// </summary>
        /// <param name="value">value to check</param>
        /// <param name="valueName">the variable or parameter name to display</param>
        /// <param name="expectedServiceArea">the Service Area where this exception is expected due to user input. See <see cref="ExpectedExceptionExtensions"/></param>
        public static void CheckValueEqualsToInfinity(float value, string valueName, String expectedServiceArea)
        {
            if (float.IsInfinity(value))
            {
                throw new ArgumentException(CommonResources.ValueEqualsToInfinity(), valueName).Expected(expectedServiceArea);
            }
        }

        private static readonly Regex s_emailPattern = new Regex(@"^([a-z0-9.!#$%&'*+/=?^_`{|}~-]+)@((\[[0-9]{1,3}" +
            @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
            @".)+))([a-z]{2,63}|[0-9]{1,3})(\]?)$", RegexOptions.IgnoreCase);

        public static bool IsInvalidString(string strIn)
        {
            return IsInvalidString(strIn, false);
        }

        public static bool IsInvalidString(string strIn, Boolean allowCrLf)
        {
            ArgumentUtility.CheckForNull(strIn, "strIn");

            foreach (char c in strIn)
            {
                if (ArgumentUtility.IsIllegalInputCharacter(c, allowCrLf))
                {
                    return true;
                }
            }

            if (HasMismatchedSurrogates(strIn) == true)
            {
                return true;
            }

            return false;
        }

        public static bool HasSurrogates(string strIn)
        {
            for (int i = 0; i < strIn.Length; i++)
            {
                Char c = strIn[i];

                if (char.IsSurrogate(c) == true)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasMismatchedSurrogates(string strIn)
        {
            for (int i = 0; i < strIn.Length; i++)
            {
                Char c = strIn[i];

                // If this is a low surrogate, that means that there wasn't a preceeding high
                // surrogate, and it is invalid
                if (Char.IsLowSurrogate(c))
                {
                    return true;
                }

                // is this the start of a surrogate pair?
                if (Char.IsHighSurrogate(c))
                {
                    if (!Char.IsSurrogatePair(strIn, i))
                    {
                        return true;
                    }

                    // skip the low surogate
                    i++;
                }
            }
            return false;
        }
    }
}
