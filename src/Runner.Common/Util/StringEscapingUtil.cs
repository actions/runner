using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using GitHub.Runner.Common;

namespace GitHub.Runner.Common.Util
{
    public static class StringEscapingUtil
    {

        public static string UnescapeString(string escaped, EscapeMapping[] _escapeDataMappings)
        {
            if (string.IsNullOrEmpty(escaped))
            {
                return string.Empty;
            }

            string unescaped = escaped;
            foreach (EscapeMapping mapping in _escapeDataMappings)
            {
                unescaped = unescaped.Replace(mapping.Replacement, mapping.Token);
            }

            return unescaped;
        }
        public class EscapeMapping
        {
            public string Replacement { get; }
            public string Token { get; }

            public EscapeMapping(string token, string replacement)
            {
                ArgUtil.NotNullOrEmpty(token, nameof(token));
                ArgUtil.NotNullOrEmpty(replacement, nameof(replacement));
                Token = token;
                Replacement = replacement;
            }
        }
    }
}
