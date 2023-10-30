using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using GitHub.Runner.Common;

namespace GitHub.Runner.Common.Util
{
    public static class StrigEscapingUtil
    {

        private static readonly EscapeMapping[] _escapeDataMappings = new[]
        {
            new EscapeMapping(token: "\r", replacement: "%0D"),
            new EscapeMapping(token: "\n", replacement: "%0A"),
            new EscapeMapping(token: "%", replacement: "%25"),
        };
        public static string UnescapeData(string escaped)
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
        private sealed class EscapeMapping
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
