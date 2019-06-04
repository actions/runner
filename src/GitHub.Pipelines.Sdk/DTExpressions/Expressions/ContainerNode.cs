using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace GitHub.DistributedTask.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ContainerNode : ExpressionNode
    {
        public IReadOnlyList<ExpressionNode> Parameters => m_parameters.AsReadOnly();

        public void AddParameter(ExpressionNode node)
        {
            m_parameters.Add(node);
            node.Container = this;
        }

        public void ReplaceParameter(Int32 index, ExpressionNode node)
        {
            m_parameters[index] = node;
            node.Container = this;
        }

        public override IEnumerable<T> GetParameters<T>()
        {
            List<T> matched = new List<T>();
            Queue<IExpressionNode> parameters = new Queue<IExpressionNode>(this.Parameters);

            while (parameters.Count > 0)
            {
                var parameter = parameters.Dequeue();
                if (typeof(T).GetTypeInfo().IsAssignableFrom(parameter.GetType().GetTypeInfo()))
                {
                    matched.Add((T)parameter);
                }

                foreach (var childParameter in parameter.GetParameters<T>())
                {
                    parameters.Enqueue(childParameter);
                }
            }

            return matched;
        }

        private readonly List<ExpressionNode> m_parameters = new List<ExpressionNode>();
    }
}
