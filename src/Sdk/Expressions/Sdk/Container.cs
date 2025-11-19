using System.Collections.Generic;

namespace GitHub.Actions.Expressions.Sdk
{
    public abstract class Container : ExpressionNode
    {
        public IReadOnlyList<ExpressionNode> Parameters => m_parameters.AsReadOnly();

        public void AddParameter(ExpressionNode node)
        {
            m_parameters.Add(node);
            node.Container = this;
        }

        private readonly List<ExpressionNode> m_parameters = new List<ExpressionNode>();
    }
}
