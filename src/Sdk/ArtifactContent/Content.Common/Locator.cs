using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// Represents a set of path segments that can be used in combination
    /// as an identifier to locate content or metadata items and properties associated
    /// with content in backing storage.
    /// </summary>
    public class Locator : IComparable<Locator>, IComparable, IEquatable<Locator>
    {
        /// <summary>
        /// Separator used when parsing a Locator from string and when constructing string representation of current object
        /// </summary>
        public const char SeparatorChar = '/';

        /// <summary>
        /// During investigation each of the storage systems that we looked at
        /// were case-sensitive on their content identifiers including
        /// Artifactory, Maven, and Azure Storage.  We are preserving the concept
        /// that identifiers/segments should be case-sensitive and that the exact casing
        /// provided by the client itself is preserved.
        /// </summary>
        public static readonly StringComparison DefaultPathSegmentComparisonType = StringComparison.Ordinal;

        public static readonly string Separator = SeparatorChar.ToString();
        public static readonly char[] SeparatorCharArray = new char[] { SeparatorChar };

        public static readonly Locator Root;
        
        private static readonly string[] PathParsingDelimiters = new string[]
        {
            // The default path delimiters (/, \ and \\)
            Separator,
            Path.AltDirectorySeparatorChar.ToString(),
            Path.DirectorySeparatorChar.ToString()
        };

        private readonly string path;

        static Locator()
        {
            Root = new Locator();
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="Locator"/> class
        /// from the path segments of the provided Locators
        /// </summary>
        public Locator(Locator l1, Locator l2) : this(l1.PathSegments.Concat(l2.PathSegments))
        {
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="Locator"/> class
        /// from the path segments of the provided Locators
        /// </summary>
        public Locator(IEnumerable<Locator> locators) : this(locators.Select(l => l.PathSegments).SelectMany(l => l))
        {
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="Locator"/> class
        /// from the path segments provided
        /// </summary>
        /// <param name="pathSegments">Path segments defining the locator.</param>
        public Locator(params string[] pathSegments)
            : this((IEnumerable<String>)pathSegments)
        {
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="Locator"/> class
        /// from the path segments provided
        /// </summary>
        /// <param name="pathSegments">Path segments defining the locator.</param>
        public Locator(IEnumerable<string> pathSegments)
        {
            this.path = Separator + String.Join(Separator, pathSegments.SelectMany(s => GetPathSegments(s, null)));
        }

        /// <summary>
        /// Gets the locator value.  This value is
        /// a concatenation of all path segments associated with the locator
        /// </summary>
        public string Value
        {
            get { return path; }
        }

        /// <summary>
        /// Gets the Locator represented as a list of segments
        /// </summary>
        /// <returns>An IList (potentially empty) of path segments defining the Locator.</returns>
        public IList<string> PathSegments
        {
            get
            {
                return path.Split(SeparatorCharArray, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        /// <summary>
        /// Gets the count of path segments associated with the locator
        /// </summary>
        public int PathSegmentCount
        {
            get
            {
                return this.PathSegments.Count;
            }
        }

        /// <summary>
        /// Determines if a Locator is null or empty
        /// </summary>
        /// <param name="locator"></param>
        /// <returns><c>true</c> if Locator is null or has zero path segments, otherwise <c>false</c>.</returns>
        public static bool IsNullOrEmpty(Locator locator)
        {
            return null == locator || locator.PathSegmentCount == 0;
        }

        /// <summary>
        /// Creates a locator from the delimited path using the delimiter specifications
        /// provided
        /// </summary>
        /// <param name="delimitedPath">The delimited path from which to create the locator</param>
        /// <param name="delimiters">The delimiters to use when splitting the segment values in the delimited path</param>
        /// <returns>
        /// Type:  <see cref="Locator"/>
        /// A locator for the delimited path.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="delimitedPath"/> is <see langword="null" />.</exception>
        public static Locator Parse(string delimitedPath, params string[] delimiters)
        {
            if (delimitedPath == null)
            {
                throw new ArgumentNullException("delimitedPath");
            }
            
            // TODO: Consider removing in favor of constructor
            return new Locator(GetPathSegments(delimitedPath, delimiters));
        }

        public static bool operator ==(Locator x, Locator y)
        {
            if (ReferenceEquals(x, null))
            {
                return ReferenceEquals(y, null);
            }

            return x.Equals(y);
        }

        public static bool operator !=(Locator x, Locator y)
        {
            return !(x == y);
        }

        public bool MatchesEnumerationQuery(Locator prefix, PathOptions options)
        {
            if (!StartsWith(prefix))
            {
                return false;
            }

            int segmentCount = PathSegmentCount;
            int prefixSegmentCount = prefix.PathSegmentCount;

            return 
                (options.HasFlag(PathOptions.Target) && segmentCount == prefixSegmentCount) ||
                (options.HasFlag(PathOptions.ImmediateChildren) && segmentCount == prefixSegmentCount + 1) ||
                (options.HasFlag(PathOptions.DeepChildren) && segmentCount > prefixSegmentCount + 1);
        }

        /// <summary>
        /// Returns the parent locator
        /// </summary>
        /// <returns>
        /// Type:  <see cref="Locator" />
        /// The parent locator for the current locator instance.
        /// </returns>
        public Locator GetParent()
        {
            IList<string> segments = PathSegments;
            if (segments.Count <= 0)
            {
                return null;
            }

            return new Locator(string.Join(Separator, segments.Take(segments.Count - 1)));
        }

        /// <summary>
        /// Returns true/false whether the current locator path segments start
        /// with the path segments defined in the comparison locator.
        /// </summary>
        /// <param name="other">The locator with which to compare</param>
        /// <param name="comparison">Override default path comparison type.</param>
        /// <returns>
        /// True if the current locater path segments begin with the path segments
        /// defined in the comparison locator.
        /// </returns>
        public bool StartsWith(Locator other, StringComparison comparison)
        {
            if (!this.path.StartsWith(other.path, comparison))
            {
                return false;
            }

            IList<string> segments = this.PathSegments;
            IList<string> otherSegments = other.PathSegments;
            if (segments.Count < otherSegments.Count)
            {
                return false;
            }

            for (int i = 0; i < otherSegments.Count; i++)
            {
                if (!segments[i].Equals(otherSegments[i], comparison))
                {
                    return false;
                }
            }

            return true;
        }

        public bool StartsWith(Locator other)
        {
            return StartsWith(other, DefaultPathSegmentComparisonType);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string representation of a Locator</returns>
        public override string ToString()
        {
            return path;
        }

        /// <summary>
        /// Performs a comparison of the current locator 
        /// with another Locator.  
        /// </summary>
        /// <param name="other">A locator to compare with this locator.</param>
        /// <param name="comparison">Override default path comparison type.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The
        /// return value has the following meanings: 1) A return value less than zero means that
        /// this object is less than the other parameter.  2) A return value of zero means that
        /// this object is equal to other.  3) A return value greater than zero means that this 
        /// object is greater than other.
        /// </returns>
        public int CompareTo(Locator other, StringComparison comparison)
        {
            return string.Compare(path, other.path, comparison);
        }

        public int CompareTo(Locator other)
        {
            return CompareTo(other, DefaultPathSegmentComparisonType);
        }

        /// <summary>
        /// Performs a comparison of the current locator 
        /// with another Locator.
        /// </summary>
        /// <param name="other">An object to compare with this locator.</param>
        /// <param name="comparison">Override default path comparison type.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The
        /// return value has the following meanings: 1) A return value less than zero means that
        /// this object is less than the other parameter.  2) A return value of zero means that
        /// this object is equal to other.  3) A return value greater than zero means that this 
        /// object is greater than other.
        /// </returns>
        public int CompareTo(object other, StringComparison comparison)
        {
            Locator otherLocator = other as Locator;
            if (null == otherLocator)
            {
                otherLocator = Locator.Parse(other.ToString());
            }

            return this.CompareTo(otherLocator, comparison);
        }

        public int CompareTo(object other)
        {
            return this.CompareTo(other, DefaultPathSegmentComparisonType);
        }

        /// <summary>
        /// Returns true/false whether the current Locator is equal to
        /// the other Locator
        /// </summary>
        /// <param name="other">A locator to compare with this locator.</param>
        /// <param name="comparison">Override default path comparison type.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(Locator other, StringComparison comparison)
        {
            bool areEqual = false;
            if (null != other)
            {
                areEqual = other.path.Equals(this.path, comparison);
            }

            return areEqual;
        }

        public bool Equals(Locator other)
        {
            return Equals(other, DefaultPathSegmentComparisonType);
        }

        /// <summary>
        /// Returns true/false whether the current locator is equal to the item
        /// </summary>
        public bool Equals(object other, StringComparison comparison)
        {
            bool areEqual = false;

            if (null != other)
            {
                if (Object.ReferenceEquals(this, other))
                {
                    areEqual = true;
                }
                else
                {
                    // Apply value-type semantics to determine
                    // the equality of the instances
                    Locator otherLocator = other as Locator;
                    if (null == otherLocator)
                    {
                        otherLocator = Locator.Parse(other.ToString());
                    }

                    areEqual = this.Equals(otherLocator, comparison);
                }
            }

            return areEqual;
        }

        public override bool Equals(object other)
        {
            return Equals(other, DefaultPathSegmentComparisonType);
        }

        /// <summary>
        /// Returns a hash value unique to the path segments defined for
        /// the locator.
        /// </summary>
        /// <returns>
        /// A hash value created from the path segments of the locator
        /// </returns>
        public override int GetHashCode()
        {
            return path.GetHashCode();
        }

        private static string[] GetPathSegments(string delimitedPath, params string[] delimiters)
        {
            string[] pathSegments = null;
            if (delimiters == null || !delimiters.Any())
            {
                pathSegments = delimitedPath.Split(
                    PathParsingDelimiters,
                    StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                pathSegments = delimitedPath.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            }

            return pathSegments;
        }
    }
}
