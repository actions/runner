using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface ISecretMask
    {
        /// <summary>
        /// Returns one item (start, length) for each match found in the input string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        IEnumerable<Tuple<int, int>> GetPositions(string input);
    }

    public class RegexMask : ISecretMask
    {
        private readonly Regex _regex;

        public RegexMask(string expression)
        {
            _regex = new Regex(expression);
        }

        public IEnumerable<Tuple<int, int>> GetPositions(string input)
        {
            int startat = 0;
            while (startat < input.Length)
            {
                var match = _regex.Match(input, startat);
                if (match.Success)
                {
                    startat = match.Index + 1;
                    yield return new Tuple<int, int>(match.Index, match.Length);
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    public class ValueMask : ISecretMask
    {
        private readonly string _valueToMask;

        public ValueMask(string value)
        {
            _valueToMask = value;
        }

        public IEnumerable<Tuple<int, int>> GetPositions(string input)
        {
            if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(_valueToMask))
            {
                int startIndex = 0;
                while (startIndex > -1 && startIndex < input.Length)
                {
                    startIndex = input.IndexOf(_valueToMask, startIndex);
                    if (startIndex > -1)
                    {
                        yield return new Tuple<int, int>(startIndex, _valueToMask.Length);
                        ++startIndex;
                    }
                }
            }
        }
    }

    public class VariableValueMask : ValueMask
    {
        public string VariableName { get; private set; }

        public VariableValueMask(string variableName, string value)
            : base(value)
        {
            VariableName = variableName;
        }
    }
}
