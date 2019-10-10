using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Utility for masking common secret patterns
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SecretUtility
    {
        /// <summary>
        /// The string to use to replace secrets when throwing exceptions, logging 
        /// or otherwise risking exposure
        /// </summary>
        internal const string PasswordMask = "******";

        /// <summary>
        /// The string used to mask newer secrets
        /// </summary>
        internal const string SecretMask = "<secret removed>";

        //We use a different mask per token, to help track down suspicious mask sequences in error
        // strings that shouldn't obviously be masked
        // Internal for testing, please don't reuse
        internal const string PasswordRemovedMask = "**password-removed**";
        internal const string PwdRemovedMask = "**pwd-removed**";
        internal const string PasswordSpaceRemovedMask = "**password-space-removed**";
        internal const string PwdSpaceRemovedMask = "**pwd-space-removed**";
        internal const string AccountKeyRemovedMask = "**account-key-removed**";


        /// <summary>
        /// Whether this string contains an unmasked secret
        /// </summary>
        /// <param name="message">The message to check</param>
        /// <returns>True if a secret this class supports was found</returns>
        /// <remarks>This implementation is as least as expensive as a ScrubSecrets call</remarks>
        public static bool ContainsUnmaskedSecret(string message)
        {
            return !String.Equals(message, ScrubSecrets(message, false), StringComparison.Ordinal);
        }


        /// <summary>
        /// Whether this string contains an unmasked secret
        /// </summary>
        /// <param name="message">The message to check</param>
        /// <param name="onlyJwtsFound">True if a secret was found and only jwts were found</param>
        /// <returns>True if a message this class supports was found</returns>
        /// <remarks>This method is a temporary workaround and should be removed in M136
        /// This implementation is as least as expensive as a ScrubSecrets call</remarks>
        public static bool ContainsUnmaskedSecret(string message, out bool onlyJwtsFound)
        {
            if (string.IsNullOrEmpty(message))
            {
                onlyJwtsFound = false;
                return false;
            }

            string scrubbedMessage = ScrubJwts(message, assertOnDetection: false);
            bool jwtsFound = !String.Equals(message, scrubbedMessage, StringComparison.Ordinal);
            scrubbedMessage = ScrubTraditionalSecrets(message, assertOnDetection: false);
            bool secretsFound = !String.Equals(message, scrubbedMessage, StringComparison.Ordinal);
            onlyJwtsFound = !secretsFound && jwtsFound;
            return secretsFound || jwtsFound;
        }

        /// <summary>
        /// Scrub a message for any secrets(passwords, tokens) in known formats
        /// This method is called to scrub exception messages and traces to prevent any secrets 
        /// from being leaked.
        /// </summary>
        /// <param name="message">The message to verify for secret data.</param>
        /// <param name="assertOnDetection">When true, if a message contains a 
        /// secret in a known format the method will debug assert. Default = true.</param>
        /// <returns>The message with any detected secrets masked</returns>
        /// <remarks>This only does best effort pattern matching for a set of known patterns</remarks>
        public static string ScrubSecrets(string message, bool assertOnDetection = true)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            message = ScrubTraditionalSecrets(message, assertOnDetection);
            message = ScrubJwts(message, assertOnDetection);
            return message;
        }

        private static string ScrubTraditionalSecrets(string message, bool assertOnDetection)
        {
            message = ScrubSecret(message, c_passwordToken, PasswordRemovedMask, assertOnDetection);
            message = ScrubSecret(message, c_pwdToken, PwdRemovedMask, assertOnDetection);
            message = ScrubSecret(message, c_passwordTokenSpaced, PasswordSpaceRemovedMask, assertOnDetection);
            message = ScrubSecret(message, c_pwdTokenSpaced, PwdSpaceRemovedMask, assertOnDetection);
            message = ScrubSecret(message, c_accountKeyToken, AccountKeyRemovedMask, assertOnDetection);

            message = ScrubSecret(message, c_authBearerToken, SecretMask, assertOnDetection);
            return message;
        }

        private static string ScrubJwts(string message, bool assertOnDetection)
        {
            //JWTs are sensitive and we need to scrub them, so this is a best effort attempt to 
            // scrub them based on typical patterns we see
            message = ScrubSecret(message, c_jwtTypToken, SecretMask, assertOnDetection,
                maskToken: true);
            message = ScrubSecret(message, c_jwtAlgToken, SecretMask, assertOnDetection,
                maskToken: true);
            message = ScrubSecret(message, c_jwtX5tToken, SecretMask, assertOnDetection,
                maskToken: true);
            message = ScrubSecret(message, c_jwtKidToken, SecretMask, assertOnDetection,
                maskToken: true);
            return message;
        }

        private static string ScrubSecret(string message, string token, string mask, bool assertOnDetection, bool maskToken=false)
        {
            int startIndex = -1;

            do
            {
                startIndex = message.IndexOf(token, (startIndex < 0) ? 0 : startIndex, StringComparison.OrdinalIgnoreCase);
                if (startIndex < 0)
                {
                    // Common case, there is not a password.
                    break;
                }

                //Explicitly check for original password mask so code that uses the orignal doesn't assert
                if (!maskToken && (
                    message.IndexOf(token + mask, StringComparison.OrdinalIgnoreCase) == startIndex
                    || (message.IndexOf(token + PasswordMask, StringComparison.OrdinalIgnoreCase) == startIndex)))
                {
                    // The password is already masked, move past this string.
                    startIndex += token.Length + mask.Length;
                    continue;
                }

                // At this point we detected a password that is not masked, remove it!
                try
                {
                    if (!maskToken)
                    {
                        startIndex += token.Length;
                    }
                    // Find the end of the password.
                    int endIndex = message.Length - 1;

                    if (message[startIndex] == '"' || message[startIndex] == '\'')
                    {
                        // The password is wrapped in quotes.  The end of the string will be the next unpaired quote. 
                        // Unless the message itself wrapped the connection string in quotes, in which case we may mask out the rest of the message.  Better to be safe than leak the connection string.
                        // Intentionally going to "i < message.Length - 1".  If the quote isn't the second to last character, it is the last character, and we delete to the end of the string anyway.
                        for (int i = startIndex + 1; i < message.Length - 1; i++)
                        {
                            if (message[startIndex] == message[i])
                            {
                                if (message[startIndex] == message[i + 1])
                                {
                                    // we found a pair of quotes. Skip over the pair and continue.
                                    i++;
                                    continue;
                                }
                                else
                                {
                                    // this is a single quote, and the end of the password.
                                    endIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // The password is not wrapped in quotes.
                        // The end is any whitespace, semi-colon, single, or double quote character.
                        for (int i = startIndex + 1; i < message.Length; i++)
                        {
                            if (Char.IsWhiteSpace(message[i]) || ((IList<Char>)s_validPasswordEnding).Contains(message[i]))
                            {
                                endIndex = i - 1;
                                break;
                            }
                        }
                    }

                    message = message.Substring(0, startIndex) + mask + message.Substring(endIndex + 1);

                    // Bug 94478: We need to scrub the message before Assert, otherwise we will fall into
                    // a recursive assert where the TeamFoundationServerException contains same message
                    if (assertOnDetection)
                    {
                        Debug.Assert(false, String.Format(CultureInfo.InvariantCulture, "Message contains an unmasked secret. Message: {0}", message));
                    }

                    // Trace raw that we have scrubbed a message.
                    //FUTURE: We need a work item to add Tracing to the VSS Client assembly.
                    //TraceLevel traceLevel = assertOnDetection ? TraceLevel.Error : TraceLevel.Info;
                    //TeamFoundationTracingService.TraceRaw(99230, traceLevel, s_area, s_layer, "An unmasked password was detected in a message. MESSAGE: {0}. STACK TRACE: {1}", message, Environment.StackTrace);
                }
                catch (Exception /*exception*/)
                {
                    // With an exception here the message may still contain an unmasked password.
                    // We also do not want to interupt the current thread with this exception, because it may be constucting a message 
                    // for a different exception. Trace this exception and continue on using a generic exception message.
                    //TeamFoundationTracingService.TraceExceptionRaw(99231, s_area, s_layer, exception);
                }
                finally
                {
                    // Iterate to the next password (if it exists)
                    startIndex += mask.Length;
                }
            } while (startIndex < message.Length);

            return message;
        }

        private const string c_passwordToken = "Password=";
        private const string c_passwordTokenSpaced = "-Password ";
        private const string c_pwdToken = "Pwd=";
        private const string c_pwdTokenSpaced = "-Pwd ";
        private const string c_accountKeyToken = "AccountKey=";
        private const string c_authBearerToken = "Bearer ";
        /// <remarks>
        /// {"typ":" // eyJ0eXAiOi
        /// </remarks>
        private const string c_jwtTypToken = "eyJ0eXAiOi";
        /// <remarks>
        /// {"alg":" // eyJhbGciOi
        /// </remarks>
        private const string c_jwtAlgToken = "eyJhbGciOi";
        /// <remarks>
        /// {"x5t":" // eyJ4NXQiOi
        /// </remarks>
        private const string c_jwtX5tToken = "eyJ4NXQiOi";
        /// <remarks>
        /// {"kid":" // eyJraWQiOi
        /// </remarks>
        private const string c_jwtKidToken = "eyJraWQiOi";



        private static readonly char[] s_validPasswordEnding = new char[] { ';', '\'', '"' };
    }
}
