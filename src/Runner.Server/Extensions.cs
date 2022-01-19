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
    }
}