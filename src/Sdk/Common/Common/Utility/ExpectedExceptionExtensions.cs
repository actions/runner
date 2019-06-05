using System;

namespace GitHub.Services.Common
{
    public static class ExpectedExceptionExtensions
    {
        private const string c_expectedKey = "isExpected";

        /// <summary>
        /// Mark the exception as expected when caused by user input in the provided area.
        /// If the exception thrower is the same area as the caller, the exception will be treated as expected.
        /// However, in the case of a service to service call, then the exception will be treated as unexpected.
        /// ex: GitRefsController throws ArgumentException called directly by a user then the exception will be expected
        ///     GitRefsController throws ArgumentException called by BuildDefinitionController then the exception will not be expected.
        /// </summary>
        /// <remarks>
        /// This allows for the use case "throw new ArgumentException().Expected(c_area)" 
        /// This will overwrite the expected area if called a second time. 
        /// This should not throw any exceptions as to avoid hiding the exception that was already caught. 
        /// See https://vsowiki.com/index.php?title=Whitelisting_Expected_Commands_and_Exceptions 
        /// </remarks> 
        /// <param name="area">The area name where the exception is expected. This will be compared against IVssRequestContext.ServiceName. Area should be non-empty</param>
        /// <returns><paramref name="ex"/> after setting the area</returns>
        public static Exception Expected(this Exception ex, string area)
        {
            if (!string.IsNullOrEmpty(area))
            {
                ex.Data[c_expectedKey] = area;
            }

            return ex;
        }

        /// <summary>
        /// Use this to "expect" an exception within the exception filtering syntax.
        /// ex:
        ///     catch(ArgumentException ex) when (ex.ExpectedExceptionFilter(c_area))
        /// See <seealso cref="Expected(Exception, string)"/>
        /// </summary>
        /// <returns>false always</returns>
        public static bool ExpectedExceptionFilter(this Exception ex, string area)
        {
            ex.Expected(area);
            return false;
        }

        /// <summary>
        /// Determine if the exception is expected in the specified area.
        /// Case is ignored for the area comparison.
        /// </summary>
        public static bool IsExpected(this Exception ex, string area)
        {
            if (string.IsNullOrEmpty(area))
            {
                return false;
            }

            // An exception's Data property is an IDictionary, which returns null for keys that do not exist.
            return area.Equals(ex.Data[c_expectedKey] as string, StringComparison.OrdinalIgnoreCase);
        }
    }
}
