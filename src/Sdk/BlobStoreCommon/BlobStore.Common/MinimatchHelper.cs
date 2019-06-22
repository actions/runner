using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minimatch;
using GitHub.Services.Content.Common.Tracing;

namespace GitHub.Services.BlobStore.Common
{
    public static class MinimatchHelper
    {
        private static readonly bool isWindows = Helpers.IsWindowsPlatform(Environment.OSVersion);

        // https://github.com/Microsoft/azure-pipelines-task-lib/blob/master/node/docs/findingfiles.md#matchoptions
        private static readonly Options minimatchOptions = new Options
        {
            Dot = true,
            NoBrace = true,
            NoCase = isWindows
        };

        public static IEnumerable<Func<string, bool>> GetMinimatchFuncs(IEnumerable<string> minimatchPatterns, IAppTraceSource tracer)
        {
            IEnumerable<Func<string, bool>> minimatcherFuncs;
            if (minimatchPatterns != null && minimatchPatterns.Count() != 0)
            {
                string minimatchPatternMsg = $"Minimatch patterns: [{ string.Join(",", minimatchPatterns) }]";
                tracer.Info(minimatchPatternMsg);
                minimatcherFuncs = minimatchPatterns
                    .Where(pattern => !string.IsNullOrEmpty(pattern)) // get rid of empty strings.
                    .Select(pattern => Minimatcher.CreateFilter(pattern, minimatchOptions));
            }
            else
            {
                minimatcherFuncs = null;
            }

            return minimatcherFuncs;
        }
    }
}
