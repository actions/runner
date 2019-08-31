using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    [EditorBrowsable(EditorBrowsableState.Never)]
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
