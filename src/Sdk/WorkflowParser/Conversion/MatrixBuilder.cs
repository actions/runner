#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using GitHub.Actions.Expressions;
using GitHub.Actions.Expressions.Data;
using GitHub.Actions.Expressions.Sdk;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    /// <summary>
    /// Used to build a matrix cross product and apply include/exclude filters.
    /// </summary>
    internal sealed class MatrixBuilder
    {
        internal MatrixBuilder(
            TemplateContext context,
            String jobName)
        {
            m_context = context;
            m_jobName = jobName;
        }

        /// <summary>
        /// Adds an input vector. <c ref="Build" /> creates a cross product from all input vectors.
        ///
        /// For example, given the matrix:
        ///   arch: [x64, x86]
        ///   os: [linux, windows]
        ///
        /// This method should be called twice:
        ///   AddVector("arch", ...);
        ///   AddVector("os", ...)
        /// </summary>
        internal void AddVector(
            String name,
            SequenceToken vector)
        {
            m_vectors.Add(name, vector.ToExpressionData());
        }

        /// <summary>
        /// Adds the sequence containing all exclude mappings.
        /// </summary>
        internal void Exclude(SequenceToken exclude)
        {
            m_excludeSequence = exclude;
        }

        /// <summary>
        /// Adds the sequence containing all include mappings.
        /// </summary>
        internal void Include(SequenceToken include)
        {
            m_includeSequence = include;
        }

        /// <summary>
        /// Builds the matrix.
        ///
        /// In addition to computing the cross product of all input vectors, this method also:
        ///   1. Applies all exclude filters against each cross product vector
        ///   2. Applies all include filters against each cross product vector, which may
        ///      add additional values into existing vectors
        ///   3. Appends all unmatched include vectors, as additional result vectors
        ///
        /// Example 1, simple cross product:
        ///   arch: [x64, x86]
        ///   os: [linux, windows]
        /// The result would contain the following vectors:
        ///   [arch: x64, os: linux]
        ///   [arch: x64, os: windows]
        ///   [arch: x86, os: linux]
        ///   [arch: x86, os: windows]
        ///
        /// Example 2, using exclude filter:
        ///   arch: [x64, x86]
        ///   os: [linux, windows]
        ///   exclude:
        ///     - arch: x86
        ///       os: linux
        /// The result would contain the following vectors:
        ///   [arch: x64, os: linux]
        ///   [arch: x64, os: windows]
        ///   [arch: x86, os: windows]
        ///
        /// Example 3, using include filter to add additional values:
        ///   arch: [x64, x86]
        ///   os: [linux, windows]
        ///   include:
        ///     - arch: x64
        ///       os: linux
        ///       publish: true
        /// The result would contain the following vectors:
        ///   [arch: x64, os: linux, publish: true]
        ///   [arch: x64, os: windows]
        ///   [arch: x86, os: linux]
        ///   [arch: x86, os: windows]
        ///
        /// Example 4, include additional vectors:
        ///   arch: [x64, x86]
        ///   os: [linux, windows]
        ///   include:
        ///     - arch: x64
        ///     - os: macos
        /// The result would contain the following vectors:
        ///   [arch: x64, os: linux]
        ///   [arch: x64, os: windows]
        ///   [arch: x86, os: linux]
        ///   [arch: x86, os: windows]
        ///   [arch: x64, os: macos]
        /// </summary>
        /// <returns>One strategy configuration per result vector</returns>
        internal IEnumerable<StrategyConfiguration> Build()
        {
            // Parse includes/excludes
            var include = new MatrixInclude(m_context, m_vectors, m_includeSequence);
            var exclude = new MatrixExclude(m_context, m_vectors, m_excludeSequence);

            // Calculate the cross product size
            int productSize;
            if (m_vectors.Count > 0)
            {
                productSize = 1;
                foreach (var vectorPair in m_vectors)
                {
                    checked
                    {
                        var vector = vectorPair.Value.AssertArray("vector");
                        productSize *= vector.Count;
                    }
                }
            }
            else
            {
                productSize = 0;
            }

            var idBuilder = new IdBuilder();

            // Cross product vectors
            for (var productIndex = 0; productIndex < productSize; productIndex++)
            {
                // Matrix
                var matrix = new DictionaryExpressionData();
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

                // Include extra values in the vector
                include.Match(matrix, out var extra);

                // Create the configuration
                yield return CreateConfiguration(idBuilder, matrix, extra);
            }

            // Explicit vectors
            foreach (var matrix in include.GetUnmatchedVectors())
            {
                yield return CreateConfiguration(idBuilder, matrix, null);
            }
        }

        private StrategyConfiguration CreateConfiguration(
            IdBuilder idBuilder,
            DictionaryExpressionData matrix,
            DictionaryExpressionData extra)
        {
            // New configuration
            var configuration = new StrategyConfiguration();
            m_context.Memory.AddBytes(TemplateMemory.MinObjectSize);

            // Gather segments for ID and display name
            var nameBuilder = new JobNameBuilder(m_jobName);
            foreach (var matrixData in matrix.Traverse(omitKeys: true))
            {
                var segment = default(String);
                if (matrixData is BooleanExpressionData || matrixData is NumberExpressionData || matrixData is StringExpressionData)
                {
                    segment = matrixData.ToString();
                }

                if (!String.IsNullOrEmpty(segment))
                {
                    // ID segment
                    idBuilder.AppendSegment(segment);

                    // Display name segment
                    nameBuilder.AppendSegment(segment);
                }
            }

            // Id
            configuration.Id = idBuilder.Build(allowReservedPrefix: false, maxLength: m_context.GetFeatures().ShortMatrixIds ? 25 : WorkflowConstants.MaxNodeNameLength);
            m_context.Memory.AddBytes(configuration.Id);

            // Display name
            configuration.Name = nameBuilder.Build();
            m_context.Memory.AddBytes(configuration.Name);

            // Extra values
            if (extra?.Count > 0)
            {
                matrix.Add(extra);
            }

            // Matrix context
            configuration.ExpressionData.Add(WorkflowTemplateConstants.Matrix, matrix);
            m_context.Memory.AddBytes(WorkflowTemplateConstants.Matrix);
            m_context.Memory.AddBytes(matrix, traverse: true);

            return configuration;
        }

        /// <summary>
        /// Represents the sequence "strategy.matrix.include"
        /// </summary>
        private sealed class MatrixInclude
        {
            public MatrixInclude(
                TemplateContext context,
                DictionaryExpressionData vectors,
                SequenceToken includeSequence)
            {
                // Convert to includes sets
                if (includeSequence?.Count > 0)
                {
                    foreach (var includeItem in includeSequence)
                    {
                        var includeMapping = includeItem.AssertMapping("matrix includes item");

                        // Distinguish filters versus extra
                        var filter = new MappingToken(null, null, null);
                        var extra = new DictionaryExpressionData();
                        foreach (var includePair in includeMapping)
                        {
                            var includeKeyLiteral = includePair.Key.AssertString("matrix include item key");
                            if (vectors.ContainsKey(includeKeyLiteral.Value))
                            {
                                filter.Add(includeKeyLiteral, includePair.Value);
                            }
                            else
                            {
                                extra.Add(includeKeyLiteral.Value, includePair.Value.ToExpressionData());
                            }
                        }

                        // At least one filter or extra
                        if (filter.Count == 0 && extra.Count == 0)
                        {
                            context.Error(includeMapping, $"Matrix include mapping does not contain any values");
                            continue;
                        }

                        // Add filter
                        m_filters.Add(new MatrixIncludeFilter(filter, extra));
                    }
                }

                m_matches = new Boolean[m_filters.Count];
            }

            /// <summary>
            /// Matches a vector from the cross product against each include filter.
            ///
            /// For example, given the matrix:
            ///   arch: [x64, x86]
            ///   config: [release, debug]
            ///   include:
            ///     - arch: x64
            ///       config: release
            ///       publish: true
            ///
            /// This method would return the following:
            ///   Match(
            ///     matrix: {arch: x64, config: release},
            ///     out extra: {publish: true})
            ///   => true
            ///
            ///   Match(
            ///     matrix: {arch: x64, config: debug},
            ///     out extra: null)
            ///   => false
            ///
            ///   Match(
            ///     matrix: {arch: x86, config: release},
            ///     out extra: null)
            ///   => false
            ///
            ///   Match(
            ///     matrix: {arch: x86, config: debug},
            ///     out extra: null)
            ///   => false
            /// </summary>
            /// <param name="matrix">A vector of the cross product</param>
            /// <param name="extra">Extra values to add to the vector</param>
            /// <returns>True if the vector matched at least one include filter</returns>
            public Boolean Match(
                DictionaryExpressionData matrix,
                out DictionaryExpressionData extra)
            {
                extra = default(DictionaryExpressionData);
                for (var i = 0; i < m_filters.Count; i++)
                {
                    var filter = m_filters[i];
                    if (filter.Match(matrix, out var items))
                    {
                        m_matches[i] = true;

                        if (extra == null)
                        {
                            extra = new DictionaryExpressionData();
                        }

                        foreach (var pair in items)
                        {
                            extra[pair.Key] = pair.Value;
                        }
                    }
                }

                return extra != null;
            }

            /// <summary>
            /// Gets all additional vectors to add. These are additional configurations that were not produced
            /// from the cross product. These are include vectors that did not match any cross product results.
            ///
            /// For example, given the matrix:
            ///   arch: [x64, x86]
            ///   config: [release, debug]
            ///   include:
            ///     - arch: arm64
            ///       config: debug
            ///
            /// This method would return the following:
            ///   - {arch: arm64, config: debug}
            /// </summary>
            public IEnumerable<DictionaryExpressionData> GetUnmatchedVectors()
            {
                for (var i = 0; i < m_filters.Count; i++)
                {
                    if (m_matches[i])
                    {
                        continue;
                    }

                    var filter = m_filters[i];
                    var matrix = new DictionaryExpressionData();
                    foreach (var pair in filter.Filter)
                    {
                        var keyLiteral = pair.Key.AssertString("matrix include item key");
                        matrix.Add(keyLiteral.Value, pair.Value.ToExpressionData());
                    }

                    foreach (var includePair in filter.Extra)
                    {
                        matrix.Add(includePair.Key, includePair.Value);
                    }

                    yield return matrix;
                }
            }

            private readonly List<MatrixIncludeFilter> m_filters = new List<MatrixIncludeFilter>();

            // Tracks whether a filter has been matched
            private readonly Boolean[] m_matches;
        }

        /// <summary>
        /// Represents an item within the sequence "strategy.matrix.include"
        /// </summary>
        private sealed class MatrixIncludeFilter : MatrixFilter
        {
            public MatrixIncludeFilter(
                MappingToken filter,
                DictionaryExpressionData extra)
                : base(filter)
            {
                Filter = filter;
                Extra = extra;
            }

            public Boolean Match(
                DictionaryExpressionData matrix,
                out DictionaryExpressionData extra)
            {
                if (base.Match(matrix))
                {
                    extra = Extra;
                    return true;
                }

                extra = null;
                return false;
            }

            public DictionaryExpressionData Extra { get; }
            public MappingToken Filter { get; }
        }

        /// <summary>
        /// Represents the sequence "strategy.matrix.exclude"
        /// </summary>
        private sealed class MatrixExclude
        {
            public MatrixExclude(
                TemplateContext context,
                DictionaryExpressionData vectors,
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

            /// <summary>
            /// Matches a vector from the cross product against each exclude filter.
            ///
            /// For example, given the matrix:
            ///   arch: [x64, x86]
            ///   config: [release, debug]
            ///   exclude:
            ///     - arch: x86
            ///       config: release
            ///
            /// This method would return the following:
            ///   Match( {arch: x64, config: release} ) => false
            ///   Match( {arch: x64, config: debug}   ) => false
            ///   Match( {arch: x86, config: release} ) => true
            ///   Match( {arch: x86, config: debug}   ) => false
            /// </summary>
            /// <param name="matrix">A vector of the cross product</param>
            /// <param name="extra">Extra values to add to the vector</param>
            /// <returns>True if the vector matched at least one exclude filter</returns>
            public Boolean Match(DictionaryExpressionData matrix)
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

        /// <summary>
        /// Represents an item within the sequence "strategy.matrix.exclude"
        /// </summary>
        private sealed class MatrixExcludeFilter : MatrixFilter
        {
            public MatrixExcludeFilter(MappingToken filter)
                : base(filter)
            {
            }

            public new Boolean Match(DictionaryExpressionData matrix)
            {
                return base.Match(matrix);
            }
        }

        /// <summary>
        /// Base class for matrix include/exclude filters. That is, an item within the
        /// sequence "strategy.matrix.include" or within the sequence "strategy.matrix.exclude".
        /// </summary>
        private abstract class MatrixFilter
        {
            protected MatrixFilter(MappingToken matrixFilter)
            {
                // Traverse the structure and add an expression to compare each leaf node.
                // For example, given the filter:
                //   versions:
                //     node-version: 12
                //     npm-version: 6
                //   config: release
                // The following filter expressions would be created:
                //   - matrix.versions.node-version == 12
                //   - matrix.versions.npm-version == 6
                //   - matrix.config == 'release'
                var state = new MappingState(null, matrixFilter) as TokenState;
                while (state != null)
                {
                    if (state.MoveNext())
                    {
                        // Leaf
                        if (state.Current is LiteralToken literal)
                        {
                            AddExpression(state.Path, literal);
                        }
                        // Mapping
                        else if (state.Current is MappingToken mapping)
                        {
                            state = new MappingState(state, mapping);
                        }
                        // Sequence
                        else if (state.Current is SequenceToken sequence)
                        {
                            state = new SequenceState(state, sequence);
                        }
                        else
                        {
                            throw new NotSupportedException($"Unexpected token type '{state.Current.Type}' when constructing matrix filter expressions");
                        }
                    }
                    else
                    {
                        state = state.Parent;
                    }
                }
            }

            protected Boolean Match(DictionaryExpressionData matrix)
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
                String path,
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
                        expressionLiteral = ExpressionUtility.ConvertToParseToken(booleanToken.Value);
                        break;

                    case TokenType.Number:
                        var numberToken = literal as NumberToken;
                        expressionLiteral = ExpressionUtility.ConvertToParseToken(numberToken.Value);
                        break;

                    case TokenType.String:
                        var stringToken = literal as StringToken;
                        expressionLiteral = ExpressionUtility.ConvertToParseToken(stringToken.Value);
                        break;

                    default:
                        throw new NotSupportedException($"Unexpected literal type '{literal.Type}'");
                }

                var parser = new ExpressionParser();
                var expressionString = $"{path} == {expressionLiteral}";
                var expression = parser.CreateTree(expressionString, null, s_matrixFilterNamedValues, null);
                m_expressions.Add(expression);
            }

            /// <summary>
            /// Used to maintain state while traversing a mapping when building filter expressions.
            /// See <see cref="MatrixFilter"/> for more info.
            /// </summary>
            private sealed class MappingState : TokenState
            {
                public MappingState(
                    TokenState parent,
                    MappingToken mapping)
                    : base(parent)
                {
                    m_mapping = mapping;
                    m_index = -1;
                }

                public override Boolean MoveNext()
                {
                    if (++m_index < m_mapping.Count)
                    {
                        var pair = m_mapping[m_index];
                        var keyLiteral = pair.Key.AssertString("matrix filter key");
                        Current = pair.Value;
                        var parentPath = Parent?.Path ?? WorkflowTemplateConstants.Matrix;
                        Path = $"{parentPath}[{ExpressionUtility.ConvertToParseToken(keyLiteral.Value)}]";
                        return true;
                    }
                    else
                    {
                        Current = null;
                        Path = null;
                        return false;
                    }
                }

                private MappingToken m_mapping;
                private Int32 m_index;
            }

            /// <summary>
            /// Used to maintain state while traversing a sequence when building filter expressions.
            /// See <see cref="MatrixFilter"/> for more info.
            /// </summary>
            private sealed class SequenceState : TokenState
            {
                public SequenceState(
                    TokenState parent,
                    SequenceToken sequence)
                    : base(parent)
                {
                    m_sequence = sequence;
                    m_index = -1;
                }

                public override Boolean MoveNext()
                {
                    if (++m_index < m_sequence.Count)
                    {
                        Current = m_sequence[m_index];
                        var parentPath = Parent?.Path ?? WorkflowTemplateConstants.Matrix;
                        Path = $"{parentPath}[{ExpressionUtility.ConvertToParseToken((Double)m_index)}]";
                        return true;
                    }
                    else
                    {
                        Current = null;
                        Path = null;
                        return false;
                    }
                }

                private SequenceToken m_sequence;
                private Int32 m_index;
            }

            /// <summary>
            /// Used to maintain state while traversing a mapping/sequence when building filter expressions.
            /// See <see cref="MatrixFilter"/> for more info.
            /// </summary>
            private abstract class TokenState
            {
                protected TokenState(TokenState parent)
                {
                    Parent = parent;
                }

                public TemplateToken Current { get; protected set; }
                public TokenState Parent { get; }

                /// <summary>
                /// The expression used to reference the current position within the structure.
                /// For example: matrix.node-version
                /// </summary>
                public String Path { get; protected set; }

                public abstract Boolean MoveNext();
            }

            /// <summary>
            /// Represents the "matrix" context within an include/exclude expression
            /// </summary>
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

            private static readonly INamedValueInfo[] s_matrixFilterNamedValues = new INamedValueInfo[]
            {
                new NamedValueInfo<MatrixNamedValue>(WorkflowTemplateConstants.Matrix),
            };
            private readonly List<IExpressionNode> m_expressions = new List<IExpressionNode>();
        }

        private readonly TemplateContext m_context;
        private readonly String m_jobName;
        private readonly DictionaryExpressionData m_vectors = new DictionaryExpressionData();
        private SequenceToken m_excludeSequence;
        private SequenceToken m_includeSequence;
    }
}
