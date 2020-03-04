using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// Context object that is flowed through while loading and evaluating object templates
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class TemplateContext
    {
        internal CancellationToken CancellationToken { get; set; }

        internal TemplateValidationErrors Errors
        {
            get
            {
                if (m_errors == null)
                {
                    m_errors = new TemplateValidationErrors();
                }

                return m_errors;
            }

            set
            {
                m_errors = value;
            }
        }

        /// <summary>
        /// Available functions within expression contexts
        /// </summary>
        internal IList<IFunctionInfo> ExpressionFunctions
        {
            get
            {
                if (m_expressionFunctions == null)
                {
                    m_expressionFunctions = new List<IFunctionInfo>();
                }

                return m_expressionFunctions;
            }
        }

        /// <summary>
        /// Available values within expression contexts
        /// </summary>
        internal IDictionary<String, Object> ExpressionValues
        {
            get
            {
                if (m_expressionValues == null)
                {
                    m_expressionValues = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
                }

                return m_expressionValues;
            }
        }

        internal TemplateMemory Memory { get; set; }

        internal TemplateSchema Schema { get; set; }

        internal IDictionary<String, Object> State
        {
            get
            {
                if (m_state == null)
                {
                    m_state = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
                }

                return m_state;
            }
        }

        internal ITraceWriter TraceWriter { get; set; }

        private IDictionary<String, Int32> FileIds
        {
            get
            {
                if (m_fileIds == null)
                {
                    m_fileIds = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
                }

                return m_fileIds;
            }
            set
            {
                m_fileIds = value;
            }
        }

        private List<String> FileNames
        {
            get
            {
                if (m_fileNames == null)
                {
                    m_fileNames = new List<String>();
                }

                return m_fileNames;
            }
            set
            {
                m_fileNames = value;
            }
        }

        internal void Error(TemplateValidationError error)
        {
            Errors.Add(error);
            TraceWriter.Error(error.Message);
        }

        internal void Error(
            TemplateToken value,
            Exception ex)
        {
            Error(value?.FileId, value?.Line, value?.Column, ex);
        }

        internal void Error(
            Int32? fileId,
            Int32? line,
            Int32? column,
            Exception ex)
        {
            var prefix = GetErrorPrefix(fileId, line, column);
            Errors.Add(prefix, ex);
            TraceWriter.Error(prefix, ex);
        }

        internal void Error(
            TemplateToken value,
            String message)
        {
            Error(value?.FileId, value?.Line, value?.Column, message);
        }

        internal void Error(
            Int32? fileId,
            Int32? line,
            Int32? column,
            String message)
        {
            var prefix = GetErrorPrefix(fileId, line, column);
            if (!String.IsNullOrEmpty(prefix))
            {
                message = $"{prefix} {message}";
            }

            Errors.Add(message);
            TraceWriter.Error(message);
        }

        internal INamedValueInfo[] GetExpressionNamedValues()
        {
            if (m_expressionValues?.Count > 0)
            {
                return m_expressionValues.Keys.Select(x => new NamedValueInfo<ContextValueNode>(x)).ToArray();
            }

            return null;
        }

        internal Int32 GetFileId(String file)
        {
            if (!FileIds.TryGetValue(file, out Int32 id))
            {
                id = FileIds.Count + 1;
                FileIds.Add(file, id);
                FileNames.Add(file);
                Memory.AddBytes(file);
            }

            return id;
        }

        internal String GetFileName(Int32 fileId)
        {
            return FileNames.Count >= fileId ? FileNames[fileId - 1] : null;
        }

        internal IReadOnlyList<String> GetFileTable()
        {
            return FileNames.AsReadOnly();
        }

        private String GetErrorPrefix(
            Int32? fileId,
            Int32? line,
            Int32? column)
        {
            var fileName = fileId.HasValue ? GetFileName(fileId.Value) : null;
            if (!String.IsNullOrEmpty(fileName))
            {
                if (line != null && column != null)
                {
                    return $"{fileName} {TemplateStrings.LineColumn(line, column)}:";
                }
                else
                {
                    return $"{fileName}:";
                }
            }
            else if (line != null && column != null)
            {
                return $"{TemplateStrings.LineColumn(line, column)}:";
            }
            else
            {
                return String.Empty;
            }
        }

        private TemplateValidationErrors m_errors;
        private IList<IFunctionInfo> m_expressionFunctions;
        private IDictionary<String, Object> m_expressionValues;
        private IDictionary<String, Int32> m_fileIds;
        private List<String> m_fileNames;
        private IDictionary<String, Object> m_state;
    }
}
