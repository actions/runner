namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1 {
    public static class Extensions {
        public static bool EvaluateBoolean(this ExpressionNode node, EvaluationContext context) {
            return node.Evaluate(context).IsTruthy;
        }

        public static string EvaluateString(this ExpressionNode node, EvaluationContext context) {
            return node.Evaluate(context).ConvertToString();
        }

        public static bool Equals(this EvaluationResult node, EvaluationContext context, EvaluationResult other) {
            return node.AbstractEqual(other);
        }

        public static bool TryConvertToString(this EvaluationResult node, EvaluationContext context, out string str) {
            if(node.Kind == ValueKind.Object || node.Kind == ValueKind.Array) {
                str = null;
                return false;
            }
            str = node.ConvertToString();
            return true;
        }

        public static int CompareTo(this EvaluationResult node, EvaluationContext context, EvaluationResult other) {
            if(node.AbstractGreaterThan(other)) {
                return 1;
            } else if(node.AbstractLessThan(other)) {
                return -1;
            }
            return 0;
        }


        

        
        
    }
}