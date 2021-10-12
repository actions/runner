using System.Text.RegularExpressions;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Handlers
{
    public sealed class GNUMakeManager
    {
        public GNUMakeManager(IExecutionContext executionContext)
        {
            _executionContext = executionContext;
        }

        public void OnDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            var line = e.Data;


            var startMatch = s_startMakeTargetRegex.Match(line);
            if (startMatch.Success)
            {
                _executionContext.Output($"[DEBUG] Matched the start of target {startMatch.Value}.");
            }

            var endMatch = s_endMakeTargetRegex.Match(line);
            if (endMatch.Success)
            {
                _executionContext.Output($"[DEBUG] Matched the end of target {endMatch.Value}.");
            }
        }

        private readonly IExecutionContext _executionContext;

        private static readonly Regex s_startMakeTargetRegex = new Regex(@"Must remake target `(.+)'\.$", RegexOptions.Compiled);
        private static readonly Regex s_endMakeTargetRegex = new Regex(@"Successfully remade target file `(.+)'\.$", RegexOptions.Compiled);
    }
}