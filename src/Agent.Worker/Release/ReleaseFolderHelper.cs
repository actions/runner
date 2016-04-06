using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public static class ReleaseFolderHelper
    {
        public static string CreateShortHash(string agentName, string teamProject, string releaseDefinitionName)
        {
            var formattedString = string.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", agentName, teamProject, releaseDefinitionName);
            using (var sha1Hash = SHA1.Create())
            {
                var data = sha1Hash.ComputeHash(Encoding.UTF8.GetBytes(formattedString));
                var stringBuilder = new StringBuilder();
                foreach (var t in data)
                {
                    stringBuilder.Append(t.ToString("x2", CultureInfo.InvariantCulture));
                }

                return stringBuilder.ToString().Substring(0, 9);
            }
        }
    }
}