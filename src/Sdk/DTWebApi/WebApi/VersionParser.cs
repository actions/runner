using GitHub.Services.Common;
using System;

namespace GitHub.DistributedTask.WebApi
{
    public static class VersionParser
    {
        public static void ParseVersion(
            String version,
            out Int32 major,
            out Int32 minor,
            out Int32 patch,
            out String semanticVersion)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(version, "version");

            String[] segments = version.Split(new char[] { '.', '-' }, StringSplitOptions.None);
            if (segments.Length < 3 || segments.Length > 4)
            {
                throw new ArgumentException("wrong number of segments");
            }

            if (!Int32.TryParse(segments[0], out major))
            {
                throw new ArgumentException("major");
            }

            if (!Int32.TryParse(segments[1], out minor))
            {
                throw new ArgumentException("minor");
            }

            if (!Int32.TryParse(segments[2], out patch))
            {
                throw new ArgumentException("patch");
            }

            semanticVersion = null;
            if (segments.Length == 4)
            {
                semanticVersion = segments[3];
            }
        }
    }
}
