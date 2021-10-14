using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHub.Runner.Worker
{
    public static class MakefileManager
    {
        // Convert the `all` target to a set of steps of its dependencies.
        // Does not recurse into the dependencies of those steps.
        public static ActionDefinitionData Load(IExecutionContext executionContext, string makefile, string target)
        {
            var dependencies = ReadTargetDependencies(executionContext, makefile, target);
            if (dependencies.Count == 0)
            {
                return null;
            }

            return new ActionDefinitionData
            {
                Name = $"make {target}",
                Description = "Execute a Makefile target",
                Execution = new MakefileExecutionData
                {
                    Targets = dependencies,
                    InitCondition = "always()",
                    CleanupCondition = "always()",
                }
            };
        }

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