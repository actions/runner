using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace GitHub.Runner.Worker
{
    public delegate void OnMatcherChanged(object sender, MatcherChangedEventArgs e);

    public sealed class MatcherChangedEventArgs : EventArgs
    {
        public MatcherChangedEventArgs(IssueMatcherConfig config)
        {
            Config = config;
        }

        public IssueMatcherConfig Config { get; }
    }

    public sealed class IssueMatcher
    {
        private string _defaultSeverity;
        private string _owner;
        private IssuePattern[] _patterns;
        private IssueMatch[] _state;

        public IssueMatcher(IssueMatcherConfig config, TimeSpan timeout)
        {
            _owner = config.Owner;
            _defaultSeverity = config.Severity;
            _patterns = config.Patterns.Select(x => new IssuePattern(x , timeout)).ToArray();
            Reset();
        }

        public string Owner
        {
            get
            {
                if (_owner == null)
                {
                    _owner = string.Empty;
                }

                return _owner;
            }
        }

        public string DefaultSeverity
        {
            get
            {
                if (_defaultSeverity == null)
                {
                    _defaultSeverity = string.Empty;
                }

                return _defaultSeverity;
            }
        }

        public IssueMatch Match(string line)
        {
            // Single pattern
            if (_patterns.Length == 1)
            {
                var pattern = _patterns[0];
                var regexMatch = pattern.Regex.Match(line);

                if (regexMatch.Success)
                {
                    return new IssueMatch(null, pattern, regexMatch.Groups, DefaultSeverity);
                }

                return null;
            }
            // Multiple patterns
            else
            {
                // Each pattern (iterate in reverse)
                for (int i = _patterns.Length - 1; i >= 0; i--)
                {
                    var runningMatch = i > 0 ? _state[i - 1] : null;

                    // First pattern or a running match
                    if (i == 0 || runningMatch != null)
                    {
                        var pattern = _patterns[i];
                        var isLast = i == _patterns.Length - 1;
                        var regexMatch = pattern.Regex.Match(line);

                        // Matched
                        if (regexMatch.Success)
                        {
                            // Last pattern
                            if (isLast)
                            {
                                // Loop
                                if (pattern.Loop)
                                {
                                    // Clear most state, but preserve the running match
                                    Reset();
                                    _state[i - 1] = runningMatch;
                                }
                                // Not loop
                                else
                                {
                                    // Clear the state
                                    Reset();
                                }

                                // Return
                                return new IssueMatch(runningMatch, pattern, regexMatch.Groups, DefaultSeverity);
                            }
                            // Not the last pattern
                            else
                            {
                                // Store the match
                                _state[i] = new IssueMatch(runningMatch, pattern, regexMatch.Groups);
                            }
                        }
                        // Not matched
                        else
                        {
                            // Last pattern
                            if (isLast)
                            {
                                // Break the running match
                                _state[i - 1] = null;
                            }
                            // Not the last pattern
                            else
                            {
                                // Record not matched
                                _state[i] = null;
                            }
                        }
                    }
                }

                return null;
            }
        }

        public void Reset()
        {
            _state = new IssueMatch[_patterns.Length - 1];
        }
    }

    public sealed class IssuePattern
    {
        public IssuePattern(IssuePatternConfig config, TimeSpan timeout)
        {
            File = config.File;
            Line = config.Line;
            Column = config.Column;
            Severity = config.Severity;
            Code = config.Code;
            Message = config.Message;
            FromPath = config.FromPath;
            Loop = config.Loop;
            Regex = new Regex(config.Pattern ?? string.Empty, IssuePatternConfig.RegexOptions, timeout);
        }

        public int? File { get; }

        public int? Line { get; }

        public int? Column { get; }

        public int? Severity { get; }

        public int? Code { get; }

        public int? Message { get; }

        public int? FromPath { get; }

        public bool Loop { get; }

        public Regex Regex { get; }
    }

    public sealed class IssueMatch
    {
        public IssueMatch(IssueMatch runningMatch, IssuePattern pattern, GroupCollection groups, string defaultSeverity = null)
        {
            File = runningMatch?.File ?? GetValue(groups, pattern.File);
            Line = runningMatch?.Line ?? GetValue(groups, pattern.Line);
            Column = runningMatch?.Column ?? GetValue(groups, pattern.Column);
            Severity = runningMatch?.Severity ?? GetValue(groups, pattern.Severity);
            Code = runningMatch?.Code ?? GetValue(groups, pattern.Code);
            Message = runningMatch?.Message ?? GetValue(groups, pattern.Message);
            FromPath = runningMatch?.FromPath ?? GetValue(groups, pattern.FromPath);

            if (string.IsNullOrEmpty(Severity) && !string.IsNullOrEmpty(defaultSeverity))
            {
                Severity = defaultSeverity;
            }
        }

        public string File { get; }

        public string Line { get; }

        public string Column { get; }

        public string Severity { get; }

        public string Code { get; }

        public string Message { get; }

        public string FromPath { get; }

        private string GetValue(GroupCollection groups, int? index)
        {
            if (index.HasValue && index.Value < groups.Count)
            {
                var group = groups[index.Value];
                return group.Value;
            }

            return null;
        }
    }

    [DataContract]
    public sealed class IssueMatchersConfig
    {
        [DataMember(Name = "problemMatcher")]
        private List<IssueMatcherConfig> _matchers;

        public List<IssueMatcherConfig> Matchers
        {
            get
            {
                if (_matchers == null)
                {
                    _matchers = new List<IssueMatcherConfig>();
                }

                return _matchers;
            }

            set
            {
                _matchers = value;
            }
        }

        public void Validate()
        {
            var distinctOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (_matchers?.Count > 0)
            {
                foreach (var matcher in _matchers)
                {
                    matcher.Validate();

                    if (!distinctOwners.Add(matcher.Owner))
                    {
                        // Not localized since this is a programming contract
                        throw new ArgumentException($"Duplicate owner name '{matcher.Owner}'");
                    }
                }
            }
        }
    }

    [DataContract]
    public sealed class IssueMatcherConfig
    {
        [DataMember(Name = "owner")]
        private string _owner;

        [DataMember(Name = "severity")]
        private string _severity;

        [DataMember(Name = "pattern")]
        private IssuePatternConfig[] _patterns;

        public string Owner
        {
            get
            {
                if (_owner == null)
                {
                    _owner = string.Empty;
                }

                return _owner;
            }

            set
            {
                _owner = value;
            }
        }

        public string Severity
        {
            get
            {
                if (_severity == null)
                {
                    _severity = string.Empty;
                }

                return _severity;
            }

            set
            {
                _severity = value;
            }
        }

        public IssuePatternConfig[] Patterns
        {
            get
            {
                if (_patterns == null)
                {
                    _patterns = new IssuePatternConfig[0];
                }

                return _patterns;
            }

            set
            {
                _patterns = value;
            }
        }

        public void Validate()
        {
            // Validate owner
            if (string.IsNullOrEmpty(_owner))
            {
                throw new ArgumentException("Owner must not be empty");
            }

            // Validate severity
            switch ((_severity ?? string.Empty).ToUpperInvariant())
            {
                case "":
                case "ERROR":
                case "WARNING":
                    break;
                default:
                    throw new ArgumentException($"Matcher '{_owner}' contains unexpected default severity '{_severity}'");
            }

            // Validate at least one pattern
            if (_patterns == null || _patterns.Length == 0)
            {
                throw new ArgumentException($"Matcher '{_owner}' does not contain any patterns");
            }

            int? file = null;
            int? line = null;
            int? column = null;
            int? severity = null;
            int? code = null;
            int? message = null;
            int? fromPath = null;

            // Validate each pattern config
            for (var i = 0; i < _patterns.Length; i++)
            {
                var isFirst = i == 0;
                var isLast = i == _patterns.Length - 1;
                var pattern = _patterns[i];
                pattern.Validate(isFirst,
                    isLast,
                    ref file,
                    ref line,
                    ref column,
                    ref severity,
                    ref code,
                    ref message,
                    ref fromPath);
            }

            if (message == null)
            {
                throw new ArgumentException($"At least one pattern must set 'message'");
            }
        }
    }

    [DataContract]
    public sealed class IssuePatternConfig
    {
        private const string _filePropertyName = "file";
        private const string _linePropertyName = "line";
        private const string _columnPropertyName = "column";
        private const string _severityPropertyName = "severity";
        private const string _codePropertyName = "code";
        private const string _messagePropertyName = "message";
        private const string _fromPathPropertyName = "fromPath";
        private const string _loopPropertyName = "loop";
        private const string _regexpPropertyName = "regexp";
        internal static readonly RegexOptions RegexOptions = RegexOptions.CultureInvariant | RegexOptions.ECMAScript;

        [DataMember(Name = _filePropertyName)]
        public int? File { get; set; }

        [DataMember(Name = _linePropertyName)]
        public int? Line { get; set; }

        [DataMember(Name = _columnPropertyName)]
        public int? Column { get; set; }

        [DataMember(Name = _severityPropertyName)]
        public int? Severity { get; set; }

        [DataMember(Name = _codePropertyName)]
        public int? Code { get; set; }

        [DataMember(Name = _messagePropertyName)]
        public int? Message { get; set; }

        [DataMember(Name = _fromPathPropertyName)]
        public int? FromPath { get; set; }

        [DataMember(Name = _loopPropertyName)]
        public bool Loop { get; set; }

        [DataMember(Name = _regexpPropertyName)]
        public string Pattern { get; set; }

        public void Validate(
            bool isFirst,
            bool isLast,
            ref int? file,
            ref int? line,
            ref int? column,
            ref int? severity,
            ref int? code,
            ref int? message,
            ref int? fromPath)
        {
            // Only the last pattern in a multiline matcher may set 'loop'
            if (Loop && (isFirst || !isLast))
            {
                throw new ArgumentException($"Only the last pattern in a multiline matcher may set '{_loopPropertyName}'");
            }

            if (Loop && Message == null)
            {
                throw new ArgumentException($"The {_loopPropertyName} pattern must set '{_messagePropertyName}'");
            }   

            var regex = new Regex(Pattern ?? string.Empty, RegexOptions);
            var groupCount = regex.GetGroupNumbers().Length;

            Validate(_filePropertyName, groupCount, File, ref file);
            Validate(_linePropertyName, groupCount, Line, ref line);
            Validate(_columnPropertyName, groupCount, Column, ref column);
            Validate(_severityPropertyName, groupCount, Severity, ref severity);
            Validate(_codePropertyName, groupCount, Code, ref code);
            Validate(_messagePropertyName, groupCount, Message, ref message);
            Validate(_fromPathPropertyName, groupCount, FromPath, ref fromPath);
        }

        private void Validate(string propertyName, int groupCount, int? newValue, ref int? trackedValue)
        {
            if (newValue == null)
            {
                return;
            }

            // The property '___' is set twice
            if (trackedValue != null)
            {
                throw new ArgumentException($"The property '{propertyName}' is set twice");
            }

            // Out of range
            if (newValue.Value < 0 || newValue >= groupCount)
            {
                throw new ArgumentException($"The property '{propertyName}' is set to {newValue} which is out of range");
            }

            // Record the value
            if (newValue != null)
            {
                trackedValue = newValue;
            }
        }
    }
}
