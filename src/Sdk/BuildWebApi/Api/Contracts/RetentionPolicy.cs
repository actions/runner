using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a retention policy for a build definition.
    /// </summary>
    [DataContract]
    public class RetentionPolicy : BaseSecuredObject
    {
        public RetentionPolicy()
            : this(null)
        {
        }

        public RetentionPolicy(
            ISecuredObject securedObject)
            : base(securedObject)
        {
            DaysToKeep = 30;  // default to 30 days
            MinimumToKeep = 1; // default to 1
            DeleteBuildRecord = true; // default to set Deleted bit on build records 
            DeleteTestResults = false; // For old build definitions, it has to be false. This value in New Definitions will be handled in ts files.
        }

        /// <summary>
        /// The list of branches affected by the retention policy.
        /// </summary>
        public List<String> Branches
        {
            get
            {
                if (m_branches == null)
                {
                    m_branches = new List<String>();
                }

                return m_branches;
            }
            internal set
            {
                m_branches = value;
            }
        }

        /// <summary>
        /// The number of days to keep builds.
        /// </summary>
        [DataMember]
        public Int32 DaysToKeep
        {
            get
            {
                return m_daysToKeep;
            }

            set
            {
                if (value < 0)
                {
                    m_daysToKeep = 0;
                }
                else
                {
                    m_daysToKeep = value;
                }
            }
        }

        /// <summary>
        /// The minimum number of builds to keep.
        /// </summary>
        [DataMember]
        public Int32 MinimumToKeep
        {
            get
            {
                return m_minimumToKeep;
            }
            set
            {
                if (value < 0)
                {
                    m_minimumToKeep = 0;
                }
                else
                {
                    m_minimumToKeep = value;
                }
            }
        }

        /// <summary>
        /// Indicates whether the build record itself should be deleted.
        /// </summary>
        [DataMember]
        public Boolean DeleteBuildRecord
        {
            get;
            set;
        }

        /// <summary>
        /// The list of artifacts to delete.
        /// </summary>
        public List<String> ArtifactsToDelete
        {
            get
            {
                if (m_artifactsToDelete == null)
                {
                    m_artifactsToDelete = new List<String>();
                }

                return m_artifactsToDelete;
            }
            internal set
            {
                m_artifactsToDelete = value;
            }
        }

        // This list contains the types of artifacts to be deleted.
        // These are different from ArtifactsToDelete because for certain artifacts giving user a choice for every single artifact can become cumbersome.
        // e.g. artifacts in file share - user can choose to delete/keep all the artifacts in file share
        /// <summary>
        /// The list of types of artifacts to delete.
        /// </summary>
        public List<String> ArtifactTypesToDelete
        {
            get
            {
                if (m_artifactTypesToDelete == null)
                {
                    m_artifactTypesToDelete = new List<String>();
                }

                return m_artifactTypesToDelete;
            }
            internal set
            {
                m_artifactTypesToDelete = value;
            }
        }

        /// <summary>
        /// Indicates whether to delete test results associated with the build.
        /// </summary>
        [DataMember]
        public Boolean DeleteTestResults
        {
            get;
            set;
        }

        [DataMember(Name = "Branches", EmitDefaultValue = false)]
        private List<String> m_branches;

        [DataMember(Name = "Artifacts", EmitDefaultValue = false)]
        private List<String> m_artifactsToDelete;

        [DataMember(Name = "ArtifactTypesToDelete", EmitDefaultValue = false)]
        private List<String> m_artifactTypesToDelete;

        private Int32 m_daysToKeep;
        private Int32 m_minimumToKeep;
    }
}
