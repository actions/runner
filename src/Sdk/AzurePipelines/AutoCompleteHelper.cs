using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using GitHub.DistributedTask.ObjectTemplating;

using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

using Sdk.Actions;

namespace Runner.Server.Azure.Devops
{
    public class AutoCompletetionHelper {
    internal static IEnumerable<CompletionItem> AddSuggestion(Context context, int column, int row, TemplateSchema schema, AutoCompleteEntry bestMatch, Definition? def, DefinitionType[]? allowed, bool flowStyle)
    {
        // if(allowed != null && !allowed.Contains(def.DefinitionType)) {
        //     yield break;
        // }
        if(bestMatch.Tokens != null) {
            var validTokens = new [] {
                GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Separator,
                GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Function,
                GitHub.DistributedTask.Expressions2.Tokens.TokenKind.NamedValue,
                GitHub.DistributedTask.Expressions2.Tokens.TokenKind.StartGroup,
                GitHub.DistributedTask.Expressions2.Tokens.TokenKind.StartIndex,
                GitHub.DistributedTask.Expressions2.Tokens.TokenKind.StartParameters,
                GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Separator,
            };
            if(/*bestMatch.Tokens.Count == 0 || validTokens.Contains(bestMatch.Tokens.Last().Kind)*/ true) {

                var desc = ActionsDescriptions.LoadDescriptions();
                var i = bestMatch.Tokens.FindLastIndex(t => t.Index <= bestMatch.Index);
                var last = bestMatch;
                if(i >= 0 && last.Tokens[i].Kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Dereference) {
                    i++;
                }
                if(i >= 2 && (i >= last.Tokens.Count || last.Tokens[i].Index + last.Tokens[i].RawValue.Length >= bestMatch.Index) && last.Tokens[i - 2].Kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.NamedValue && last.Tokens[i - 1].Kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Dereference && new [] { "github", "runner", "strategy" }.Contains(last.Tokens[i - 2].RawValue.ToLower())) {
                    foreach(var k in desc[last.Tokens[i - 2].RawValue]) {
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = k.Key,
                            },
                            Kind = 5,
                            Documentation = new MarkdownString {
                                Value = k.Value.Description
                            },
                            Range = i < last.Tokens.Count && last.Tokens[i].Index <= bestMatch.Index ? new InsertReplaceRange {
                                Replacing = new Range {
                                    Start = new Position {
                                        Line = row - 1,
                                        Character = column - (bestMatch.Index - last.Tokens[i].Index) - 1
                                    },
                                    End = new Position {
                                        Line = row - 1,
                                        Character = column - (bestMatch.Index - last.Tokens[i].Index) + last.Tokens[i].RawValue.Length - 1
                                    }
                                },
                                Inserting = new Range {
                                    Start = new Position {
                                        Line = row - 1,
                                        Character = column - (bestMatch.Index - last.Tokens[i].Index) - 1
                                    },
                                    End = new Position {
                                        Line = row - 1,
                                        Character = column - 1
                                    }
                                }
                              } : null
                        };
                    }
                } 
                else
                {
                    foreach(var k in bestMatch.AllowedContext) {
                        if(k.Contains("(")) {
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = k.Substring(0, k.IndexOf("(")),
                                },
                                InsertText = new SnippedString {
                                    Value = $"{k.Substring(0, k.IndexOf("("))}($1)"
                                },
                                Kind = 2,
                                Documentation = new MarkdownString {
                                    Value = desc["functions"].TryGetValue(k, out var d) ? d.Description : "**Additional func** Item"
                                }
                            };
                        } else {
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = k,
                                },
                                Kind = 5,
                                Documentation = new MarkdownString {
                                    Value = desc["root"].TryGetValue(k, out var d) ? d.Description : "**Context** Item"
                                }
                            };
                        }
                    }
                }
                var adoFunctions = (context.Flags & GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedFunctions) != GitHub.DistributedTask.Expressions2.ExpressionFlags.None
                    || (context.Flags & GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1) != GitHub.DistributedTask.Expressions2.ExpressionFlags.None;
                if(bestMatch.AllowedContext.Length > 0 && adoFunctions) {
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "True",
                        },
                        Kind = 5,
                        Documentation = new MarkdownString {
                            Value = "Boolean Literal"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "False",
                        },
                        Kind = 5,
                        Documentation = new MarkdownString {
                            Value = "Boolean Literal"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "and",
                            Detail = "(lhs, rhs...)"
                        },
                        InsertText = new SnippedString {
                            Value = "and($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates to True if all parameters are True
Min parameters: 2. Max parameters: N
Casts parameters to Boolean for evaluation
Short-circuits after first False
Example: `and(eq(variables.letters, 'ABC'), eq(variables.numbers, 123))`
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "coalesce",
                            Detail = "(lhs, rhs...)"
                        },
                        InsertText = new SnippedString {
                            Value = "coalesce($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates the parameters in order (left to right), and returns the first value that doesn't equal null or empty-string.
No value is returned if the parameter values all are null or empty strings.
Min parameters: 2. Max parameters: N
Example: `coalesce(variables.couldBeNull, variables.couldAlsoBeNull, 'literal so it always works')`
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "contains",
                            Detail = "(lhs, rhs...)"
                        },
                        InsertText = new SnippedString {
                            Value = "contains($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if left parameter String contains right parameter
Min parameters: 2. Max parameters: 2
Casts parameters to String for evaluation
Performs ordinal ignore-case comparison
Example: `contains('ABCDE', 'BCD')` (returns True)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "containsValue",
                            Detail = "(lhs, rhs...)"
                        },
                        InsertText = new SnippedString {
                            Value = "containsValue($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if the left parameter is an array, and any item equals the right parameter. Also evaluates True if the left parameter is an object, and the value of any property equals the right parameter.
Min parameters: 2. Max parameters: 2
If the left parameter is an array, convert each item to match the type of the right parameter. If the left parameter is an object, convert the value of each property to match the type of the right parameter. The equality comparison for each specific item evaluates False if the conversion fails.
Ordinal ignore-case comparison for Strings
Short-circuits after the first match
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "convertToJson",
                            Detail = "(lhs)"
                        },
                        InsertText = new SnippedString {
                            Value = "convertToJson($1)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Take a complex object and outputs it as JSON.
Min parameters: 1. Max parameters: 1.
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "endsWith",
                            Detail = "(lhs, rhs)"
                        },
                        InsertText = new SnippedString {
                            Value = "endsWith($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if left parameter String ends with right parameter
Min parameters: 2. Max parameters: 2
Casts parameters to String for evaluation
Performs ordinal ignore-case comparison
Example: `endsWith('ABCDE', 'DE')` (returns True)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "eq",
                            Detail = "(lhs, rhs)"
                        },
                        Kind = 2,
                        InsertText = new SnippedString {
                            Value = "eq($1, $2)"
                        },
                        Documentation = new MarkdownString {
                            Value = "Compares two objects for equality. _This operation is case insensitive_."
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "format",
                            Detail = "(fmt...)"
                        },
                        Kind = 2,
                        InsertText = new SnippedString {
                            Value = "format($1, $2)"
                        },
                        Documentation = new MarkdownString {
                            Value = "Formats the string according to the fmt string placeholder `{0}` are get replaced by the additional parameters."
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "ge",
                            Detail = "(lhs, rhs)"
                        },
                        Kind = 2,
                        InsertText = new SnippedString {
                            Value = "ge($1, $2)"
                        },
                        Documentation = new MarkdownString {
                            Value = "Compares two objects for ge. _This operation is case insensitive_."
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "gt",
                            Detail = "(lhs, rhs)"
                        },
                        Kind = 2,
                        InsertText = new SnippedString {
                            Value = "gt($1, $2)"
                        },
                        Documentation = new MarkdownString {
                            Value = "Compares two objects for gt. _This operation is case insensitive_."
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "in",
                            Detail = "(lhs...)"
                        },
                        InsertText = new SnippedString {
                            Value = "in($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if left parameter is equal to any right parameter
Min parameters: 1. Max parameters: N
Converts right parameters to match type of left parameter. Equality comparison evaluates False if conversion fails.
Ordinal ignore-case comparison for Strings
Short-circuits after first match
Example: in('B', 'A', 'B', 'C') (returns True)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "join",
                            Detail = "(sep, array)"
                        },
                        InsertText = new SnippedString {
                            Value = "join($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Concatenates all elements in the right parameter array, separated by the left parameter string.
Min parameters: 2. Max parameters: 2
Each element in the array is converted to a string. Complex objects are converted to empty string.
If the right parameter isn't an array, the result is the right parameter converted to a string.
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "le",
                            Detail = "(lhs, rhs)"
                        },
                        Kind = 2,
                        InsertText = new SnippedString {
                            Value = "le($1, $2)"
                        },
                        Documentation = new MarkdownString {
                            Value = "Compares two objects for less or equal. _This operation is case insensitive_."
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "length",
                            Detail = "(str)"
                        },
                        InsertText = new SnippedString {
                            Value = "length($1)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Returns the length of a string or an array, either one that comes from the system or that comes from a parameter
Min parameters: 1. Max parameters 1
Example: length('fabrikam') returns 8
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "lower",
                            Detail = "(str)"
                        },
                        InsertText = new SnippedString {
                            Value = "lower($1)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Converts a string or variable value to all lowercase characters
Min parameters: 1. Max parameters 1
Returns the lowercase equivalent of a string
Example: lower('FOO') returns foo
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "le",
                            Detail = "(lhs, rhs)"
                        },
                        InsertText = new SnippedString {
                            Value = "le($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if left parameter is less than the right parameter
Min parameters: 2. Max parameters: 2
Converts right parameter to match type of left parameter. Errors if conversion fails.
Ordinal ignore-case comparison for Strings
Example: le(2, 5) (returns True)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "lt",
                            Detail = "(lhs, rhs)"
                        },
                        InsertText = new SnippedString {
                            Value = "lt($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if left parameter is less than the right parameter
Min parameters: 2. Max parameters: 2
Converts right parameter to match type of left parameter. Errors if conversion fails.
Ordinal ignore-case comparison for Strings
Example: lt(2, 5) (returns True)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "ne",
                            Detail = "(lhs, rhs)"
                        },
                        InsertText = new SnippedString {
                            Value = "ne($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if parameters are not equal
Min parameters: 2. Max parameters: 2
Converts right parameter to match type of left parameter. Returns True if conversion fails.
Ordinal ignore-case comparison for Strings
Example: ne(1, 2) (returns True)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "not",
                            Detail = "(lhs)"
                        },
                        InsertText = new SnippedString {
                            Value = "not($1)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if parameter is False
Min parameters: 1. Max parameters: 1
Converts value to Boolean for evaluation
Example: not(eq(1, 2)) (returns True)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "notIn",
                            Detail = "(lhs...)"
                        },
                        InsertText = new SnippedString {
                            Value = "notIn($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if left parameter isn't equal to any right parameter
Min parameters: 1. Max parameters: N
Converts right parameters to match type of left parameter. Equality comparison evaluates False if conversion fails.
Ordinal ignore-case comparison for Strings
Short-circuits after first match
Example: notIn('D', 'A', 'B', 'C') (returns True)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "or",
                            Detail = "(lhs, rhs...)"
                        },
                        InsertText = new SnippedString {
                            Value = "or($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if any parameter is True
Min parameters: 2. Max parameters: N
Casts parameters to Boolean for evaluation
Short-circuits after first True
Example: or(eq(1, 1), eq(2, 3)) (returns True, short-circuits)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "replace",
                            Detail = "(str, src, replacement)"
                        },
                        InsertText = new SnippedString {
                            Value = "replace($1, $2, $3)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Returns a new string in which all instances of a string in the current instance are replaced with another string
Min parameters: 3. Max parameters: 3
replace(a, b, c): returns a, with all instances of b replaced by c
Example: replace('https://www.tinfoilsecurity.com/saml/consume','https://www.tinfoilsecurity.com','http://server') (returns http://server/saml/consume)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "split",
                            Detail = "(lhs, rhs)"
                        },
                        InsertText = new SnippedString {
                            Value = "split($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Splits a string into substrings based on the specified delimiting characters
Min parameters: 2. Max parameters: 2
The first parameter is the string to split
The second parameter is the delimiting characters
Returns an array of substrings. The array includes empty strings when the delimiting characters appear consecutively or at the end of the string
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "startsWith",
                            Detail = "(lhs, rhs)"
                        },
                        InsertText = new SnippedString {
                            Value = "startsWith($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if left parameter string starts with right parameter
Min parameters: 2. Max parameters: 2
Casts parameters to String for evaluation
Performs ordinal ignore-case comparison
Example: startsWith('ABCDE', 'AB') (returns True)
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "upper",
                            Detail = "(str)"
                        },
                        InsertText = new SnippedString {
                            Value = "upper($1)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Converts a string or variable value to all uppercase characters
Min parameters: 1. Max parameters 1
Returns the uppercase equivalent of a string
Example: upper('bah') returns BAH
"
                        }
                    };
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "xor",
                            Detail = "(lhs, rhs)"
                        },
                        InsertText = new SnippedString {
                            Value = "xor($1, $2)"
                        },
                        Kind = 2,
                        Documentation = new MarkdownString {
                            Value = @"
Evaluates True if exactly one parameter is True
Min parameters: 2. Max parameters: 2
Casts parameters to Boolean for evaluation
Example: xor(True, False) (returns True)
"
                        }
                    };
                }

                if(bestMatch.AllowedContext.Length > 0 && !adoFunctions) {
                    var kind = bestMatch.Tokens?.LastOrDefault(t => t.Index < bestMatch.Index)?.Kind;
                    switch(kind) {
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.EndGroup:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.EndIndex:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.EndParameters:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Null:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Number:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.NamedValue:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.PropertyName:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.String:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Boolean:
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "==",
                            },
                            Kind = 24,
                            Documentation = new MarkdownString {
                                Value = "Equals Operator"
                            }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "!=",
                            },
                            Kind = 24,
                            Documentation = new MarkdownString {
                                Value = "Not Equals Operator"
                            }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "||",
                            },
                            Kind = 24,
                            Documentation = new MarkdownString {
                                Value = "logical or / coalescence from left to right"
                            }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "&&",
                            },
                            Kind = 24,
                            Documentation = new MarkdownString {
                                Value = "logical and, returns and evaluates right parameter if left is truthy"
                            }
                        };
                        if(kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.NamedValue || kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.PropertyName || kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.EndIndex || kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.EndGroup || kind == GitHub.DistributedTask.Expressions2.Tokens.TokenKind.EndParameters ) {
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = ".",
                                },
                                Kind = 24,
                                Documentation = new MarkdownString {
                                    Value = "dereference returns null if property doesn't exist"
                                }
                            };
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = "[...]",
                                },
                                InsertText = new SnippedString {
                                    Value = "[$1]"
                                },
                                Kind = 2,
                                Documentation = new MarkdownString {
                                    Value = @"index access"
                                }
                            };
                        }
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "join",
                                Detail = "(array [, sep])"
                            },
                            InsertText = new SnippedString {
                                Value = "join($1)"
                            },
                            Kind = 2,
                            Documentation = new MarkdownString {
                                Value = @"
`join( array, optionalSeparator )`

The value for `array` can be an array or a string. All values in `array` are concatenated into a string. If you provide `optionalSeparator`, it is inserted between the concatenated values. Otherwise, the default separator `,` is used. Casts values to a string.
"
                            }
                        };
                        break;
                    }
                    switch(kind) {
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.EndGroup:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.EndIndex:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.EndParameters:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Null:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Number:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.NamedValue:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.PropertyName:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.String:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Boolean:
                        case GitHub.DistributedTask.Expressions2.Tokens.TokenKind.Dereference:
                            break;
                        default:
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = "true",
                                },
                                Kind = 21,
                                Documentation = new MarkdownString {
                                    Value = "Boolean Literal"
                                }
                            };
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = "false",
                                },
                                Kind = 21,
                                Documentation = new MarkdownString {
                                    Value = "Boolean Literal"
                                }
                            };
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = "!",
                                },
                                Kind = 24,
                                Documentation = new MarkdownString {
                                    Value = "logical not"
                                }
                            };
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = "(...)",
                                },
                                InsertText = new SnippedString {
                                    Value = "($1)"
                                },
                                Kind = 2,
                                Documentation = new MarkdownString {
                                    Value = @"logical group"
                                }
                            };
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = "startsWith",
                                    Detail = "(lhs, rhs)"
                                },
                                InsertText = new SnippedString {
                                    Value = "startsWith($1, $2)"
                                },
                                Kind = 2,
                                Documentation = new MarkdownString {
                                    Value = @"
Evaluates True if left parameter string starts with right parameter
Min parameters: 2. Max parameters: 2
Casts parameters to String for evaluation
Performs ordinal ignore-case comparison
Example: startsWith('ABCDE', 'AB') (returns True)
        "
                                }
                            };
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = "endsWith",
                                    Detail = "(lhs, rhs)"
                                },
                                InsertText = new SnippedString {
                                    Value = "endsWith($1, $2)"
                                },
                                Kind = 2,
                                Documentation = new MarkdownString {
                                    Value = @"
Evaluates True if left parameter String ends with right parameter
Min parameters: 2. Max parameters: 2
Casts parameters to String for evaluation
Performs ordinal ignore-case comparison
Example: `endsWith('ABCDE', 'DE')` (returns True)
        "
                                }
                            };
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = "format",
                                    Detail = "(fmt...)"
                                },
                                Kind = 2,
                                InsertText = new SnippedString {
                                    Value = "format($1, $2)"
                                },
                                Documentation = new MarkdownString {
                                    Value = "Formats the string according to the fmt string placeholder `{0}` are get replaced by the additional parameters."
                                }
                            };
                        break;
                    }
                }
            }

            yield break;
        }
        if(def is MappingDefinition mapping && (bestMatch.Token is StringToken stkn && stkn.Value == "" || bestMatch.Token is NullToken || bestMatch.Token is MappingToken))
        {
            if((flowStyle || row == bestMatch.Token.Line && context.AutoCompleteMatches.LastOrDefault(m => m != bestMatch)?.Token is MappingToken) && !(bestMatch.Token is MappingToken)) {
                yield return new CompletionItem {
                    Label = new CompletionItemLabel {
                        Label = "{}",
                    },
                    InsertText = new SnippedString { Value = "{$1}" }
                };
                yield break;
            }
            var candidates = mapping.Properties.Where(p => (bestMatch.Token as MappingToken)?.FirstOrDefault(e => e.Key?.ToString() == p.Key).Key == null);
            var hasFirstProperties = candidates.Any(c => c.Value.FirstProperty);
            foreach(var (k, desc) in candidates.Where(c => !hasFirstProperties || c.Value.FirstProperty).Select(p => {
                var nested = schema.GetDefinition(p.Value.Type);
                return (p.Key, p.Value.Description ?? nested?.Description);
            })) {
                yield return new CompletionItem {
                    Label = new CompletionItemLabel {
                        Label = k,
                    },
                    InsertText = new SnippedString { Value = k + ":$0" },
                    Documentation = desc == null ? null : new MarkdownString {
                        Value = desc
                    }
                };
            }
            if(bestMatch.AllowedContext?.Length > 0) {
                var adoDirectives = (context.Flags & GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedDirectives) != GitHub.DistributedTask.Expressions2.ExpressionFlags.None;
                if(!flowStyle) {
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "${{ insert }}",
                        },
                        InsertText = new SnippedString { Value = "${{ insert }}:$0" }
                    };
                    if(adoDirectives) {
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "${{ if _ }}",
                            },
                            InsertText = new SnippedString { Value = "${{ if $1 }}:$0" }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "${{ elseif _ }}",
                            },
                            InsertText = new SnippedString { Value = "${{ elseif $1 }}:$0" }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "${{ else }}",
                            },
                            InsertText = new SnippedString { Value = "${{ else }}:$0" }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "${{ each _ in _ }}",
                            },
                            InsertText = new SnippedString { Value = "${{ each $1 in $2 }}:$0" }
                        };
                    }
                } else {
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "${{ insert }}",
                        },
                        InsertText = new SnippedString { Value = "\"${{ insert }}\":$0" }
                    };
                    if(adoDirectives) {
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "${{ if _ }}",
                            },
                            InsertText = new SnippedString { Value = "\"${{ if $1 }}\":$0" }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "${{ elseif _ }}",
                            },
                            InsertText = new SnippedString { Value = "\"${{ elseif $1 }}\":$0" }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "${{ else }}",
                            },
                            InsertText = new SnippedString { Value = "\"${{ else }}\":$0" }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "${{ each _ in _ }}",
                            },
                            InsertText = new SnippedString { Value = "\"${{ each $1 in $2 }}\":$0" }
                        };
                    }
                }
            }
        }
        if(def is SequenceDefinition sequence && (bestMatch.Token is StringToken stkn2 && stkn2.Value == "" || bestMatch.Token is SequenceToken))
        {
            if(flowStyle || row == bestMatch.Token.Line && !(bestMatch.Token is SequenceToken)) {
                if(bestMatch.Token is SequenceToken) {
                    var item = schema.GetDefinition(sequence.ItemType);
                    if(schema.Get<MappingDefinition>(item).Any()) {
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "{}",
                            },
                            InsertText = new SnippedString { Value = "{$1}" }
                        };
                    }
                    if(schema.Get<SequenceDefinition>(item).Any()) {
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "[]",
                            },
                            InsertText = new SnippedString { Value = "[$1]" }
                        };
                    }
                    yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = ",",
                        }
                    };
                } else {
                     yield return new CompletionItem {
                        Label = new CompletionItemLabel {
                            Label = "[]",
                        },
                        InsertText = new SnippedString { Value = "[$1]" }
                    };
                }
            } else {
                
                yield return new CompletionItem {
                    Label = new CompletionItemLabel {
                        Label = "- ",
                    },
                    InsertText = new SnippedString { Value = "- " }
                };
            }
        }
        if(def is StringDefinition str && bestMatch.Token is ScalarToken)
        {
            if(str.Constant != null) {
                yield return new CompletionItem {
                    Label = new CompletionItemLabel {
                        Label = str.Constant,
                    },
                    Kind = 11
                };
            }
            if(str.Pattern != null && Regex.IsMatch(str.Pattern, "^\\^[^\\\\\\+\\[\\]\\*\\{\\}\\.]+\\$$")) {
                yield return new CompletionItem {
                    Label = new CompletionItemLabel {
                        Label = str.Pattern.Substring(1, str.Pattern.Length - 2),
                    },
                    Kind = 11
                };
            }
        }
        if(def is OneOfDefinition oneOf) {
            foreach(var k in oneOf.OneOf) {
                var d = schema.GetDefinition(k);
                foreach(var u in AddSuggestion(context, column, row, schema, bestMatch, d, allowed, flowStyle)) {
                    yield return u;
                }
            }
        }
    }

    public static List<CompletionItem> CollectCompletions(int column, int row, Context context, TemplateSchema schema)
    {
        var src = context.AutoCompleteMatches.Any(a => a.Token.Column == column) ? context.AutoCompleteMatches.Where(a => a.Token.Column == column) : context.AutoCompleteMatches.Where(a => a.Token.Column == context.AutoCompleteMatches.Last().Token.Column);
        List<CompletionItem> list = src
            .SelectMany(bestMatch => bestMatch.Definitions.SelectMany(def => AddSuggestion(context, column, row, schema, bestMatch, def, bestMatch.Token.Line <= row && bestMatch.Token.Column <= column && !(bestMatch.Token is ScalarToken) ? null : bestMatch.Token.Line < row ? new[] { DefinitionType.OneOf, DefinitionType.Mapping, DefinitionType.Sequence } : new[] { DefinitionType.OneOf, DefinitionType.Null, DefinitionType.Boolean, DefinitionType.Number, DefinitionType.String }, context.AutoCompleteMatches.TakeWhile(m => m != bestMatch).Append(bestMatch).Any(m => (m.Token.Type == TokenType.Sequence || m.Token.Type == TokenType.Mapping) && m.Token.PreWhiteSpace == null)))).DistinctBy(k => k.Label.Label).ToList();
        return list;
    }
    }

}