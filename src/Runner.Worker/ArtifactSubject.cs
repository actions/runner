using System;

namespace GitHub.Runner.Worker
{
    public enum ArtifactSubjectKind
    {
        File,
        OciSubject,
    }

    /// <summary>
    /// Represents a single artifact subject declared via the
    /// <c>GITHUB_ARTIFACTS</c> per-step environment file.
    /// </summary>
    public sealed class ArtifactSubject : IEquatable<ArtifactSubject>
    {
        public ArtifactSubject(string name, string digest, ArtifactSubjectKind kind)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name must not be null or empty.", nameof(name));
            }
            if (string.IsNullOrEmpty(digest))
            {
                throw new ArgumentException("Digest must not be null or empty.", nameof(digest));
            }
            Name = name;
            Digest = digest;
            Kind = kind;
        }

        public string Name { get; }
        public string Digest { get; }
        public ArtifactSubjectKind Kind { get; }

        public bool Equals(ArtifactSubject other)
        {
            if (other is null)
            {
                return false;
            }
            return string.Equals(Name, other.Name, StringComparison.Ordinal)
                && string.Equals(Digest, other.Digest, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as ArtifactSubject);

        public override int GetHashCode() => HashCode.Combine(Name, Digest);

        public override string ToString() => $"{Name}@{Digest}";
    }
}
