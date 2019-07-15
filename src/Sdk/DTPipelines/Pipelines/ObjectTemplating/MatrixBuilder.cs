using System;
using System.Collections.Generic;
using System.Globalization;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    internal sealed class MatrixBuilder
    {
        internal MatrixBuilder(
            TemplateContext context,
            String jobFactoryDisplayName)
        {
            m_context = context;
            m_jobFactoryDisplayName = jobFactoryDisplayName;
        }

        internal void AddVector(
            String name,
            SequenceToken vector)
        {
            m_vectors.Add(name, vector.ToContextData());
        }

        internal DictionaryContextData Vectors => m_vectors;

        internal void Exclude(SequenceToken exclude)
        {
            m_excludeSequence = exclude;
        }

        internal void Include(SequenceToken include)
        {
            m_includeSequence = include;
        }

        internal IEnumerable<StrategyConfiguration> Build()
        {
            if (m_vectors.Count > 0)
            {
                // Parse includes/excludes
                var include = new MatrixInclude(m_context, m_vectors, m_includeSequence);
                var exclude = new MatrixExclude(m_context, m_vectors, m_excludeSequence);

                // Calculate the cross product size
                var productSize = 1;
                foreach (var vectorPair in m_vectors)
                {
                    checked
                    {
                        var vector = vectorPair.Value.AssertArray("vector");
                        productSize *= vector.Count;
                    }
                }

                var nameBuilder = new ReferenceNameBuilder();
                var displayNameBuilder = new JobDisplayNameBuilder(m_jobFactoryDisplayName);

                // Cross product
                for (var productIndex = 0; productIndex < productSize; productIndex++)
                {
                    // Matrix
                    var matrix = new DictionaryContextData();
                    var blockSize = productSize;
                    foreach (var vectorPair in m_vectors)
                    {
                        var vectorName = vectorPair.Key;
                        var vector = vectorPair.Value.AssertArray("vector");
                        blockSize = blockSize / vector.Count;
                        var vectorIndex = (productIndex / blockSize) % vector.Count;
                        matrix.Add(vectorName, vector[vectorIndex]);
                    }

                    // Exclude
                    if (exclude.Match(matrix))
                    {
                        continue;
                    }

                    // New configuration
                    var configuration = new StrategyConfiguration();
                    m_context.Memory.AddBytes(TemplateMemory.MinObjectSize);

                    // Gather segments for name and display name
                    foreach (var matrixData in matrix.Traverse(omitKeys: true))
                    {
                        if (!(matrixData is StringContextData matrixStringData) ||
                            String.IsNullOrEmpty(matrixStringData.Value))
                        {
                            continue;
                        }

                        // Name segment
                        nameBuilder.AppendSegment(matrixStringData.Value);

                        // Display name segment
                        displayNameBuilder.AppendSegment(matrixStringData.Value);
                    }

                    // Name
                    configuration.Name = nameBuilder.Build();
                    m_context.Memory.AddBytes(configuration.Name);

                    // Display name
                    configuration.DisplayName = displayNameBuilder.Build();
                    m_context.Memory.AddBytes(configuration.DisplayName);

                    // Include
                    if (include.Match(matrix, out var extra))
                    {
                        matrix.Add(extra);
                    }

                    // Matrix context
                    configuration.ContextData.Add(PipelineTemplateConstants.Matrix, matrix);
                    m_context.Memory.AddBytes(PipelineTemplateConstants.Matrix);
                    m_context.Memory.AddBytes(matrix, traverse: true);

                    // Add configuration
                    yield return configuration;
                }
            }
        }

        private sealed class MatrixInclude
        {
            public MatrixInclude(
                TemplateContext context,
                DictionaryContextData vectors,
                SequenceToken includeSequence)
            {
                // Convert to excludes sets
                if (includeSequence?.Count > 0)
                {
                    foreach (var includeItem in includeSequence)
                    {
                        var includeMapping = includeItem.AssertMapping("matrix includes item");

                        // Distinguish filters versus extra
                        var filter = new MappingToken(null, null, null);
                        var extra = new DictionaryContextData();
                        foreach (var includePair in includeMapping)
                        {
                            var includeKeyLiteral = includePair.Key.AssertString("matrix include item key");
                            if (vectors.ContainsKey(includeKeyLiteral.Value))
                            {
                                filter.Add(includeKeyLiteral, includePair.Value);
                            }
                            else
                            {
                                extra.Add(includeKeyLiteral.Value, includePair.Value.ToContextData());
                            }
                        }

                        // At least one filter
                        if (filter.Count == 0)
                        {
                            context.Error(includeMapping, $"Matrix include mapping does not contain any filters");
                            continue;
                        }

                        // At least one extra
                        if (extra.Count == 0)
                        {
                            context.Error(includeMapping, $"Matrix include mapping does not contain any extra values to include");
                            continue;
                        }

                        // Add filter
                        m_filters.Add(new MatrixIncludeFilter(filter, extra));
                    }
                }
            }

            public Boolean Match(
                DictionaryContextData matrix,
                out DictionaryContextData extra)
            {
                extra = default(DictionaryContextData);
                foreach (var filter in m_filters)
                {
                    if (filter.Match(matrix, out var items))
                    {
                        if (extra == null)
                        {
                            extra = new DictionaryContextData();
                        }

                        foreach (var pair in items)
                        {
                            extra[pair.Key] = pair.Value;
                        }
                    }
                }

                return extra != null;
            }

            private readonly List<MatrixIncludeFilter> m_filters = new List<MatrixIncludeFilter>();
        }

        private sealed class MatrixIncludeFilter : MatrixFilter
        {
            public MatrixIncludeFilter(
                MappingToken filter,
                DictionaryContextData extra)
                : base(filter)
            {
                m_extra = extra;
            }

            public Boolean Match(
                DictionaryContextData matrix,
                out DictionaryContextData extra)
            {
                if (base.Match(matrix))
                {
                    extra = m_extra;
                    return true;
                }

                extra = null;
                return false;
            }

            private readonly DictionaryContextData m_extra;
        }

        private sealed class MatrixExclude
        {
            public MatrixExclude(
                TemplateContext context,
                DictionaryContextData vectors,
                SequenceToken excludeSequence)
            {
                // Convert to excludes sets
                if (excludeSequence?.Count > 0)
                {
                    foreach (var excludeItem in excludeSequence)
                    {
                        var excludeMapping = excludeItem.AssertMapping("matrix excludes item");

                        // Check empty
                        if (excludeMapping.Count == 0)
                        {
                            context.Error(excludeMapping, $"Matrix exclude filter must not be empty");
                            continue;
                        }

                        // Validate first-level keys
                        foreach (var excludePair in excludeMapping)
                        {
                            var excludeKey = excludePair.Key.AssertString("matrix excludes item key");
                            if (!vectors.ContainsKey(excludeKey.Value))
                            {
                                context.Error(excludeKey, $"Matrix exclude key '{excludeKey.Value}' does not match any key within the matrix");
                                continue;
                            }
                        }

                        // Add filter
                        m_filters.Add(new MatrixExcludeFilter(excludeMapping));
                    }
                }
            }

            public Boolean Match(DictionaryContextData matrix)
            {
                foreach (var filter in m_filters)
                {
                    if (filter.Match(matrix))
                    {
                        return true;
                    }
                }

                return false;
            }

            private readonly List<MatrixExcludeFilter> m_filters = new List<MatrixExcludeFilter>();
        }

        private sealed class MatrixExcludeFilter : MatrixFilter
        {
            public MatrixExcludeFilter(MappingToken filter)
                : base(filter)
            {
            }

            public new Boolean Match(DictionaryContextData matrix)
            {
                return base.Match(matrix);
            }
        }

        private abstract class MatrixFilter
        {
            protected MatrixFilter(MappingToken matrixFilter)
            {
                var state = new MappingState(null, matrixFilter);
                while (state != null)
                {
                    if (state.MoveNext())
                    {
                        var value = state.Mapping[state.Index].Value;
                        if (value is LiteralToken literal)
                        {
                            AddExpression(state, literal);
                        }
                        else
                        {
                            var mapping = state.Mapping[state.Index].Value.AssertMapping("matrix filter");
                            state = new MappingState(state, mapping);
                        }
                    }
                    else
                    {
                        state = state.Parent;
                    }
                }
            }

            protected Boolean Match(DictionaryContextData matrix)
            {
                if (matrix.Count == 0)
                {
                    throw new InvalidOperationException("Matrix filter cannot be empty");
                }

                foreach (var expression in m_expressions)
                {
                    var result = expression.Evaluate(null, null, matrix, null);
                    if (result.IsFalsy)
                    {
                        return false;
                    }
                }

                return true;
            }

            private void AddExpression(
                MappingState state,
                LiteralToken literal)
            {
                var expressionLiteral = default(String);
                switch (literal.Type)
                {
                    case TokenType.Null:
                        expressionLiteral = ExpressionConstants.Null;
                        break;

                    case TokenType.Boolean:
                        var booleanToken = literal as BooleanToken;
                        expressionLiteral = booleanToken.Value ? ExpressionConstants.True : ExpressionConstants.False;
                        break;

                    case TokenType.Number:
                        var numberToken = literal as NumberToken;
                        expressionLiteral = String.Format(CultureInfo.InvariantCulture, ExpressionConstants.NumberFormat, numberToken.Value);
                        break;

                    case TokenType.String:
                        var stringToken = literal as StringToken;
                        expressionLiteral = $"'{ExpressionUtility.StringEscape(stringToken.Value)}'";
                        break;

                    default:
                        throw new NotSupportedException($"Unexpected literal type '{literal.Type}'");
                }

                var str = $"{state.Path} == {expressionLiteral}";
                var parser = new ExpressionParser();
                var expression = parser.CreateTree(str, null, s_matrixFilterNamedValues, null);
                m_expressions.Add(expression);
            }

            private static readonly INamedValueInfo[] s_matrixFilterNamedValues = new INamedValueInfo[]
            {
                new NamedValueInfo<MatrixNamedValue>(PipelineTemplateConstants.Matrix),
            };
            private readonly List<IExpressionNode> m_expressions = new List<IExpressionNode>();
        }

        private sealed class MappingState
        {
            public MappingState(
                MappingState parent,
                MappingToken mapping)
            {
                Parent = parent;
                Mapping = mapping;
                Index = -1;
            }

            public Boolean MoveNext()
            {
                if (++Index < Mapping.Count)
                {
                    var keyLiteral = Mapping[Index].Key.AssertString("matrix filter key");
                    var parentPath = Parent?.Path ?? PipelineTemplateConstants.Matrix;
                    Path = $"{parentPath}['{ExpressionUtility.StringEscape(keyLiteral.Value)}']";
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public MappingState Parent;
            public MappingToken Mapping;
            public Int32 Index;
            public String Path;
        }

        private sealed class MatrixNamedValue : NamedValue
        {
            protected override Object EvaluateCore(
                EvaluationContext context,
                out ResultMemory resultMemory)
            {
                resultMemory = null;
                return context.State;
            }
        }

        private readonly TemplateContext m_context;
        private readonly String m_jobFactoryDisplayName;
        private readonly DictionaryContextData m_vectors = new DictionaryContextData();
        private SequenceToken m_excludeSequence;
        private SequenceToken m_includeSequence;
    }
}
