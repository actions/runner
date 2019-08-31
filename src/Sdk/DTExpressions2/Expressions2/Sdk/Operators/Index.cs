using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace GitHub.DistributedTask.Expressions2.Sdk.Operators
{
    internal sealed class Index : Container
    {
        protected sealed override Boolean TraceFullyRealized => true;

        internal sealed override String ConvertToExpression()
        {
            // Verify if we can simplify the expression, we would rather return 
            // github.sha then github['sha'] so we check if this is a simple case.
            if (Parameters[1] is Literal literal &&
                literal.Value is String literalString &&
                ExpressionUtility.IsLegalKeyword(literalString))
            {
                return String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.{1}",
                    Parameters[0].ConvertToExpression(),
                    literalString);
            }
            else
            {
                return String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}[{1}]",
                    Parameters[0].ConvertToExpression(),
                    Parameters[1].ConvertToExpression());
            }
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
        {
            // Check if the result was stored
            if (context.TryGetTraceResult(this, out String result))
            {
                return result;
            }

            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}[{1}]",
                Parameters[0].ConvertToRealizedExpression(context),
                Parameters[1].ConvertToRealizedExpression(context));
        }

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            var left = Parameters[0].Evaluate(context);

            // Not a collection
            if (!left.TryGetCollectionInterface(out Object collection))
            {
                resultMemory = null;
                return Parameters[1] is Wildcard ? new FilteredArray() : null;
            }
            // Filtered array
            else if (collection is FilteredArray filteredArray)
            {
                return HandleFilteredArray(context, filteredArray, out resultMemory);
            }
            // Object
            else if (collection is IReadOnlyObject obj)
            {
                return HandleObject(context, obj, out resultMemory);
            }
            // Array
            else if (collection is IReadOnlyArray array)
            {
                return HandleArray(context, array, out resultMemory);
            }

            resultMemory = null;
            return null;
        }

        private Object HandleFilteredArray(
            EvaluationContext context,
            FilteredArray filteredArray,
            out ResultMemory resultMemory)
        {
            var result = new FilteredArray();
            var counter = new MemoryCounter(this, context.Options.MaxMemory);

            var index = new IndexHelper(context, Parameters[1]);

            foreach (var item in filteredArray)
            {
                // Leverage the expression SDK to traverse the object
                var itemResult = EvaluationResult.CreateIntermediateResult(context, item);
                if (itemResult.TryGetCollectionInterface(out var nestedCollection))
                {
                    // Apply the index to each child object
                    if (nestedCollection is IReadOnlyObject nestedObject)
                    {
                        // Wildcard
                        if (index.IsWildcard)
                        {
                            foreach (var val in nestedObject.Values)
                            {
                                result.Add(val);
                                counter.Add(IntPtr.Size);
                            }
                        }
                        // String
                        else if (index.HasStringIndex)
                        {
                            if (nestedObject.TryGetValue(index.StringIndex, out var nestedObjectValue))
                            {
                                result.Add(nestedObjectValue);
                                counter.Add(IntPtr.Size);
                            }
                        }
                    }
                    // Apply the index to each child array
                    else if (nestedCollection is IReadOnlyArray nestedArray)
                    {
                        // Wildcard
                        if (index.IsWildcard)
                        {
                            foreach (var val in nestedArray)
                            {
                                result.Add(val);
                                counter.Add(IntPtr.Size);
                            }
                        }
                        // String
                        else if (index.HasIntegerIndex &&
                            index.IntegerIndex < nestedArray.Count)
                        {
                            result.Add(nestedArray[index.IntegerIndex]);
                            counter.Add(IntPtr.Size);
                        }
                    }
                }
            }

            resultMemory = new ResultMemory { Bytes = counter.CurrentBytes };
            return result;
        }

        private Object HandleObject(
            EvaluationContext context,
            IReadOnlyObject obj,
            out ResultMemory resultMemory)
        {
            var index = new IndexHelper(context, Parameters[1]);

            // Wildcard
            if (index.IsWildcard)
            {
                var filteredArray = new FilteredArray();
                var counter = new MemoryCounter(this, context.Options.MaxMemory);
                counter.AddMinObjectSize();

                foreach (var val in obj.Values)
                {
                    filteredArray.Add(val);
                    counter.Add(IntPtr.Size);
                }

                resultMemory = new ResultMemory { Bytes = counter.CurrentBytes };
                return filteredArray;
            }
            // String
            else if (index.HasStringIndex &&
                obj.TryGetValue(index.StringIndex, out var result))
            {
                resultMemory = null;
                return result;
            }

            resultMemory = null;
            return null;
        }

        private Object HandleArray(
            EvaluationContext context,
            IReadOnlyArray array,
            out ResultMemory resultMemory)
        {
            var index = new IndexHelper(context, Parameters[1]);

            // Wildcard
            if (index.IsWildcard)
            {
                var filtered = new FilteredArray();
                var counter = new MemoryCounter(this, context.Options.MaxMemory);
                counter.AddMinObjectSize();

                foreach (var item in array)
                {
                    filtered.Add(item);
                    counter.Add(IntPtr.Size);
                }

                resultMemory = new ResultMemory { Bytes = counter.CurrentBytes };
                return filtered;
            }
            // Integer
            else if (index.HasIntegerIndex && index.IntegerIndex < array.Count)
            {
                resultMemory = null;
                return array[index.IntegerIndex];
            }

            resultMemory = null;
            return null;
        }
        
        private class FilteredArray : IReadOnlyArray
        {
            public FilteredArray()
            {
                m_list = new List<Object>();
            }

            public void Add(Object o)
            {
                m_list.Add(o);
            }

            public Int32 Count => m_list.Count;

            public Object this[Int32 index] => m_list[index];

            public IEnumerator GetEnumerator() => m_list.GetEnumerator();

            private readonly IList<Object> m_list;
        }

        private class IndexHelper
        {
            public IndexHelper(
                EvaluationContext context,
                ExpressionNode parameter)
            {
                m_parameter = parameter;
                m_result = parameter.Evaluate(context);

                m_integerIndex = new Lazy<Int32?>(() =>
                {
                    var doubleIndex = m_result.ConvertToNumber();
                    if (Double.IsNaN(doubleIndex) || doubleIndex < 0d)
                    {
                        return null;
                    }

                    doubleIndex = Math.Floor(doubleIndex);
                    if (doubleIndex > (Double)Int32.MaxValue)
                    {
                        return null;
                    }

                    return (Int32)doubleIndex;
                });

                m_stringIndex = new Lazy<String>(() =>
                {
                    return m_result.IsPrimitive ? m_result.ConvertToString() : null;
                });
            }

            public Boolean HasIntegerIndex => m_integerIndex.Value != null;

            public Boolean HasStringIndex => m_stringIndex.Value != null;

            public Boolean IsWildcard => m_parameter is Wildcard;

            public Int32 IntegerIndex => m_integerIndex.Value ?? default(Int32);

            public String StringIndex => m_stringIndex.Value;

            private readonly ExpressionNode m_parameter;
            private readonly EvaluationResult m_result;
            private readonly Lazy<Int32?> m_integerIndex;
            private readonly Lazy<String> m_stringIndex;
        }
    }
}
