using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Runner.Listener
{
    public class EnvironmentParser
    {
        /// <summary>
        /// Load key value environment variable pairs from a text file and set environment variables based on the data.
        /// </summary>
        /// <param name="envFileContents">Optionally pass text file data instead of reading from a assembly root directory.</param>
        /// <returns>A dictionary of the environment variables that were set</returns>
        public static Dictionary<string, string> LoadAndSetEnvironment(string[] envFileContents = null)
        {
            if (null == envFileContents)
            {
                var binDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var rootDir = new DirectoryInfo(binDir).Parent.FullName;
                string envFile = Path.Combine(rootDir, ".env");

                if (!File.Exists(envFile))
                {
                    return new Dictionary<string, string>();
                }
                
                envFileContents = File.ReadAllLines(envFile);
            }

            var environmentDictionary = new Dictionary<string, string>();
            
            foreach (var env in envFileContents)
            {
                var kvp = env?.Split('=');
                if (kvp.Length > 1)
                {
                    var value = String.Join("=", kvp.Skip(1).ToArray());
                    if (!string.IsNullOrEmpty(value))
                    {
                        environmentDictionary[kvp[0]] = value;
                        Environment.SetEnvironmentVariable(kvp[0], value);
                    }
                }
            }

            return environmentDictionary;
        }
    }
}
