using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GitHub.DistributedTask.Expressions
{
    internal sealed class IndexerNode : ContainerNode
    {
        internal IndexerNode()
        {
            Name = "indexer";
        }

        protected sealed override Boolean TraceFullyRealized => true;

        internal sealed override String ConvertToExpression()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}[{1}]",
                Parameters[0].ConvertToExpression(),
                Parameters[1].ConvertToExpression());
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
        {
            // Check if the result was stored
            if (context.TryGetTraceResult(this, out String result))
            {
                return result;
            }

            return ConvertToExpression();
        }

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            EvaluationResult firstParameter = Parameters[0].Evaluate(context);

            if (context.Options.UseCollectionInterfaces)
            {
                if (!firstParameter.TryGetCollectionInterface(out Object collection))
                {
                    // Even if we can't get the collection interface, return empty filtered array if it is a wildcard.
                    if (Parameters.Count > 2)
                    {
                        resultMemory = null;
                        return new FilteredArray();
                    }

                    resultMemory = null;
                    return null;
                }

                // Handle operating on a filtered array
                if (collection is FilteredArray filteredArray)
                {
                    return HandleFilteredArray(context, filteredArray, out resultMemory);
                }
                // Handle operating on an object
                else if (collection is IReadOnlyObject obj)
                {
                    return HandleObject(context, obj, out resultMemory);
                }
                // Handle operating on an array
                else if (collection is IReadOnlyArray array)
                {
                    return HandleArray(context, array, out resultMemory);
                }

                resultMemory = null;
                return null;
            }
            else
            {
                Object result = null;

                if (firstParameter.Kind == ValueKind.Array && firstParameter.Value is JArray)
                {
                    var jarray = firstParameter.Value as JArray;
                    EvaluationResult index = Parameters[1].Evaluate(context);
                    if (index.Kind == ValueKind.Number)
                    {
                        Decimal d = (Decimal)index.Value;
                        if (d >= 0m && d < (Decimal)jarray.Count && d == Math.Floor(d))
                        {
                            result = jarray[(Int32)d];
                        }
                    }
                    else if (index.Kind == ValueKind.String && !String.IsNullOrEmpty(index.Value as String))
                    {
                        Decimal d;
                        if (index.TryConvertToNumber(context, out d))
                        {
                            if (d >= 0m && d < (Decimal)jarray.Count && d == Math.Floor(d))
                            {
                                result = jarray[(Int32)d];
                            }
                        }
                    }
                }
                else if (firstParameter.Kind == ValueKind.Object)
                {
                    if (firstParameter.Value is JObject)
                    {
                        var jobject = firstParameter.Value as JObject;
                        EvaluationResult index = Parameters[1].Evaluate(context);
                        String s;
                        if (index.TryConvertToString(context, out s))
                        {
                            result = jobject[s];
                        }
                    }
                    else if (firstParameter.Value is IDictionary<String, String>)
                    {
                        var dictionary = firstParameter.Value as IDictionary<String, String>;
                        EvaluationResult index = Parameters[1].Evaluate(context);
                        if (index.TryConvertToString(context, out String key))
                        {
                            if (!dictionary.TryGetValue(key, out String resultString))
                            {
                                result = null;
                            }
                            else
                            {
                                result = resultString;
                            }
                        }
                    }
                    else if (firstParameter.Value is IDictionary<String, Object>)
                    {
                        var dictionary = firstParameter.Value as IDictionary<String, Object>;
                        EvaluationResult index = Parameters[1].Evaluate(context);
                        String s;
                        if (index.TryConvertToString(context, out s))
                        {
                            if (!dictionary.TryGetValue(s, out result))
                            {
                                result = null;
                            }
                        }
                    }
                    else if (firstParameter.Value is IReadOnlyDictionary<String, String>)
                    {
                        var dictionary = firstParameter.Value as IReadOnlyDictionary<String, String>;
                        EvaluationResult index = Parameters[1].Evaluate(context);
                        if (index.TryConvertToString(context, out String key))
                        {
                            if (!dictionary.TryGetValue(key, out String resultString))
                            {
                                result = null;
                            }
                            else
                            {
                                result = resultString;
                            }
                        }
                    }
                    else if (firstParameter.Value is IReadOnlyDictionary<String, Object>)
                    {
                        var dictionary = firstParameter.Value as IReadOnlyDictionary<String, Object>;
                        EvaluationResult index = Parameters[1].Evaluate(context);
                        String s;
                        if (index.TryConvertToString(context, out s))
                        {
                            if (!dictionary.TryGetValue(s, out result))
                            {
                                result = null;
                            }
                        }
                    }
                    else
                    {
                        var contract = s_serializer.Value.ContractResolver.ResolveContract(firstParameter.Value.GetType());
                        var objectContract = contract as JsonObjectContract;
                        if (objectContract != null)
                        {
                            EvaluationResult index = Parameters[1].Evaluate(context);
                            if (index.TryConvertToString(context, out String key))
                            {
                                var property = objectContract.Properties.GetClosestMatchProperty(key);
                                if (property != null)
                                {
                                    result = objectContract.Properties[property.PropertyName].ValueProvider.GetValue(firstParameter.Value);
                                }
                            }
                        }
                        else
                        {
                            var dictionaryContract = contract as JsonDictionaryContract;
                            if (dictionaryContract != null && dictionaryContract.DictionaryKeyType == typeof(String))
                            {
                                EvaluationResult index = Parameters[1].Evaluate(context);
                                if (index.TryConvertToString(context, out String key))
                                {
                                    var genericMethod = s_tryGetValueTemplate.Value.MakeGenericMethod(dictionaryContract.DictionaryValueType);
                                    resultMemory = null;
                                    return genericMethod.Invoke(null, new[] { firstParameter.Value, key });
                                }
                            }
                        }
                    }
                }

                resultMemory = null;
                return result;
            }
        }

        private Object HandleFilteredArray(
            EvaluationContext context,
            FilteredArray filteredArray,
            out ResultMemory resultMemory)
        {
            EvaluationResult indexResult = Parameters[1].Evaluate(context);
            var indexHelper = new IndexHelper(indexResult, context);

            Boolean isFilter;
            if (Parameters.Count > 2)
            {
                isFilter = true;

                if (!String.Equals(indexHelper.StringIndex, ExpressionConstants.Wildcard.ToString(), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Unexpected filter '{indexHelper.StringIndex}'");
                }
            }
            else
            {
                isFilter = false;
            }

            var result = new FilteredArray();
            var counter = new MemoryCounter(this, context.Options.MaxMemory);

            foreach (var item in filteredArray)
            {
                // Leverage the expression SDK to traverse the object
                var itemResult = EvaluationResult.CreateIntermediateResult(context, item, out _);
                if (itemResult.TryGetCollectionInterface(out Object nestedCollection))
                {
                    // Apply the index to a child object
                    if (nestedCollection is IReadOnlyObject nestedObject)
                    {
                        if (isFilter)
                        {
                            foreach (var val in nestedObject.Values)
                            {
                                result.Add(val);
                                counter.Add(IntPtr.Size);
                            }
                        }
                        else if (indexHelper.HasStringIndex)
                        {
                            if (nestedObject.TryGetValue(indexHelper.StringIndex, out Object nestedObjectValue))
                            {
                                result.Add(nestedObjectValue);
                                counter.Add(IntPtr.Size);
                            }
                        }
                    }
                    // Apply the index to a child array
                    else if (nestedCollection is IReadOnlyArray nestedArray)
                    {
                        if (isFilter)
                        {
                            foreach (var val in nestedArray)
                            {
                                result.Add(val);
                                counter.Add(IntPtr.Size);
                            }
                        }
                        else if (indexHelper.HasIntegerIndex &&
                            indexHelper.IntegerIndex < nestedArray.Count)
                        {
                            result.Add(nestedArray[indexHelper.IntegerIndex]);
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
            EvaluationResult indexResult = Parameters[1].Evaluate(context);
            var indexHelper = new IndexHelper(indexResult, context);

            if (indexHelper.HasStringIndex)
            {
                Boolean isFilter = Parameters.Count > 2;

                if (isFilter)
                {
                    var filteredArray = new FilteredArray();
                    var counter = new MemoryCounter(this, context.Options.MaxMemory);

                    foreach (var val in obj.Values)
                    {
                        filteredArray.Add(val);
                        counter.Add(IntPtr.Size);
                    }

                    resultMemory = new ResultMemory { Bytes = counter.CurrentBytes };
                    return filteredArray;
                }
                else if (obj.TryGetValue(indexHelper.StringIndex, out Object result))
                {
                    resultMemory = null;
                    return result;
                }
            }

            resultMemory = null;
            return null;
        }

        private Object HandleArray(
            EvaluationContext context,
            IReadOnlyArray array,
            out ResultMemory resultMemory)
        {
            // Similar to as above but for an array
            EvaluationResult indexResult = Parameters[1].Evaluate(context);
            var indexHelper = new IndexHelper(indexResult, context);

            // When we are operating on a array and it has three parameters, with the second being a string * and the third being a true boolean, it's a filtered array.
            if (Parameters.Count > 2)
            {
                var filtered = new FilteredArray();
                var counter = new MemoryCounter(this, context.Options.MaxMemory);

                foreach (var x in array)
                {
                    filtered.Add(x);
                    counter.Add(IntPtr.Size);
                }

                resultMemory = new ResultMemory { Bytes = counter.CurrentBytes };
                return filtered;
            }

            if (indexHelper.HasIntegerIndex && indexHelper.IntegerIndex < array.Count)
            {
                resultMemory = null;
                return array[indexHelper.IntegerIndex];
            }

            resultMemory = null;
            return null;
        }

        // todo: remove with feature flag cleanup for "UseCollectionInterfaces"
        private static Object TryGetValue<TValue>(
            IDictionary<String, TValue> dictionary,
            String key)
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                return null;
            }

            return value;
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

            public IEnumerator<Object> GetEnumerator() => m_list.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => m_list.GetEnumerator();

            private readonly IList<Object> m_list;
        }

        private class IndexHelper
        {
            public Boolean HasIntegerIndex => m_integerIndex.Value.Item1;
            public Int32 IntegerIndex => m_integerIndex.Value.Item2;

            public Boolean HasStringIndex => m_stringIndex.Value.Item1;
            public String StringIndex => m_stringIndex.Value.Item2;

            public IndexHelper(
                EvaluationResult result,
                EvaluationContext context)
            {
                m_result = result;
                m_context = context;

                m_integerIndex = new Lazy<Tuple<Boolean, Int32>>(() =>
                {
                    if (m_result.TryConvertToNumber(m_context, out Decimal decimalIndex) &&
                        decimalIndex >= 0m)
                    {
                        return new Tuple<Boolean, Int32>(true, (Int32)Math.Floor(decimalIndex));
                    }

                    return new Tuple<Boolean, Int32>(false, default(Int32));
                });

                m_stringIndex = new Lazy<Tuple<Boolean, String>>(() =>
                {
                    if (m_result.TryConvertToString(m_context, out String stringIndex))
                    {
                        return new Tuple<Boolean, String>(true, stringIndex);
                    }

                    return new Tuple<Boolean, String>(false, null);
                });
            }

            private Lazy<Tuple<Boolean, Int32>> m_integerIndex;
            private Lazy<Tuple<Boolean, String>> m_stringIndex;

            private readonly EvaluationResult m_result;
            private readonly EvaluationContext m_context;
        }

        // todo: remove these properties with feature flag cleanup for "UseCollectionInterfaces"
        private static Lazy<JsonSerializer> s_serializer = new Lazy<JsonSerializer>(() => JsonUtility.CreateJsonSerializer());
        private static Lazy<MethodInfo> s_tryGetValueTemplate = new Lazy<MethodInfo>(() => typeof(IndexerNode).GetTypeInfo().GetMethod(nameof(TryGetValue), BindingFlags.NonPublic | BindingFlags.Static));
    }
}
