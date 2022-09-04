using System;
using System.Collections.Generic;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace Runner.Server {
    public static class Extension {
        public static SequenceToken AssertScalarOrSequence(this TemplateToken token, string objectDescription) {
            switch(token.Type) {
                case TokenType.Boolean:
                case TokenType.Number:
                case TokenType.String:
                    var seq = new SequenceToken(null, null, null);
                    seq.Add(token);
                    return seq;
                default:
                    return token.AssertSequence(objectDescription);
            }
        }

        public static TemplateToken AssertPermissionsValues(this TemplateToken token, string objectDescription) {
            switch(token.Type) {
                case TokenType.String:
                string value = token.AssertString(objectDescription).Value;
                switch(value) {
                    case "read-all":
                    case "write-all":
                    return token;
                    default:
                    throw new Exception($"{objectDescription}: Unexpected value: '{value}' expected 'read-all', 'write-all' or a mapping");
                }
                default:
                var mapping = token.AssertMapping(objectDescription);
                foreach(var kv in mapping) {
                    var val = kv.Value.AssertString($"{objectDescription}.{kv.Key}").Value;
                    switch(val) {
                    case "none":
                    case "read":
                    case "write":
                    break;
                    default:
                    throw new Exception($"{objectDescription}: Unexpected value: '{val}' expected 'none', 'read' or 'write'");
                    }
                }
                return token;
            }
        }

        public static TemplateToken AssertJobSecrets(this TemplateToken token, string objectDescription) {
            switch(token.Type) {
                case TokenType.String:
                string value = token.AssertString(objectDescription).Value;
                switch(value) {
                    case "inherit":
                    return token;
                    default:
                    throw new Exception($"{objectDescription}: Unexpected value: '{value}' expected 'inherit' or a mapping");
                }
                default:
                return token.AssertMapping(objectDescription);
            }
        }

        public static Dictionary<string, TSource> ToOrdinalIgnoreCaseDictionary<TSource>(this IEnumerable<KeyValuePair<string, TSource>> source) {
            var ret = new Dictionary<string, TSource>(StringComparer.OrdinalIgnoreCase);
            foreach(var kv in source) {
                ret[kv.Key] = kv.Value;
            }
            return ret;
        }

        public static Dictionary<string, TValue> ToOrdinalIgnoreCaseDictionary<TSource, TValue>(this IEnumerable<TSource> source, Func<TSource, string> keySelector, Func<TSource, TValue> valueSelector) {
            var ret = new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
            foreach(var kv in source) {
                ret[keySelector(kv)] = valueSelector(kv);
            }
            return ret;
        }

        public static string PrefixIfNotNull(this string name, string seperator, string prefix) {
            return prefix != null ? prefix + seperator + name : name;
        }

        public static string PrefixJobNameIfNotNull(this string name, string parentName) {
            return name.PrefixIfNotNull(" / ", parentName);
        }

        public static string PrefixJobIdIfNotNull(this string name, string parentId) {
            return name.PrefixIfNotNull("/", parentId);
        }

        public static IDictionary<string, string> Merge(this IDictionary<string, string> dict, IDictionary<string, string> other) {
            var ret = dict == null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
            if(other != null) {
                foreach(var kv in other) {
                    ret[kv.Key] = kv.Value;
                }
            }
            return ret;
        }

        // public static DictionaryContextData EvalEnvBlocks(this IEnumerable<TemplateToken> blocks, DictionaryContextData contextData) {
        //     if(!contextData.ContainsKey("secrets")) {
        //         contextData["secrets"] = null;
        //     }
        //     var templateContext = CreateTemplateContext(workflowTraceWriter, workflowContext, contextData);
        //     var workflowEnv = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "workflow-env", actionPair.Value, 0, null, true);
        //     // Best effort, don't check for errors
        //     // templateContext.Errors.Check();
        //     // Best effort, make global env available this is not available on github actions
        //     if(workflowEnv is MappingToken genvToken) {
        //         globalEnv = new DictionaryContextData();
        //         foreach(var kv in genvToken) {
        //             if(kv.Key is StringToken key && kv.Value is StringToken val) {
        //                 globalEnv[key.Value] = new StringContextData(val.Value);
        //             }
        //         }
        //     }
        // }
    }
}