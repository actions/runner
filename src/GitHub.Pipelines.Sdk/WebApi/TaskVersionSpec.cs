using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    public sealed class TaskVersionSpec
    {
        /// <summary>
        /// Gets or sets the major version component.
        /// </summary>
        public Int32? Major
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the minor version component.
        /// </summary>
        public Int32? Minor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the patch version component.
        /// </summary>
        public Int32? Patch
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value locking the semantic version to test.
        /// </summary>
        public Boolean IsTest
        {
            get;
            set;
        }

        /// <summary>
        /// Provides a string representation of the version specification.
        /// </summary>
        /// <returns>A printable string representation of a version specification</returns>
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (this.Major == null)
            {
                sb.Append("*");
            }
            else
            {
                sb.Append(this.Major.Value);
                if (this.Minor != null)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ".{0}", this.Minor.Value);
                    if (this.Patch != null)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, ".{0}", this.Patch.Value);
                    }
                    else
                    {
                        sb.Append(".*");
                    }
                }
                else
                {
                    sb.Append(".*");
                }
            }

            if (this.IsTest)
            {
                sb.Append("-test");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Provides an explicit conversion constructor for converting from a <c>String</c>.
        /// </summary>
        /// <param name="version">The version specification string</param>
        /// <returns>A version specification object</returns>
        /// <exception cref="System.ArgumentException">When the provided version string is not valid</exception>
        public static explicit operator TaskVersionSpec(String version)
        {
            return Parse(version);
        }

        /// <summary>
        /// Finds the closest version match for the current specification. If no match can be found then a null
        /// value is returned.
        /// </summary>
        /// <param name="versions">The list of versions available for matching</param>
        /// <returns>The version which matches the specification if found; otherwise, null</returns>
        public TaskVersion Match(IEnumerable<TaskVersion> versions)
        {
            ArgumentUtility.CheckForNull(versions, nameof(versions));

            // Do not evaluate until the end so we only actually iterate the list a single time. Since LINQ returns
            // lazy evaluators from the Where method, we can avoid multiple iterations by leaving the variable
            // as IEnumerable and performing the iteration after all clauses have been concatenated.
            var matchedVersions = versions.Where(x => x.IsTest == this.IsTest);
            if (this.Major != null)
            {
                matchedVersions = matchedVersions.Where(x => x.Major == this.Major);
                if (this.Minor != null)
                {
                    matchedVersions = matchedVersions.Where(x => x.Minor == this.Minor);
                    if (this.Patch != null)
                    {
                        matchedVersions = matchedVersions.Where(x => x.Patch == this.Patch);
                    }
                }
            }

            return matchedVersions.OrderByDescending(x => x).FirstOrDefault();
        }

        public TaskDefinition Match(IEnumerable<TaskDefinition> definitions)
        {
            ArgumentUtility.CheckForNull(definitions, nameof(definitions));

            // Do not evaluate until the end so we only actually iterate the list a single time. Since LINQ returns
            // lazy evaluators from the Where method, we can avoid multiple iterations by leaving the variable
            // as IEnumerable and performing the iteration after all clauses have been concatenated.
            var matchedDefinitions = definitions.Where(x => x.Version.IsTest == this.IsTest);
            if (this.Major != null)
            {
                matchedDefinitions = matchedDefinitions.Where(x => x.Version.Major == this.Major);
                if (this.Minor != null)
                {
                    matchedDefinitions = matchedDefinitions.Where(x => x.Version.Minor == this.Minor);
                    if (this.Patch != null)
                    {
                        matchedDefinitions = matchedDefinitions.Where(x => x.Version.Patch == this.Patch);
                    }
                }
            }

            return matchedDefinitions.OrderByDescending(x => x.Version).FirstOrDefault();
        }

        public static TaskVersionSpec Parse(String version)
        {
            TaskVersionSpec versionSpec;
            if (!TryParse(version, out versionSpec))
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The value {0} is not a valid version specification", version), "version");
            }
            return versionSpec;
        }

        public static Boolean TryParse(
            String version,
            out TaskVersionSpec versionSpec)
        {
            String[] versionComponents = version.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (versionComponents.Length < 1 || versionComponents.Length > 3)
            {
                versionSpec = null;
                return false;
            }

            Int32? major = null;
            Int32? minor = null;
            Int32? patch = null;
            Boolean isTest = false;
            String lastComponent = versionComponents[versionComponents.Length - 1];
            if (lastComponent.EndsWith("-test", StringComparison.OrdinalIgnoreCase))
            {
                isTest = true;
                versionComponents[versionComponents.Length - 1] = lastComponent.Remove(lastComponent.Length - "-test".Length);
            }

            if (versionComponents.Length == 1)
            {
                if (!TryParseVersionComponent(version, "major", versionComponents[0], true, out major))
                {
                    versionSpec = null;
                    return false;
                }
            }
            else if (versionComponents.Length == 2)
            {
                if (!TryParseVersionComponent(version, "major", versionComponents[0], false, out major) ||
                    !TryParseVersionComponent(version, "minor", versionComponents[1], true, out minor))
                {
                    versionSpec = null;
                    return false;
                }
            }
            else
            {
                if (!TryParseVersionComponent(version, "major", versionComponents[0], false, out major) ||
                    !TryParseVersionComponent(version, "minor", versionComponents[1], false, out minor) ||
                    !TryParseVersionComponent(version, "patch", versionComponents[2], true, out patch))
                {
                    versionSpec = null;
                    return false;
                }
            }

            versionSpec = new TaskVersionSpec { Major = major, Minor = minor, Patch = patch, IsTest = isTest };
            return true;
        }

        private static Boolean TryParseVersionComponent(
            String version,
            String name,
            String value,
            Boolean allowStar,
            out Int32? versionValue)
        {
            versionValue = null;

            Int32 parsedVersion;
            if (Int32.TryParse(value, out parsedVersion))
            {
                versionValue = parsedVersion;
            }
            else if (!allowStar || value != "*")
            {
                return false;
            }

            return true;
        }
    }
}
