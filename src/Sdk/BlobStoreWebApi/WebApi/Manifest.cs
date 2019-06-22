using System;
using System.Collections.Generic;
using GitHub.Services.BlobStore.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GitHub.Services.BlobStore.WebApi
{
    [CLSCompliant(false)]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class Manifest
    {
        private const string versionV1 = "1.0.0";
        private const string versionV2 = "1.1.0";
        private static string LatestVersion => versionV2;
        private static readonly ISet<string> validVersions = CreateValidVersionsSet();

        public Manifest(IList<ManifestItem> items) : this(LatestVersion, items, manifestReferences: null)
        {
        }

        [JsonConstructor]
        public Manifest(string manifestFormat, IList<ManifestItem> items, IList<ManifestReference> manifestReferences = null)
        {
            if (!validVersions.Contains(manifestFormat))
            {
                throw new ArgumentException("The manifest version is not valid.");
            }

            ManifestFormat = manifestFormat;
            Items = items;
            ManifestReferences = manifestReferences ?? new List<ManifestReference>();
        }

        [JsonProperty(PropertyName = "manifestFormat", Required = Required.Always)]
        public readonly string ManifestFormat;

        [JsonProperty(PropertyName = "items", Required = Required.Always)]
        public readonly IList<ManifestItem> Items;

        [JsonProperty(PropertyName = "manifestReferences", Required = Required.Default)]
        public readonly IList<ManifestReference> ManifestReferences;

        private static ISet<string> CreateValidVersionsSet()
        {
            ISet<string> validVersions = new HashSet<string>()
            {
                versionV1,
                versionV2
            };
            return validVersions;
        }
    }

    [CLSCompliant(false)]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class ManifestItem
    {
        public ManifestItem(string path, DedupInfo blob) : this(path, blob, ManifestItemType.File)
        {
        }

        [JsonConstructor]
        public ManifestItem(string path, DedupInfo blob, ManifestItemType type)
        {
            Path = path;
            Blob = blob;
            Type = type;
        }

        [JsonProperty(PropertyName = "path", Required = Required.Always)]
        public readonly string Path;

        [JsonProperty(PropertyName = "blob", Required = Required.Default)]
        public readonly DedupInfo Blob;

        [JsonProperty(PropertyName = "type", Required = Required.Default)]
        public readonly ManifestItemType Type = ManifestItemType.File;

        public bool ShouldSerializeType()
        {
            return Type != ManifestItemType.File;
        }

        public bool ShouldSerializeBlob()
        {
            return Type == ManifestItemType.File;
        }
    }

    [CLSCompliant(false)]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class DedupInfo
    {
        [JsonConstructor]
        public DedupInfo(string id, ulong size)
        {
            Id = id;
            Size = size;
        }

        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public readonly string Id;

        [JsonProperty(PropertyName = "size", Required = Required.Always)]
        public readonly ulong Size;
    }

    [CLSCompliant(false)]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class ManifestReference
    {
        [JsonConstructor]
        public ManifestReference(DedupIdentifier manifestId)
        {
            ManifestId = manifestId;
        }

        [JsonProperty(PropertyName = "manifestId", Required = Required.Always)]
        public readonly DedupIdentifier ManifestId;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ManifestItemType
    {
        File,
        EmptyDirectory
    }
}
