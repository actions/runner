using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Agent.Plugins.Log.TestResultParser.Contracts;

namespace Agent.Plugins.Log.TestResultParser.Plugin
{
    public class ParserFactory
    {
        public static IEnumerable<AbstractTestResultParser> GetTestResultParsers(ITestRunManager testRunManager, ITraceLogger logger, ITelemetryDataCollector telemetry)
        {
            var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dllFile = new FileInfo(Path.Combine(currentDir, "Agent.Plugins.Log.TestResultParser.Parser.dll"));
            Assembly.LoadFrom(dllFile.FullName);

            var interfaceType = typeof(AbstractTestResultParser);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => interfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .Select(x => (AbstractTestResultParser)Activator.CreateInstance(x, testRunManager, logger, telemetry));
        }
    }
}
