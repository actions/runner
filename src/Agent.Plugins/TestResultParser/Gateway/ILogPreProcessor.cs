namespace Agent.Plugins.Log.TestResultParser.Plugin
{
    public interface ILogPreProcessor
    {
        /// <summary>
        /// Pre processes the data performing sanitization operations if any before
        /// sending it over to the parsers
        /// </summary>
        string ProcessData(string data);
    }
}
