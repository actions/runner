using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(SecretMasker))]
    public interface ISecretMasker : IAgentService
    {
        string MaskSecrets(string input);

        void AddRegex(string expression);

        void AddValue(string value);

        void AddVariable(string name, string value);
    }

    public sealed class SecretMasker : AgentService, ISecretMasker
    {
        private const string Mask = "********";
        private readonly List<ISecret> _secrets = new List<ISecret>();
        private readonly HashSet<string> _variableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> VariableNames => _variableNames;

        public override void Initialize(IHostContext hostContext)
        {
            //Do not call base.Initialize.
            //The HostContext.Trace needs SecretMasker to be constructed already.
            //We should not call HostContext.GetTrace or trace anywhere from SecretMasker
            //implementation, because of the circular dependency (this.Trace is null).
            //Also HostContext is null, but we don't need it anyway here.
        }

        public string MaskSecrets(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // get indexes and lengths of all substrings that will be replaced
            var positionsToReplace = new List<ReplacementPosition>();
            foreach (ISecret secret in _secrets)
            {
                positionsToReplace.AddRange(secret.GetPositions(input));
            }

            // short-circuit if nothing to replace
            if (positionsToReplace.Count == 0)
            {
                return input;
            }

            // merge positions into ranges of characters to replace
            List<ReplacementPosition> replacementPositions = new List<ReplacementPosition>();
            ReplacementPosition currentReplacement = null;
            foreach (var substring in positionsToReplace.OrderBy(t => t.Start))
            {
                if (currentReplacement == null)
                {
                    currentReplacement = new ReplacementPosition(substring.Start, substring.Length);
                    replacementPositions.Add(currentReplacement);
                }
                else
                {
                    if (substring.Start <= currentReplacement.End)
                    {
                        // overlap
                        currentReplacement.Length = Math.Max(currentReplacement.End, substring.Start + substring.Length) - currentReplacement.Start;
                    }
                    else
                    {
                        // no overlap
                        currentReplacement = new ReplacementPosition(substring.Start, substring.Length);
                        replacementPositions.Add(currentReplacement);
                    }
                }
            }

            // replace
            var stringBuilder = new StringBuilder();
            int startIndex = 0;
            foreach (var replacement in replacementPositions)
            {
                stringBuilder.Append(input.Substring(startIndex, replacement.Start - startIndex));
                stringBuilder.Append(Mask);
                startIndex = replacement.Start + replacement.Length;
            }

            if (startIndex < input.Length)
            {
                stringBuilder.Append(input.Substring(startIndex));
            }

            return stringBuilder.ToString();
        }

        public void AddRegex(string expression)
        {
            _secrets.Add(new RegexSecret(expression));
        }

        public void AddValue(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _secrets.Add(new ValueSecret(value));
            }
        }

        // TODO: REVISIT WRT CONCURRENCY. NEED TO SWAP OUT UNDERLYING LIST TO AVOID INTERFERENCE WITH ITERATORS.
        public void AddVariable(string name, string value)
        {
            _variableNames.Add(name);
            if (!string.IsNullOrEmpty(value))
            {
                _secrets.Add(new ValueSecret(value));
            }
        }
    }

    public class ReplacementPosition
    {
        public ReplacementPosition(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int Start { get; set; }
        public int Length { get; set; }
        public int End
        {
            get
            {
                return Start + Length;
            }
        }
    }

    public interface ISecret
    {
        /// <summary>
        /// Returns one item (start, length) for each match found in the input string.
        /// </summary>
        IEnumerable<ReplacementPosition> GetPositions(string input);
    }

    public sealed class RegexSecret : ISecret
    {
        private readonly Regex _regex;

        public RegexSecret(string expression)
        {
            _regex = new Regex(expression);
        }

        public IEnumerable<ReplacementPosition> GetPositions(string input)
        {
            int startIndex = 0;
            while (startIndex < input.Length)
            {
                var match = _regex.Match(input, startIndex);
                if (match.Success)
                {
                    startIndex = match.Index + 1;
                    yield return new ReplacementPosition(match.Index, match.Length);
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    public sealed class ValueSecret : ISecret
    {
        private readonly string _valueToMask;

        public ValueSecret(string value)
        {
            _valueToMask = value;
        }

        public IEnumerable<ReplacementPosition> GetPositions(string input)
        {
            if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(_valueToMask))
            {
                int startIndex = 0;
                while (startIndex > -1 && startIndex < input.Length)
                {
                    startIndex = input.IndexOf(_valueToMask, startIndex);
                    if (startIndex > -1)
                    {
                        yield return new ReplacementPosition(startIndex, _valueToMask.Length);
                        ++startIndex;
                    }
                }
            }
        }
    }
}
