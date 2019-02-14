namespace Agent.Plugins.Log.TestResultParser.Plugin
{
    public class LogPreProcessor : ILogPreProcessor
    {
        /// <summary>
        /// Strips away the prefixed ##[error] from lines written to the error stream
        /// Additionally also returns null if the line was identified to be a debug, command
        /// or section log line
        /// </summary>
        public string ProcessData(string data)
        {
            if (data.StartsWith(debugLogPrefix))
            {
                return null;
            }

            if (data.StartsWith(errorLogPrefix))
            {
                return data.Substring(errorLogPrefix.Length);
            }

            if (data.StartsWith(commandLogPrefix))
            {
                return null;
            }

            if (data.StartsWith(sectionLogPrefix))
            {
                return null;
            }

            return data;
        }

        private const string debugLogPrefix = "##[debug]";

        private const string errorLogPrefix = "##[error]";

        private const string commandLogPrefix = "##[command]";

        private const string sectionLogPrefix = "##[section]";
    }
}
