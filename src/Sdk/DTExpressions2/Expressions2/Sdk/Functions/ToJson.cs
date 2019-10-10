using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class ToJson : Function
    {
        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var result = new StringBuilder();
            var memory = new MemoryCounter(this, context.Options.MaxMemory);
            var current = Parameters[0].Evaluate(context);
            var ancestors = new Stack<ICollectionEnumerator>();

            do
            {
                // Descend as much as possible
                while (true)
                {
                    // Collection
                    if (current.TryGetCollectionInterface(out Object collection))
                    {
                        // Array
                        if (collection is IReadOnlyArray array)
                        {
                            if (array.Count > 0)
                            {
                                // Write array start
                                WriteArrayStart(result, memory, ancestors);

                                // Move to first item
                                var enumerator = new ArrayEnumerator(context, current, array);
                                enumerator.MoveNext();
                                ancestors.Push(enumerator);
                                current = enumerator.Current;
                            }
                            else
                            {
                                // Write empty array
                                WriteEmptyArray(result, memory, ancestors);
                                break;
                            }
                        }
                        // Mapping
                        else if (collection is IReadOnlyObject obj)
                        {
                            if (obj.Count > 0)
                            {
                                // Write mapping start
                                WriteMappingStart(result, memory, ancestors);

                                // Move to first pair
                                var enumerator = new ObjectEnumerator(context, current, obj);
                                enumerator.MoveNext();
                                ancestors.Push(enumerator);

                                // Write mapping key
                                WriteMappingKey(context, result, memory, enumerator.Current.Key, ancestors);

                                // Move to mapping value
                                current = enumerator.Current.Value;
                            }
                            else
                            {
                                // Write empty mapping
                                WriteEmptyMapping(result, memory, ancestors);
                                break;
                            }
                        }
                        else
                        {
                            throw new NotSupportedException($"Unexpected type '{collection?.GetType().FullName}'");
                        }
                    }
                    // Not a collection
                    else
                    {
                        // Write value
                        WriteValue(context, result, memory, current, ancestors);
                        break;
                    }
                }

                // Next sibling or ancestor sibling
                do
                {
                    if (ancestors.Count > 0)
                    {
                        var parent = ancestors.Peek();

                        // Parent array
                        if (parent is ArrayEnumerator arrayEnumerator)
                        {
                            // Move to next item
                            if (arrayEnumerator.MoveNext())
                            {
                                current = arrayEnumerator.Current;

                                break;
                            }
                            // Move to parent
                            else
                            {
                                ancestors.Pop();
                                current = arrayEnumerator.Array;

                                // Write array end
                                WriteArrayEnd(result, memory, ancestors);
                            }
                        }
                        // Parent mapping
                        else if (parent is ObjectEnumerator objectEnumerator)
                        {
                            // Move to next pair
                            if (objectEnumerator.MoveNext())
                            {
                                // Write mapping key
                                WriteMappingKey(context, result, memory, objectEnumerator.Current.Key, ancestors);

                                // Move to mapping value
                                current = objectEnumerator.Current.Value;

                                break;
                            }
                            // Move to parent
                            else
                            {
                                ancestors.Pop();
                                current = objectEnumerator.Object;

                                // Write mapping end
                                WriteMappingEnd(result, memory, ancestors);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException($"Unexpected type '{parent?.GetType().FullName}'");
                        }
                    }
                    else
                    {
                        current = null;
                    }

                } while (current != null);

            } while (current != null);

            return result.ToString();
        }

        private void WriteArrayStart(
            StringBuilder writer,
            MemoryCounter memory,
            Stack<ICollectionEnumerator> ancestors)
        {
            var str = PrefixValue("[", ancestors);
            memory.Add(str);
            writer.Append(str);
        }

        private void WriteMappingStart(
            StringBuilder writer,
            MemoryCounter memory,
            Stack<ICollectionEnumerator> ancestors)
        {
            var str = PrefixValue("{", ancestors);
            memory.Add(str);
            writer.Append(str);
        }

        private void WriteArrayEnd(
            StringBuilder writer,
            MemoryCounter memory,
            Stack<ICollectionEnumerator> ancestors)
        {
            var str = $"\n{new String(' ', ancestors.Count * 2)}]";
            memory.Add(str);
            writer.Append(str);
        }

        private void WriteMappingEnd(
            StringBuilder writer,
            MemoryCounter memory,
            Stack<ICollectionEnumerator> ancestors)
        {
            var str = $"\n{new String(' ', ancestors.Count * 2)}}}";
            memory.Add(str);
            writer.Append(str);
        }

        private void WriteEmptyArray(
            StringBuilder writer,
            MemoryCounter memory,
            Stack<ICollectionEnumerator> ancestors)
        {
            var str = PrefixValue("[]", ancestors);
            memory.Add(str);
            writer.Append(str);
        }

        private void WriteEmptyMapping(
            StringBuilder writer,
            MemoryCounter memory,
            Stack<ICollectionEnumerator> ancestors)
        {
            var str = PrefixValue("{}", ancestors);
            memory.Add(str);
            writer.Append(str);
        }

        private void WriteMappingKey(
            EvaluationContext context,
            StringBuilder writer,
            MemoryCounter memory,
            EvaluationResult key,
            Stack<ICollectionEnumerator> ancestors)
        {
            var str = PrefixValue(JsonUtility.ToString(key.ConvertToString()), ancestors, isMappingKey: true);
            memory.Add(str);
            writer.Append(str);
        }

        private void WriteValue(
            EvaluationContext context,
            StringBuilder writer,
            MemoryCounter memory,
            EvaluationResult value,
            Stack<ICollectionEnumerator> ancestors)
        {
            String str;
            switch (value.Kind)
            {
                case ValueKind.Null:
                    str = "null";
                    break;

                case ValueKind.Boolean:
                    str = (Boolean)value.Value ? "true" : "false";
                    break;

                case ValueKind.Number:
                    str = value.ConvertToString();
                    break;

                case ValueKind.String:
                    str = JsonUtility.ToString(value.Value);
                    break;

                default:
                    str = "{}"; // The value is an object we don't know how to traverse
                    break;
            }

            str = PrefixValue(str, ancestors);
            memory.Add(str);
            writer.Append(str);
        }

        private String PrefixValue(
            String value,
            Stack<ICollectionEnumerator> ancestors,
            Boolean isMappingKey = false)
        {
            var level = ancestors.Count;
            var parent = level > 0 ? ancestors.Peek() : null;

            if (!isMappingKey && parent is ObjectEnumerator)
            {
                return $": {value}";
            }
            else if (level > 0)
            {
                return $"{(parent.IsFirst ? String.Empty : ",")}\n{new String(' ', level * 2)}{value}";
            }
            else
            {
                return value;
            }
        }

        private interface ICollectionEnumerator : IEnumerator
        {
            Boolean IsFirst { get; }
        }

        private sealed class ArrayEnumerator : ICollectionEnumerator
        {
            public ArrayEnumerator(
                EvaluationContext context,
                EvaluationResult result,
                IReadOnlyArray array)
            {
                m_context = context;
                m_result = result;
                m_enumerator = array.GetEnumerator();
            }

            public EvaluationResult Array => m_result;

            public EvaluationResult Current => m_current;

            Object IEnumerator.Current => m_current;

            public Boolean IsFirst => m_index == 0;

            public Boolean MoveNext()
            {
                if (m_enumerator.MoveNext())
                {
                    m_current = EvaluationResult.CreateIntermediateResult(m_context, m_enumerator.Current);
                    m_index++;
                    return true;
                }
                else
                {
                    m_current = null;
                    return false;
                }
            }

            public void Reset()
            {
                throw new NotSupportedException(nameof(Reset));
            }

            private readonly EvaluationContext m_context;
            private readonly IEnumerator m_enumerator;
            private readonly EvaluationResult m_result;
            private EvaluationResult m_current;
            private Int32 m_index = -1;
        }

        private sealed class ObjectEnumerator : ICollectionEnumerator
        {
            public ObjectEnumerator(
                EvaluationContext context,
                EvaluationResult result,
                IReadOnlyObject obj)
            {
                m_context = context;
                m_result = result;
                m_enumerator = obj.GetEnumerator();
            }

            public KeyValuePair<EvaluationResult, EvaluationResult> Current => m_current;

            Object IEnumerator.Current => m_current;

            public Boolean IsFirst => m_index == 0;

            public EvaluationResult Object => m_result;

            public Boolean MoveNext()
            {
                if (m_enumerator.MoveNext())
                {
                    var current = (KeyValuePair<String, Object>)m_enumerator.Current;
                    var key = EvaluationResult.CreateIntermediateResult(m_context, current.Key);
                    var value = EvaluationResult.CreateIntermediateResult(m_context, current.Value);
                    m_current = new KeyValuePair<EvaluationResult, EvaluationResult>(key, value);
                    m_index++;
                    return true;
                }
                else
                {
                    m_current = default(KeyValuePair<EvaluationResult, EvaluationResult>);
                    return false;
                }
            }

            public void Reset()
            {
                throw new NotSupportedException(nameof(Reset));
            }

            private readonly EvaluationContext m_context;
            private readonly IEnumerator m_enumerator;
            private readonly EvaluationResult m_result;
            private KeyValuePair<EvaluationResult, EvaluationResult> m_current;
            private Int32 m_index = -1;
        }
    }
}
