using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class RepositoryPropertyNames
    {
        public static readonly String Id = "id";
        public static readonly String Mappings = "mappings";
        public static readonly String Name = "name";
        public static readonly String Ref = "ref";
        public static readonly String Type = "type";
        public static readonly String Url = "url";
        public static readonly String Version = "version";
        public static readonly String VersionInfo = "versionInfo";
        public static readonly String VersionSpec = "versionSpec";
        public static readonly String Shelveset = "shelveset";
        public static readonly String Project = "project";
        public static readonly String Path = "path";
        public static readonly String CheckoutOptions = "checkoutOptions";
        public static readonly String DefaultBranch = "defaultBranch";
        public static readonly String ExternalId = "externalId";
        public static readonly String IsJustInTimeRepository = "isJustInTimeRepository";
    }

    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VersionInfo
    {
        [DataMember(EmitDefaultValue = false)]
        public String Author { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Message { get; set; }
    }

    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CheckoutOptions
    {
        [JsonConstructor]
        public CheckoutOptions()
        { }

        private CheckoutOptions(CheckoutOptions optionsToCopy)
        {
            this.Clean = optionsToCopy.Clean;
            this.FetchDepth = optionsToCopy.FetchDepth;
            this.Lfs = optionsToCopy.Lfs;
            this.Submodules = optionsToCopy.Submodules;
            this.PersistCredentials = optionsToCopy.PersistCredentials;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Clean{ get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String FetchDepth{ get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Lfs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Submodules { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String PersistCredentials { get; set; }

        public CheckoutOptions Clone()
        {
            return new CheckoutOptions(this);
        }
    }

    /// <summary>
    /// Provides a data contract for a repository resource referenced by a pipeline.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class RepositoryResource : Resource
    {
        /// <summary>
        /// Initializes a new <c>RepositoryReference</c> instance with default values.
        /// </summary>
        public RepositoryResource()
        {
        }

        private RepositoryResource(RepositoryResource referenceToCopy)
            : base(referenceToCopy)
        {
        }

        /// <summary>
        /// Gets or sets a unique identifier for this repository.
        /// </summary>
        public String Id
        {
            get
            {
                return this.Properties.Get<String>(RepositoryPropertyNames.Id);
            }
            set
            {
                this.Properties.Set(RepositoryPropertyNames.Id, value);
            }
        }

        /// <summary>
        /// Gets or sets the type of repository.
        /// </summary>
        public String Type
        {
            get
            {
                return this.Properties.Get<String>(RepositoryPropertyNames.Type);
            }
            set
            {
                this.Properties.Set(RepositoryPropertyNames.Type, value);
            }
        }

        /// <summary>
        /// Gets or sets the url of the repository.
        /// </summary>
        public Uri Url
        {
            get
            {
                return this.Properties.Get<Uri>(RepositoryPropertyNames.Url);
            }
            set
            {
                this.Properties.Set(RepositoryPropertyNames.Url, value);
            }
        }

        /// <summary>
        /// Gets or sets the version of the repository.
        /// </summary>
        public String Version
        {
            get
            {
                return this.Properties.Get<String>(RepositoryPropertyNames.Version);
            }
            set
            {
                this.Properties.Set(RepositoryPropertyNames.Version, value);
            }
        }

        /// <summary>
        /// Creates a clone of the current repository instance.
        /// </summary>
        /// <returns>A new <c>RepositoryReference</c> instance which is a copy of the current instance</returns>
        public RepositoryResource Clone()
        {
            return new RepositoryResource(this);
        }
    }
}
