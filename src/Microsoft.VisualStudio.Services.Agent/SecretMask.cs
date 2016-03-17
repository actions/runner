using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.Services.Agent
{
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
        /// <param name="input"></param>
        /// <returns></returns>
        IEnumerable<ReplacementPosition> GetPositions(string input);
    }

    public class RegexSecret : ISecret
    {
        private readonly Regex _regex;

        public RegexSecret(string expression)
        {
            _regex = new Regex(expression);
        }

        public IEnumerable<ReplacementPosition> GetPositions(string input)
        {
            int startat = 0;
            while (startat < input.Length)
            {
                var match = _regex.Match(input, startat);
                if (match.Success)
                {
                    startat = match.Index + 1;
                    yield return new ReplacementPosition(match.Index, match.Length);
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    public class ValueSecret : ISecret
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

    public class VariableSecret : ValueSecret
    {
        public string VariableName { get; private set; }

        public VariableSecret(string variableName, string value)
            : base(value)
        {
            VariableName = variableName;
        }
    }
}
