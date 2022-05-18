using System;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

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
    }
}