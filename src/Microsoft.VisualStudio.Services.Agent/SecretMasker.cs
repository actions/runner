using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(SecretMasker))]
    public interface ISecretMasker : IAgentService
    {
        IEnumerable<ISecretMask> Masks { get; }

        string MaskSecrets(string input);

        void Add(ISecretMask mask);
    }

    public class SecretMasker : AgentService, ISecretMasker
    {
        private const string MaskedString = "********";

        private readonly List<ISecretMask> _secretMasks = new List<ISecretMask>();

        private class ReplacementPosition
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

        public override void Initialize(IHostContext hostContext)
        {
            //Do not call base.Initialize.
            //The HostContext.Trace needs SecretMasker to be constructed already.
            //We should not call HostContext.GetTrace or trace anywhere from SecretMasker
            //implementation, because of the circular dependency (this.Trace is null).
            HostContext = hostContext;            
        }

        public string MaskSecrets(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // get indexes and lengths of all substrings that will be replaced
            var positionsToReplace = new List<Tuple<int, int>>();
            foreach (ISecretMask secretMask in _secretMasks)
            {
                positionsToReplace.AddRange(secretMask.GetPositions(input));
            }

            if (positionsToReplace.Count == 0)
            {
                return input;
            }

            // merge positions into ranges of characters to replace
            List<ReplacementPosition> replacementPositions = new List<ReplacementPosition>();
            ReplacementPosition currentReplacement = null;
            foreach (var substring in positionsToReplace.OrderBy(t => t.Item1))
            {
                if (currentReplacement == null)
                {
                    currentReplacement = new ReplacementPosition(substring.Item1, substring.Item2);
                    replacementPositions.Add(currentReplacement);
                }
                else
                {
                    if (substring.Item1 <= currentReplacement.End)
                    {
                        // overlap
                        currentReplacement.Length = Math.Max(currentReplacement.End, substring.Item1 + substring.Item2) - currentReplacement.Start;
                    }
                    else
                    {
                        // no overlap
                        currentReplacement = new ReplacementPosition(substring.Item1, substring.Item2);
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
                stringBuilder.Append(MaskedString);
                startIndex = replacement.Start + replacement.Length;
            }

            if (startIndex < input.Length)
            {
                stringBuilder.Append(input.Substring(startIndex));
            }

            return stringBuilder.ToString();
        }

        public void Add(ISecretMask mask)
        {
            _secretMasks.Add(mask);
        }

        public IEnumerable<ISecretMask> Masks
        {
            get
            {
                return _secretMasks.AsReadOnly();
            }
        }
    }
}
