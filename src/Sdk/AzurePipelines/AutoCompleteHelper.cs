using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitHub.DistributedTask.Expressions2;
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
                var adoFunctions = (context.Flags & GitHub.DistributedTask.Expressions2.ExpressionFlags.ExtendedFunctions) != GitHub.DistributedTask.Expressions2.ExpressionFlags.None
                    || (context.Flags & GitHub.DistributedTask.Expressions2.ExpressionFlags.DTExpressionsV1) != GitHub.DistributedTask.Expressions2.ExpressionFlags.None;

                var desc = adoFunctions ? PipelinesDescriptions.LoadDescriptions() : ActionsDescriptions.LoadDescriptions();
                var root = desc["root"];
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
                            var name = k.Substring(0, k.IndexOf("("));
                            // TODO parse min parameters and display them in the result
                            yield return new CompletionItem {
                                Label = new CompletionItemLabel {
                                    Label = name,
                                    Detail = "()"
                                },
                                InsertText = new SnippedString {
                                    Value = $"{name}($1)"
                                },
                                Kind = 2,
                                Documentation = new MarkdownString {
                                    Value = desc["functions"].TryGetValue(name, out var d) ? d.Description : "**Additional func** Item"
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

                    foreach (var item in ExpressionConstants.AzureWellKnownFunctions)
                    {
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = item.Key,
                                Detail = $"({string.Join(", ", Enumerable.Repeat("", item.Value.MinParameters).Select((_, i) => $"p{i + 1}"))})"
                            },
                            InsertText = new SnippedString {
                                Value = $"{item.Key}({string.Join(", ", Enumerable.Repeat("", item.Value.MinParameters).Select((_, i) => $"${i + 1}"))})",
                            },
                            Kind = 2,
                            Documentation = new MarkdownString {
                                Value = desc["functions"].TryGetValue(item.Key, out var d) ? d.Description : "**Additional func** Item"
                            }
                        };
                    }
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
                                Label = "<=",
                            },
                            Kind = 24,
                            Documentation = new MarkdownString {
                                Value = "Less Equals Operator"
                            }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = ">=",
                            },
                            Kind = 24,
                            Documentation = new MarkdownString {
                                Value = "Greater Equals Operator"
                            }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = "<",
                            },
                            Kind = 24,
                            Documentation = new MarkdownString {
                                Value = "Less Operator"
                            }
                        };
                        yield return new CompletionItem {
                            Label = new CompletionItemLabel {
                                Label = ">",
                            },
                            Kind = 24,
                            Documentation = new MarkdownString {
                                Value = "Greater Operator"
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
                            foreach (var item in ExpressionConstants.WellKnownFunctions)
                            {
                                yield return new CompletionItem {
                                    Label = new CompletionItemLabel {
                                        Label = item.Key,
                                        Detail = $"({string.Join(", ", Enumerable.Repeat("", item.Value.MinParameters).Select((_, i) => $"p{i + 1}"))})"
                                    },
                                    InsertText = new SnippedString {
                                        Value = $"{item.Key}({string.Join(", ", Enumerable.Repeat("", item.Value.MinParameters).Select((_, i) => $"${i + 1}"))})",
                                    },
                                    Kind = 2,
                                    Documentation = new MarkdownString {
                                        Value = desc["functions"].TryGetValue(item.Key, out var d) ? d.Description : "**Additional func** Item"
                                    }
                                };
                            }
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