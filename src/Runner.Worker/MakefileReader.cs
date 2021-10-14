using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHub.Runner.Worker
{
    public static class MakefileReader
    {
        // Get the dependencies for a target from a Makefile.
        // Does not recurse into the dependencies of those dependencies.
        public static List<string> ReadTargetDependencies(IExecutionContext executionContext, string makefile, string target)
        {
            var targetToFind = target + ":";
            var lines = File.ReadLines(makefile);
            string targetLine = lines.FirstOrDefault(line => line.TrimStart().StartsWith(targetToFind));
            if (targetLine is null)
            {
                return null;
            }

            return targetLine.Split().Skip(1).ToList();
        }
    }
}