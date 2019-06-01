﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.ContextData
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PipelineContextDataExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ArrayContextData AssertArray(
            this PipelineContextData value,
            String objectDescription)
        {
            if (value is ArrayContextData array)
            {
                return array;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(ArrayContextData)}' was expected.");
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static DictionaryContextData AssertDictionary(
            this PipelineContextData value,
            String objectDescription)
        {
            if (value is DictionaryContextData dictionary)
            {
                return dictionary;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(DictionaryContextData)}' was expected.");
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static StringContextData AssertString(
            this PipelineContextData value,
            String objectDescription)
        {
            if (value is StringContextData str)
            {
                return str;
            }

            throw new ArgumentException($"Unexpected type '{value?.GetType().Name}' encountered while reading '{objectDescription}'. The type '{nameof(StringContextData)}' was expected.");
        }

        /// <summary>
        /// Returns all context data objects (depth first)
        /// </summary>
        internal static IEnumerable<PipelineContextData> Traverse(this PipelineContextData value)
        {
            return Traverse(value, omitKeys: false);
        }

        /// <summary>
        /// Returns all context data objects (depth first)
        /// </summary>
        internal static IEnumerable<PipelineContextData> Traverse(
            this PipelineContextData value,
            Boolean omitKeys)
        {
            yield return value;

            if (value is ArrayContextData || value is DictionaryContextData)
            {
                var state = new TraversalState(null, value);
                while (state != null)
                {
                    if (state.MoveNext(omitKeys))
                    {
                        value = state.Current;
                        yield return value;

                        if (value is ArrayContextData || value is DictionaryContextData)
                        {
                            state = new TraversalState(state, value);
                        }
                    }
                    else
                    {
                        state = state.Parent;
                    }
                }
            }
        }

        internal static JToken ToJToken(this PipelineContextData value)
        {
            JToken result;

            if (value is StringContextData str)
            {
                result = str.Value ?? String.Empty;
            }
            else if (value is ArrayContextData array)
            {
                var jarray = new JArray();

                foreach (var item in array)
                {
                    jarray.Add(item.ToJToken()); // Recurse
                }

                result = jarray;
            }
            else if (value is DictionaryContextData dictionary)
            {
                var jobject = new JObject();

                foreach (var pair in dictionary)
                {
                    var key = pair.Key ?? String.Empty;
                    var value2 = pair.Value.ToJToken(); // Recurse

                    if (value2 != null)
                    {
                        jobject[key] = value2;
                    }
                }

                result = jobject;
            }
            else
            {
                throw new InvalidOperationException("Internal error reading the template. Expected a string, an array, or a dictionary");
            }

            return result;
        }

        internal static TemplateToken ToTemplateToken(this PipelineContextData data)
        {
            switch (data.Type)
            {
                case PipelineContextDataType.Dictionary:
                    var dictionary = data.AssertDictionary("dictionary");
                    var mapping = new MappingToken(null, null, null);
                    if (dictionary.Count > 0)
                    {
                        foreach (var pair in dictionary)
                        {
                            var key = new LiteralToken(null, null, null, pair.Key);
                            var value = pair.Value.ToTemplateToken();
                            mapping.Add(key, value);
                        }
                    }
                    return mapping;

                case PipelineContextDataType.Array:
                    var array = data.AssertArray("array");
                    var sequence = new SequenceToken(null, null, null);
                    if (array.Count > 0)
                    {
                        foreach (var item in array)
                        {
                            sequence.Add(item.ToTemplateToken());
                        }
                    }
                    return sequence;

                case PipelineContextDataType.String:
                    return new LiteralToken(null, null, null, data.ToString());

                default:
                    throw new NotSupportedException($"Unexpected {nameof(PipelineContextDataType)} type '{data.Type}'");
            }
        }

        private sealed class TraversalState
        {
            public TraversalState(
                TraversalState parent,
                PipelineContextData data)
            {
                Parent = parent;
                m_data = data;
            }

            public Boolean MoveNext(Boolean omitKeys)
            {
                switch (m_data.Type)
                {
                    case PipelineContextDataType.Array:
                        var array = m_data.AssertArray("array");
                        if (++m_index < array.Count)
                        {
                            Current = array[m_index];
                            return true;
                        }
                        else
                        {
                            Current = null;
                            return false;
                        }

                    case PipelineContextDataType.Dictionary:
                        var dictionary = m_data.AssertDictionary("dictionary");

                        // Return the value
                        if (m_isKey)
                        {
                            m_isKey = false;
                            Current = dictionary[m_index].Value;
                            return true;
                        }

                        if (++m_index < dictionary.Count)
                        {
                            // Skip the key, return the value
                            if (omitKeys)
                            {
                                m_isKey = false;
                                Current = dictionary[m_index].Value;
                                return true;
                            }

                            // Return the key
                            m_isKey = true;
                            Current = new StringContextData(dictionary[m_index].Key);
                            return true;
                        }

                        Current = null;
                        return false;

                    default:
                        throw new NotSupportedException($"Unexpected {nameof(PipelineContextData)} type '{m_data.Type}'");
                }
            }

            private PipelineContextData m_data;
            private Int32 m_index = -1;
            private Boolean m_isKey;
            public PipelineContextData Current;
            public TraversalState Parent;
        }
    }
}