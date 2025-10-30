using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Actions.WorkflowParser.Conversion;
using Newtonsoft.Json;

namespace GitHub.Actions.WorkflowParser
{
    [DataContract]
    public class Permissions
    {
        [JsonConstructor]
        public Permissions()
        {
        }

        public Permissions(Permissions copy)
        {
            Actions = copy.Actions;
			ArtifactMetadata = copy.ArtifactMetadata;
            Attestations = copy.Attestations;
            Checks = copy.Checks;
            Contents = copy.Contents;
            Deployments = copy.Deployments;
            Issues = copy.Issues;
            Discussions = copy.Discussions;
            Packages = copy.Packages;
            Pages = copy.Pages;
            PullRequests = copy.PullRequests;
            RepositoryProjects = copy.RepositoryProjects;
            Statuses = copy.Statuses;
            SecurityEvents = copy.SecurityEvents;
            IdToken = copy.IdToken;
            Models = copy.Models;
        }

        public Permissions(
            PermissionLevel permissionLevel,
            bool includeIdToken,
            bool includeAttestations,
            bool includeModels)
        {
            Actions = permissionLevel;
			ArtifactMetadata = permissionLevel;
            Attestations = includeAttestations ? permissionLevel : PermissionLevel.NoAccess;
            Checks = permissionLevel;
            Contents = permissionLevel;
            Deployments = permissionLevel;
            Issues = permissionLevel;
            Discussions = permissionLevel;
            Packages = permissionLevel;
            Pages = permissionLevel;
            PullRequests = permissionLevel;
            RepositoryProjects = permissionLevel;
            Statuses = permissionLevel;
            SecurityEvents = permissionLevel;
            IdToken = includeIdToken ? permissionLevel : PermissionLevel.NoAccess;
            // Models must not have higher permissions than Read
            Models = includeModels 
                ? (permissionLevel == PermissionLevel.Write ? PermissionLevel.Read : permissionLevel) 
                : PermissionLevel.NoAccess;
        }

        private static KeyValuePair<string, (PermissionLevel, PermissionLevel)>[] ComparisonKeyMapping(Permissions left, Permissions right)
        {
            return new[]
            {
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("actions", (left.Actions, right.Actions)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("artifact-metadata", (left.ArtifactMetadata, right.ArtifactMetadata)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("attestations", (left.Attestations, right.Attestations)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("checks", (left.Checks, right.Checks)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("contents", (left.Contents, right.Contents)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("deployments", (left.Deployments, right.Deployments)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("discussions", (left.Discussions, right.Discussions)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("issues", (left.Issues, right.Issues)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("packages", (left.Packages, right.Packages)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("pages", (left.Pages, right.Pages)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("pull-requests", (left.PullRequests, right.PullRequests)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("repository-projects", (left.RepositoryProjects, right.RepositoryProjects)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("statuses", (left.Statuses, right.Statuses)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("security-events", (left.SecurityEvents, right.SecurityEvents)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("id-token", (left.IdToken, right.IdToken)),
                new KeyValuePair<string, (PermissionLevel, PermissionLevel)>("models", (left.Models, right.Models)),
            };
        }

        [DataMember(Name = "actions", EmitDefaultValue = false)]
        public PermissionLevel Actions
        {
            get;
            set;
        }

        [DataMember(Name = "artifact-metadata", EmitDefaultValue = false)]
        public PermissionLevel ArtifactMetadata
        {
            get;
            set;
        }

        [DataMember(Name = "attestations", EmitDefaultValue = false)]
        public PermissionLevel Attestations
        {
            get;
            set;
        }

        [DataMember(Name = "checks", EmitDefaultValue = false)]
        public PermissionLevel Checks
        {
            get;
            set;
        }

        [DataMember(Name = "contents", EmitDefaultValue = false)]
        public PermissionLevel Contents
        {
            get;
            set;
        }

        [DataMember(Name = "deployments", EmitDefaultValue = false)]
        public PermissionLevel Deployments
        {
            get;
            set;
        }

        [DataMember(Name = "discussions", EmitDefaultValue = false)]
        public PermissionLevel Discussions
        {
            get;
            set;
        }

        [DataMember(Name = "id-token", EmitDefaultValue = false)]
        public PermissionLevel IdToken
        {
            get;
            set;
        }

        [DataMember(Name = "issues", EmitDefaultValue = false)]
        public PermissionLevel Issues
        {
            get;
            set;
        }

        [DataMember(Name = "models", EmitDefaultValue = false)]
        public PermissionLevel Models
        {
            get;
            set;
        }

        [DataMember(Name = "packages", EmitDefaultValue = false)]
        public PermissionLevel Packages
        {
            get;
            set;
        }

        [DataMember(Name = "pages", EmitDefaultValue = false)]
        public PermissionLevel Pages
        {
            get;
            set;
        }

        [DataMember(Name = "pull-requests", EmitDefaultValue = false)]
        public PermissionLevel PullRequests
        {
            get;
            set;
        }

        [DataMember(Name = "repository-projects", EmitDefaultValue = false)]
        public PermissionLevel RepositoryProjects
        {
            get;
            set;
        }

        [DataMember(Name = "security-events", EmitDefaultValue = false)]
        public PermissionLevel SecurityEvents
        {
            get;
            set;
        }

        [DataMember(Name = "statuses", EmitDefaultValue = false)]
        public PermissionLevel Statuses
        {
            get;
            set;
        }

        public Permissions Clone()
        {
            return new Permissions(this);
        }

        internal bool ViolatesMaxPermissions(Permissions maxPermissions, out List<PermissionLevelViolation> permissionsViolations)
        {
            var mapping = Permissions.ComparisonKeyMapping(this, maxPermissions);
            permissionsViolations = new List<PermissionLevelViolation>();

            foreach (var (key, (permissionLevel, maxPermissionLevel)) in mapping)
            {
                if (!permissionLevel.IsLessThanOrEqualTo(maxPermissionLevel))
                {
                    permissionsViolations.Add(new PermissionLevelViolation(key, permissionLevel, maxPermissionLevel));
                }
            }

            return permissionsViolations.Count > 0;
        }
    }
}
