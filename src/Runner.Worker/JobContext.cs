using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker
{
    public sealed class JobContext : DictionaryContextData
    {
        public ActionResult? Status
        {
            get
            {
                if (this.TryGetValue("status", out var status) && status is StringContextData statusString)
                {
                    return EnumUtil.TryParse<ActionResult>(statusString);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                this["status"] = new StringContextData(value.ToString().ToLowerInvariant());
            }
        }

        public DictionaryContextData Services
        {
            get
            {
                if (this.TryGetValue("services", out var services) && services is DictionaryContextData servicesDictionary)
                {
                    return servicesDictionary;
                }
                else
                {
                    this["services"] = new DictionaryContextData();
                    return this["services"] as DictionaryContextData;
                }
            }
        }

        public DictionaryContextData Container
        {
            get
            {
                if (this.TryGetValue("container", out var container) && container is DictionaryContextData containerDictionary)
                {
                    return containerDictionary;
                }
                else
                {
                    this["container"] = new DictionaryContextData();
                    return this["container"] as DictionaryContextData;
                }
            }
        }

        public double? CheckRunId
        {
            get
            {
                if (this.TryGetValue("check_run_id", out var value) && value is NumberContextData number)
                {
                    return number.Value;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value.HasValue)
                {
                    this["check_run_id"] = new NumberContextData(value.Value);
                }
                else
                {
                    this["check_run_id"] = null;
                }
            }
        }

        public string WorkflowRef
        {
            get
            {
                if (this.TryGetValue("workflow_ref", out var value) && value is StringContextData str)
                {
                    return str.Value;
                }
                return null;
            }
            set
            {
                this["workflow_ref"] = value != null ? new StringContextData(value) : null;
            }
        }

        public string WorkflowSha
        {
            get
            {
                if (this.TryGetValue("workflow_sha", out var value) && value is StringContextData str)
                {
                    return str.Value;
                }
                return null;
            }
            set
            {
                this["workflow_sha"] = value != null ? new StringContextData(value) : null;
            }
        }

        public string WorkflowRepository
        {
            get
            {
                if (this.TryGetValue("workflow_repository", out var value) && value is StringContextData str)
                {
                    return str.Value;
                }
                return null;
            }
            set
            {
                this["workflow_repository"] = value != null ? new StringContextData(value) : null;
            }
        }

        public string WorkflowFilePath
        {
            get
            {
                if (this.TryGetValue("workflow_file_path", out var value) && value is StringContextData str)
                {
                    return str.Value;
                }
                return null;
            }
            set
            {
                this["workflow_file_path"] = value != null ? new StringContextData(value) : null;
            }
        }

        /// <summary>
        /// Parses a composite workflow_ref (e.g. "owner/repo/.github/workflows/file.yml@refs/heads/main")
        /// and populates workflow_repository and workflow_file_path if they are not already set.
        /// </summary>
        public void DeriveWorkflowRefComponents()
        {
            var workflowRef = WorkflowRef;
            if (string.IsNullOrEmpty(workflowRef))
            {
                return;
            }

            // Format: owner/repo/.github/workflows/file.yml@ref
            var atIndex = workflowRef.IndexOf('@');
            var pathPart = atIndex >= 0 ? workflowRef.Substring(0, atIndex) : workflowRef;

            // Split at /.github/workflows/ to correctly handle repos named ".github"
            // e.g. "octo-org/.github/.github/workflows/ci.yml" → repo="octo-org/.github"
            var marker = "/.github/workflows/";
            var markerIndex = pathPart.IndexOf(marker);
            if (markerIndex < 0)
            {
                return;
            }

            var repo = pathPart.Substring(0, markerIndex);
            var filePath = pathPart.Substring(markerIndex + 1); // skip leading '/'

            // Validate repo is owner/repo format (must have at least one slash with non-empty segments)
            var slashIndex = repo.IndexOf('/');
            if (slashIndex <= 0 || slashIndex >= repo.Length - 1)
            {
                return;
            }

            if (WorkflowRepository == null || WorkflowRepository == "")
            {
                WorkflowRepository = repo;
            }

            if (WorkflowFilePath == null || WorkflowFilePath == "")
            {
                WorkflowFilePath = filePath;
            }
        }
    }
}
