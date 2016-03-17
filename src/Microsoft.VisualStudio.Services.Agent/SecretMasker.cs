using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(SecretMasker))]
    public interface ISecretMasker : IAgentService
    {
        IEnumerable<ISecret> Masks { get; }

        string MaskSecrets(string input);

        void Add(ISecret mask);

        void AddRegEx(string expression);

        void AddValue(string value);

        void AddVariableName(string variableName, string value);
    }

    public class SecretMasker : AgentService, ISecretMasker
    {
        private const string Mask = "********";

        private readonly List<ISecret> _secretMasks = new List<ISecret>();

        public override void Initialize(IHostContext hostContext)
        {
            //Do not call base.Initialize.
            //The HostContext.Trace needs SecretMasker to be constructed already.
            //We should not call HostContext.GetTrace or trace anywhere from SecretMasker
            //implementation, because of the circular dependency (this.Trace is null).
            //Also HostContext is null, but we don't need it anyway here
        }

        public string MaskSecrets(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // get indexes and lengths of all substrings that will be replaced
            var positionsToReplace = new List<ReplacementPosition>();
            foreach (ISecret secretMask in _secretMasks)
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

        public void Add(ISecret mask)
        {
            _secretMasks.Add(mask);
        }

        public void AddRegEx(string expression)
        {
            Add(new RegexSecret(expression));
        }

        public void AddValue(string value)
        {
            Add(new ValueSecret(value));
        }

        public void AddVariableName(string variableName, string value)
        {
            Add(new VariableSecret(variableName, value));
        }

        public IEnumerable<ISecret> Masks
        {
            get
            {
                return _secretMasks.AsReadOnly();
            }
        }
    }
}
