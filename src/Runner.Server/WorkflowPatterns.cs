using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Runner.Server {
    class WorkflowPattern {
        public WorkflowPattern(string pattern, RegexOptions opts = RegexOptions.None) {
            if(pattern.StartsWith("!")) {
                Negative = true;
                Regex = new Regex(PatternToRegex(pattern.Substring(1)), opts);
            } else {
                Regex = new Regex(PatternToRegex(pattern), opts);
            }
        }

        public static string PatternToRegex(string pattern) {
            var rpattern = new StringBuilder();
            rpattern.Append("^");
            int pos = 0;
            var errors = new Dictionary<int, string>();
            while(pos < pattern.Length) {
                switch(pattern[pos]) {
                    case '*':
                    if(pos + 1 < pattern.Length && pattern[pos + 1] == '*') {
                        if(pos + 2 < pattern.Length && pattern[pos + 2] == '/') {
                            rpattern.Append("(.+/)?");
                            pos += 3;
                        } else {
                            rpattern.Append(".*");
                            pos += 2;
                        }
                    } else {
                        rpattern.Append("[^/]*");
                        pos++;
                    }
                    break;
                    case '?':
                    case '+':
                    if(pos > 0) {
                        rpattern.Append(pattern[pos]);
                    } else {
                        rpattern.Append(Regex.Escape(pattern[pos].ToString()));
                    }
                    pos++;
                    break;
                    case '[':
                    rpattern.Append(pattern[pos]);
                    pos++;
                    if(pos < pattern.Length && pattern[pos] == ']') {
                        errors.Add(pos++, "Unexpected empty brackets '[]'");
                        break;
                    }
                    Func<char, char, char, bool> validChar = (a, b, test) => {
                        return test >= a && test <= b;
                    };
                    var startPos = pos;
                    while(pos < pattern.Length && pattern[pos] != ']') {
                        switch(pattern[pos]) {
                            case '\\':
                            if(pos + 1 >= pattern.Length) {
                                errors.Add(pos++, "Missing symbol after \\");
                                break;
                            }
                            rpattern.Append(Regex.Escape(pattern[pos+1].ToString()));
                            pos += 2;
                            break;
                            case '-':
                            if(pos <= startPos || pos + 1 >= pattern.Length) {
                                errors.Add(pos++, "Invalid range");
                                break;
                            }
                            Func<char, char, bool> validRange = (a, b) => {
                                return validChar(a, b, pattern[pos - 1]) && validChar(a, b, pattern[pos + 1]) && pattern[pos - 1] <= pattern[pos + 1];
                            };
                            if(!validRange('A', 'z') && !validRange('0', '9')) {
                                errors.Add(pos++, "Ranges can only include a-z, A-Z, A-z, and 0-9");
                                break;
                            }
                            rpattern.Append(pattern.Substring(pos, 2));
                            pos += 2;
                            break;
                            default:
                            if(!validChar('A', 'z', pattern[pos]) && !validChar('0', '9', pattern[pos])) {
                                errors.Add(pos++, "Ranges can only include a-z, A-Z and 0-9");
                                break;
                            }
                            rpattern.Append(Regex.Escape(pattern[pos].ToString()));
                            pos++;
                            break;
                        }
                    }
                    if(pos >= pattern.Length || pattern[pos] != ']') {
                        errors.Add(pos++, "Missing closing bracket ']' after '['");
                    }
                    rpattern.Append(']');
                    pos++;
                    break;
                    case '\\':
                    if(pos + 1 >= pattern.Length) {
                    errors.Add(pos++, "Missing symbol after \\");
                    break;
                    }
                    rpattern.Append(Regex.Escape(pattern[pos + 1].ToString()));
                    pos += 2;
                    break;
                    default:
                    rpattern.Append(Regex.Escape(pattern[pos].ToString()));
                    pos++;
                    break;
                }
            }
            if(errors.Any()) {
                throw new Exception($"Invalid Pattern '{pattern}': {string.Join(", ", from error in errors select $"Position: {error.Key} Error: {error.Value}")}");
            }
            rpattern.Append("$");
            return rpattern.ToString();
        }
        public bool Negative { get; }
        public Regex Regex { get; }
    }
}
