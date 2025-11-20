#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser
{
    [DataContract]
    public class WorkflowTemplate
    {
        public IDictionary<String, String> InputTypes
        {
            get
            {
                if (m_inputTypes == null)
                {
                    m_inputTypes = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_inputTypes;
            }
        }
        [DataMember(Order = 0, Name = "input-types", EmitDefaultValue = false)]
        private Dictionary<String, String>? m_inputTypes;

        [DataMember(Order = 1, Name = "env", EmitDefaultValue = false)]
        public TemplateToken? Env
        {
            get;
            set;
        }

        [DataMember(Order = 2, Name = "permissions", EmitDefaultValue = false)]
        public Permissions? Permissions
        {
            get;
            set;
        }

        [DataMember(Order = 3, Name = "defaults", EmitDefaultValue = false)]
        public TemplateToken? Defaults
        {
            get;
            set;
        }

        [DataMember(Order = 4, Name = "concurrency", EmitDefaultValue = false)]
        public TemplateToken? Concurrency
        {
            get;
            set;
        }

        public IList<IJob> Jobs
        {
            get
            {
                if (m_jobs == null)
                {
                    m_jobs = new List<IJob>();
                }
                return m_jobs;
            }
        }
        [DataMember(Order = 5, Name = "jobs", EmitDefaultValue = false)]
        private List<IJob>? m_jobs;

        public List<String> FileTable
        {
            get
            {
                if (m_fileTable == null)
                {
                    m_fileTable = new List<String>();
                }
                return m_fileTable;
            }
        }
        [DataMember(Order = 6, Name = "file-table", EmitDefaultValue = false)]
        private List<String>? m_fileTable;

        public IList<WorkflowValidationError> Errors
        {
            get
            {
                if (m_errors == null)
                {
                    m_errors = new List<WorkflowValidationError>();
                }
                return m_errors;
            }
        }
        [DataMember(Order = 7, Name = "errors", EmitDefaultValue = false)]
        private List<WorkflowValidationError>? m_errors;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public List<FileInfo> FileInfo
        {
            get
            {
                if (m_fileInfo == null)
                {
                    m_fileInfo = new List<FileInfo>();
                }
                return m_fileInfo;
            }
        }

        [DataMember(Order = 8, Name = "file-info", EmitDefaultValue = false)]
        private List<FileInfo>? m_fileInfo;

        [IgnoreDataMember]
        public String? InitializationLog
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public Telemetry? Telemetry
        {
            get;
            set;
        }

        public void CheckErrors()
        {
            if (m_errors?.Count > 0)
            {
                throw new WorkflowValidationException(m_errors);
            }
        }

        internal WorkflowTemplate Clone(bool omitSource)
        {
            var result = new WorkflowTemplate
            {
                Concurrency = Concurrency?.Clone(omitSource),
                Defaults = Defaults?.Clone(omitSource),
                Env = Env?.Clone(omitSource),
                Permissions = Permissions?.Clone(),
            };
            result.Errors.AddRange(Errors.Select(x => x.Clone()));
            result.InitializationLog = InitializationLog;
            result.InputTypes.AddRange(InputTypes);
            result.Jobs.AddRange(Jobs.Select(x => x.Clone(omitSource)));
            if (!omitSource)
            {
                result.FileTable.AddRange(FileTable);
                result.FileInfo.AddRange(FileInfo.Select(x => x.Clone()));
            }
            return result;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_inputTypes?.Count == 0)
            {
                m_inputTypes = null;
            }

            if (m_jobs?.Count == 0)
            {
                m_jobs = null;
            }

            if (m_errors?.Count == 0)
            {
                m_errors = null;
            }

            if (m_fileTable?.Count == 0)
            {
                m_fileTable = null;
            }

            if (m_fileInfo?.Count == 0)
            {
                m_fileInfo = null;
            }
        }
    }
}
