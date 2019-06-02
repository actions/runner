using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;

namespace Microsoft.VisualStudio.Services.Common.Internal
{
    /// <summary>
    /// Utility class for general Uri actions.  See LinkingUtilities for artifact uri specific methods.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HtmlUtility
    {
        public static string CreateAutoSubmitForm(
            string uriString,
            IDictionary<string, string> inputs,
            string title = null)
        {
            ArgumentUtility.CheckForNull(uriString, nameof(uriString));
            ArgumentUtility.CheckForNull(inputs, nameof(inputs));

            var inputBuilder = new StringBuilder();
            foreach (var input in inputs)
            {
                inputBuilder.Append($@"<input type=""hidden"" name=""{input.Key}"" value=""{input.Value}"" />");
            }

            var form = $@"
                <html>
                    <head>
                        <title>{title}</title>
                    </head>
                    <body>
                        <form method=""POST"" name=""hiddenform"" action=""{uriString}"" >
                            {inputBuilder.ToString()}
                            <noscript>
                                <p>Script is disabled. Click Submit to continue.</p>
                                <input type=""submit"" value=""Submit"" />
                            </noscript>
                        </form>
                        <script language=""javascript"">
                            document.forms[0].submit();
                        </script>
                    </body>
                </html>";

            return form;
        }
    }
}
