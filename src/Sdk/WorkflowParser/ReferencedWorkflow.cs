#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GitHub.Actions.WorkflowParser
{
    [DataContract]
    public sealed class ReferencedWorkflow
    {
        [JsonConstructor]
        public ReferencedWorkflow()
        {
        }

        private ReferencedWorkflow(ReferencedWorkflow infoToClone)
        {
            this.CallingWorkflowRef = infoToClone.CallingWorkflowRef;
            this.CallingWorkflowSha = infoToClone.CallingWorkflowSha;
            this.Repository = infoToClone.Repository;
            this.RepositoryId = infoToClone.RepositoryId;
            this.TenantId = infoToClone.TenantId;
            this.ResolvedRef = infoToClone.ResolvedRef;
            this.ResolvedSha = infoToClone.ResolvedSha;
            this.WorkflowRef = infoToClone.WorkflowRef;
            this.WorkflowFileFullPath = infoToClone.WorkflowFileFullPath;
            this.m_data = new Dictionary<string, string>(infoToClone.Data, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the repository's NWO, ex: <org>/<repo>
        /// </summary>
        [DataMember]
        public string Repository { get; set; }

        /// <summary>
        /// Gets or sets the repository's GitHub global relay id
        /// </summary>
        [DataMember]
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the branch/tag ref that was resolved to the calling workflow file
        /// refs/tags/ or refs/heads/
        /// This could be empty if the calling workflow file was referenced directly via commit SHA, or if there is no calling workflow
        /// </summary>
        [DataMember]
        public string CallingWorkflowRef { get; set; }

        /// <summary>
        /// Gets or sets the commit SHA for the calling workflow file
        /// This is empty if there is no calling workflow
        /// </summary>
        [DataMember]
        public string CallingWorkflowSha { get; set; }

        /// <summary>
        /// Gets or sets the repository's Actions tenant HostId
        /// </summary>
        [DataMember]
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the branch/tag ref that was resolved to the workflow file
        /// refs/tags/ or refs/heads/
        /// This could be empty if the workflow file was referenced directly via commit SHA
        /// </summary>
        [DataMember]
        public string ResolvedRef { get; set; }

        /// <summary>
        /// Gets or sets the commit SHA for the workflow file
        /// </summary>
        [DataMember]
        public string ResolvedSha { get; set; }

        /// <summary>
        /// Gets or sets the full path to the workflow file
        /// owner/repo/path/to/workflow.yml
        /// </summary>
        [DataMember]
        public string WorkflowFileFullPath { get; set; }

        /// <summary>
        /// Gets or sets the workflow ref.
        //  for a callable workflow:
        ///     owner/repo/path/to/workflow.yml@ref
        /// for main workflow file:
        ///     path/to/workflow.yml
        /// </summary>
        [DataMember]
        public string WorkflowRef { get; set; }

        [IgnoreDataMember]
        public string CanonicalWorkflowRef
        {
            get
            {
                // When ResolvedRef is not empty, the workflow ref was like "uses: my-org/my-repo/.github/workflows/foo.yml@main".
                // Otherwise the workflow ref was like "uses: my-org/my-repo/.github/workflows/foo.yml@664bf207624be1e27b36b04c058d01893570f45c"
                return string.Concat(
                    this.WorkflowFileFullPath,
                    "@",
                    !string.IsNullOrEmpty(this.ResolvedRef) ? this.ResolvedRef : this.ResolvedSha);
            }
        }

        public Dictionary<string, string> Data
        {
            get
            {
                if (m_data == null)
                {
                    m_data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                return m_data;
            }
        }

        public ReferencedWorkflow Clone()
        {
            return new ReferencedWorkflow(this);
        }

        public bool IsTrusted()
        {
            if (Data.TryGetValue("IsTrusted", out var isTrusted))
            {
                return string.Equals(isTrusted, bool.TrueString, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public bool IsRequiredWorkflow()
        {
            if (Data.TryGetValue("IsRequiredWorkflow", out var isRequiredWorkflow))
            {
                return string.Equals(isRequiredWorkflow, bool.TrueString, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public string GetPlanOwnerId()
        {
            if (Data.TryGetValue("PlanOwnerId", out var planOwnerId))
            {
                return planOwnerId;
            }
            return null;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_data?.Count == 0)
            {
                m_data = null;
            }
        }

        [DataMember(Name = "Data", EmitDefaultValue = false)]
        private Dictionary<string, string> m_data;
    }
}
