using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechansim for modeling a variable dictionary as a simple string dictionary for expression
    /// evaluation.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class VariablesDictionary : IDictionary<String, VariableValue>, IDictionary<String, String>
    {
        /// <summary>
        /// Initializes a new <c>VariablesDictionary</c> instance with an empty variable set.
        /// </summary>
        public VariablesDictionary()
        {
            m_variables = new Dictionary<String, VariableValue>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new <c>VariablesDictionary</c> instance using the specified dictionary for initialization.
        /// </summary>
        /// <param name="copyFrom">The source from which to copy</param>
        public VariablesDictionary(VariablesDictionary copyFrom)
            : this(copyFrom, false)
        {
        }

        /// <summary>
        /// Initializes a new <c>VariablesDictionary</c> instance using the specified dictionary for initialization.
        /// </summary>
        /// <param name="copyFrom">The source from which to copy</param>
        public VariablesDictionary(IDictionary<String, String> copyFrom)
            : this(copyFrom?.ToDictionary(x => x.Key, x => new VariableValue { Value = x.Value }, StringComparer.OrdinalIgnoreCase), false)
        {
        }

        /// <summary>
        /// Initializes a new <c>VariablesDictionary</c> instance using the specified dictionary for initialization.
        /// </summary>
        /// <param name="copyFrom">The source from which to copy</param>
        public VariablesDictionary(IDictionary<String, VariableValue> copyFrom)
            : this(copyFrom, false)
        {
        }

        private VariablesDictionary(
            IDictionary<String, VariableValue> copyFrom, 
            Boolean readOnly)
        {
            ArgumentUtility.CheckForNull(copyFrom, nameof(copyFrom));

            if (readOnly)
            {
                m_variables = new ReadOnlyDictionary<String, VariableValue>(copyFrom);
            }
            else
            {
                m_variables = new Dictionary<String, VariableValue>(copyFrom, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the set of secrets which were accessed.
        /// </summary>
        public HashSet<String> SecretsAccessed
        {
            get
            {
                return m_secretsAccessed;
            }
        }

        public VariableValue this[String key]
        {
            get
            {
                if (!m_variables.TryGetValue(key, out VariableValue variableValue))
                {
                    throw new KeyNotFoundException(key);
                }

                if (variableValue.IsSecret)
                {
                    m_secretsAccessed.Add(key);
                }

                return variableValue;
            }
            set
            {
                m_variables[key] = value;
            }
        }

        public ICollection<String> Keys
        {
            get
            {
                return m_variables.Keys;
            }
        }

        public ICollection<VariableValue> Values
        {
            get
            {
                return m_variables.Values;
            }
        }

        public Int32 Count
        {
            get
            {
                return m_variables.Count;
            }
        }

        public Boolean IsReadOnly
        {
            get
            {
                return m_variables.IsReadOnly;
            }
        }

        public void Add(
            String key, 
            VariableValue value)
        {
            m_variables.Add(key, value);
        }

        public void Add(KeyValuePair<String, VariableValue> item)
        {
            m_variables.Add(item);
        }

        public VariablesDictionary AsReadOnly()
        {
            if (m_variables.IsReadOnly)
            {
                return this;
            }

            return new VariablesDictionary(m_variables, true);
        }

        public void Clear()
        {
            m_variables.Clear();
        }

        public Boolean Contains(KeyValuePair<String, VariableValue> item)
        {
            return m_variables.Contains(item);
        }

        public Boolean ContainsKey(String key)
        {
            return m_variables.ContainsKey(key);
        }

        public void CopyTo(
            KeyValuePair<String, VariableValue>[] array, 
            Int32 arrayIndex)
        {
            m_variables.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<String, VariableValue>> GetEnumerator()
        {
            return m_variables.GetEnumerator();
        }

        public Boolean Remove(String key)
        {
            return m_variables.Remove(key);
        }

        public Boolean Remove(KeyValuePair<String, VariableValue> item)
        {
            return m_variables.Remove(item);
        }

        public Boolean TryGetValue(
            String key, 
            out VariableValue value)
        {
            if (m_variables.TryGetValue(key, out value))
            {
                if (value.IsSecret)
                {
                    m_secretsAccessed.Add(key);
                }

                return true;
            }

            return false;
        }

        ICollection<String> IDictionary<String, String>.Keys
        {
            get
            {
                return m_variables.Keys;
            }
        }

        ICollection<String> IDictionary<String, String>.Values
        {
            get
            {
                return m_variables.Select(x => x.Value?.Value).ToArray();
            }
        }

        Int32 ICollection<KeyValuePair<String, String>>.Count
        {
            get
            {
                return m_variables.Count;
            }
        }

        Boolean ICollection<KeyValuePair<String, String>>.IsReadOnly
        {
            get
            {
                return m_variables.IsReadOnly;
            }
        }

        String IDictionary<String, String>.this[String key]
        {
            get
            {
                if (!m_variables.TryGetValue(key, out VariableValue variableValue))
                {
                    throw new KeyNotFoundException(key);
                }

                if (variableValue.IsSecret)
                {
                    m_secretsAccessed.Add(key);
                }

                return variableValue.Value;
            }
            set
            {
                if (!m_variables.TryGetValue(key, out VariableValue existingValue))
                {
                    m_variables.Add(key, value);
                }
                else
                {
                    // Preserve whether or not this variable value is a secret 
                    existingValue.Value = value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_variables.GetEnumerator();
        }

        IEnumerator<KeyValuePair<String, String>> IEnumerable<KeyValuePair<String, String>>.GetEnumerator()
        {
            foreach (var variable in m_variables)
            {
                yield return new KeyValuePair<String, String>(variable.Key, variable.Value?.Value);
            }
        }

        Boolean IDictionary<String, String>.TryGetValue(
            String key, 
            out String value)
        {
            if (m_variables.TryGetValue(key, out VariableValue variableValue))
            {
                if (variableValue.IsSecret)
                {
                    m_secretsAccessed.Add(key);
                }

                value = variableValue.Value;
                return true;
            }

            value = null;
            return false;
        }

        Boolean IDictionary<String, String>.ContainsKey(String key)
        {
            return m_variables.ContainsKey(key);
        }

        void IDictionary<String, String>.Add(
            String key, 
            String value)
        {
            m_variables.Add(key, value);
        }

        Boolean IDictionary<String, String>.Remove(String key)
        {
            return m_variables.Remove(key);
        }

        void ICollection<KeyValuePair<String, String>>.Add(KeyValuePair<String, String> item)
        {
            m_variables.Add(new KeyValuePair<String, VariableValue>(item.Key, item.Value));
        }

        void ICollection<KeyValuePair<String, String>>.Clear()
        {
            m_variables.Clear();
        }

        Boolean ICollection<KeyValuePair<String, String>>.Contains(KeyValuePair<String, String> item)
        {
            return m_variables.Contains(new KeyValuePair<String, VariableValue>(item.Key, item.Value));
        }

        void ICollection<KeyValuePair<String, String>>.CopyTo(
            KeyValuePair<String, String>[] array, 
            Int32 arrayIndex)
        {
            foreach (var variable in m_variables)
            {
                array[arrayIndex++] = new KeyValuePair<String, String>(variable.Key, variable.Value?.Value);
            }
        }

        Boolean ICollection<KeyValuePair<String, String>>.Remove(KeyValuePair<String, String> item)
        {
            return m_variables.Remove(new KeyValuePair<String, VariableValue>(item.Key, item.Value));
        }

        private readonly HashSet<String> m_secretsAccessed = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        private IDictionary<String, VariableValue> m_variables = new Dictionary<String, VariableValue>(StringComparer.OrdinalIgnoreCase);
    }
}
