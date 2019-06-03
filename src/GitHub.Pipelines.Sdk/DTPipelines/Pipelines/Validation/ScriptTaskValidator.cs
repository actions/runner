using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.Pipelines.Validation
{
    /// <summary>
    /// Validates script tasks for bad tokens. For best performance, create one instance and reuse - this is
    /// thread safe.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ScriptTaskValidator
    {
        /// <param name="clientTokenPrv">
        /// If supplied, combined with <see cref="BaseBadTokenProvider"/> to form a single set of bad tokens.
        /// </param>
        public ScriptTaskValidator(IBadTokenProvider clientTokenPrv = null)
        {
            var regexToMatch = new HashSet<Regex>(RegexPatternComparer.Instance);
            var tokenToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var tokenPrvs = new List<IBadTokenProvider>(2) { BaseBadTokenProvider.Instance };
            if (clientTokenPrv != null)
            {
                tokenPrvs.Add(clientTokenPrv);
            }

            foreach (IBadTokenProvider tokenPrv in tokenPrvs)
            {
                foreach (string pattern in tokenPrv.GetRegexPatternsToMatch())
                {
                    regexToMatch.Add(
                        new Regex(pattern, RegexOptions.Compiled, matchTimeout: TimeSpan.FromMilliseconds(100)));
                }

                foreach (string staticToken in tokenPrv.GetStaticTokensToMatch())
                {
                    tokenToMatch.Add(staticToken);
                }
            }

            m_regexesToMatch = regexToMatch.ToArray();
            m_stringsToMatch = tokenToMatch.ToArray();
        }

        /// <summary>
        /// Check a string for tokens containing "banned" patterns. This method is thread safe.
        /// </summary>
        public bool HasBadParamOrArgument(
            string exeAndArgs,
            out string matchedPattern,
            out string matchedToken)
        {
            ArgumentUtility.CheckForNull(exeAndArgs, nameof(exeAndArgs));

            string[] args = exeAndArgs.Split();

            // Using for loops b/c they are measurably faster than foreach and this is n^2 
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                // Check static matches
                for (int j = 0; j < m_stringsToMatch.Length; j++)
                {
                    string toTest = m_stringsToMatch[j];
                    if (arg.IndexOf(toTest, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchedPattern = toTest;
                        matchedToken = arg;

                        return true;
                    }
                }

                // Check regexes
                for (int j = 0; j < m_regexesToMatch.Length; j++)
                {
                    Regex toTest = m_regexesToMatch[j];
                    if (toTest.IsMatch(arg))
                    {
                        matchedPattern = toTest.ToString();
                        matchedToken = arg;

                        return true;
                    }
                }
            }

            matchedPattern = null;
            matchedToken = null;
            return false;
        }

        // These are arrays for max perf when enumerating/indexing
        private readonly Regex[] m_regexesToMatch;
        private readonly string[] m_stringsToMatch;

        public interface IBadTokenProvider
        {
            IEnumerable<string> GetRegexPatternsToMatch();

            IEnumerable<string> GetStaticTokensToMatch();
        }

        /// <summary>
        /// Static set of bad tokens we know about.
        /// </summary>
        private sealed class BaseBadTokenProvider : IBadTokenProvider
        {
            private BaseBadTokenProvider()
            {
            }

            public IEnumerable<string> GetRegexPatternsToMatch()
            {
                // https://en.wikipedia.org/wiki/Base58
                const string base58charPattern = "[1-9a-km-zA-HJ-NP-Z]";

                // We expect arguments to begin with whitespace (i.e. --config-option bla) or an = (i.e.
                // --config-option=bla). If whitespace, we'll split the string so there is none to start.
                const string beginTokenDelimiter = "(^|=)";

                // We always expect arguments to end with whitespace, so any match will be the end of the string
                const string endTokenDelimiter = "$";

                string wrapInDelimeters(string argument)
                {
                    return beginTokenDelimiter + argument + endTokenDelimiter;
                }

                // Avoid patterns than can cause catastrophic backtracking for perf reasons
                // https://www.regular-expressions.info/catastrophic.html
                return new[]
                {
                    // Monero wallets. See https://moneroaddress.org/
                    // http://monero.wikia.com/wiki/Address_validation
                    wrapInDelimeters("4" + base58charPattern + "{94}"),
                    wrapInDelimeters("4" + base58charPattern  + "{105}"), 

                    // Bitcoin wallets. See https://en.bitcoin.it/wiki/Address
                    // Starts with 1 or 3, total 33-35 base58 chars
                    wrapInDelimeters("[1-3]" + base58charPattern + "{32,34}"),
                    // Starts with bc[1-16], then 39(?) to 87 (90-3) base32 chars
                    // See: https://en.bitcoin.it/wiki/Bech32
                    wrapInDelimeters("bc[0-9]{1,2}([0-9a-zA-Z]){39}"),
                    wrapInDelimeters("bc[0-9]{1,2}([0-9a-zA-Z]){59}"),
                };
            }

            public IEnumerable<string> GetStaticTokensToMatch()
            {
                return new[]
                {
                    // Begin known mining pools
                    "xmr.suprnova.cc",
                    "MoneroOcean.stream",
                    "supportXMR.com",
                    "xmr.nanopool.org",
                    "monero.hashvault.pro",
                    "MoriaXMR.com",
                    "xmrpool.",
                    "minergate.com",
                    "viaxmr.com",
                    "xmr.suprnova.cc",
                    // End known mining pools
        
                    // Probable mining argument
                    "--donate-level",
        
                    // Other probable mining processes
                    "cpuminer",
                    "cryptonight",
                    "sgminer",
                    "xmrig",
                    "nheqminer"
                };
            }

            public static readonly IBadTokenProvider Instance = new BaseBadTokenProvider();
        }

        private sealed class RegexPatternComparer : IEqualityComparer<Regex>
        {
            private RegexPatternComparer()
            {
            }

            public bool Equals(Regex x, Regex y) => x.ToString() == y.ToString();
            public int GetHashCode(Regex obj) => obj.GetHashCode();

            public static readonly IEqualityComparer<Regex> Instance = new RegexPatternComparer();
        }
    }
}
