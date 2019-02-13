using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Agent.Plugins.Log.TestResultParser.Contracts;

namespace Agent.Plugins.Log.TestResultParser.Plugin
{
    public class LogParserGateway : ILogParserGateway, IBus<LogData>
    {
        /// <inheritdoc />
        public async Task InitializeAsync(IClientFactory clientFactory, IPipelineConfig pipelineConfig, ITraceLogger traceLogger, ITelemetryDataCollector telemetry)
        {
            await Task.Run(() =>
            {
                _logger = traceLogger;
                _telemetry = telemetry;
                var publisher = new PipelineTestRunPublisher(clientFactory, pipelineConfig, _logger, _telemetry);
                _testRunManager = new TestRunManager(publisher, _logger, _telemetry);
                var parsers = ParserFactory.GetTestResultParsers(_testRunManager, traceLogger, _telemetry);

                _telemetry.AddOrUpdate(TelemetryConstants.ParserCount, parsers.Count());

                foreach (var parser in parsers)
                {
                    // Subscribe parsers to Pub-Sub model
                    Subscribe(parser.Parse);
                }
            });
        }

        /// <inheritdoc />
        public async Task ProcessDataAsync(string data)
        {
            var logData = new LogData
            {
                Line = data,
                LineNumber = ++_counter
            };

            await _broadcast.SendAsync(logData);
        }

        /// <inheritdoc />
        public async Task CompleteAsync()
        {
            try
            {
                _telemetry.AddOrUpdate(TelemetryConstants.TotalLines, _counter);

                _broadcast.Complete();
                Task.WaitAll(_subscribers.Values.Select(x => x.Completion).ToArray());

                using (var timer = new SimpleTimer("TestRunManagerFinalize", _logger, 
                    new TelemetryDataWrapper(_telemetry, TelemetryConstants.TestRunManagerEventArea, TelemetryConstants.FinalizeAsync), 
                    TimeSpan.FromMilliseconds(Int32.MaxValue)))
                {
                    await _testRunManager.FinalizeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to finish the complete operation: {ex}");
            }
        }

        /// <inheritdoc />
        public Guid Subscribe(Action<LogData> handlerAction)
        {
            var handler = new ActionBlock<LogData>(handlerAction);

            _broadcast.LinkTo(handler, new DataflowLinkOptions { PropagateCompletion = true });

            return AddSubscription(handler);
        }

        /// <inheritdoc />
        public void Unsubscribe(Guid subscriptionId)
        {
            if (_subscribers.TryRemove(subscriptionId, out var subscription))
            {
                subscription.Complete();
            }
        }

        private Guid AddSubscription(ITargetBlock<LogData> subscription)
        {
            var subscriptionId = Guid.NewGuid();
            _subscribers.TryAdd(subscriptionId, subscription);
            return subscriptionId;
        }

        private readonly BroadcastBlock<LogData> _broadcast = new BroadcastBlock<LogData>(message => message);
        private readonly ConcurrentDictionary<Guid, ITargetBlock<LogData>> _subscribers = new ConcurrentDictionary<Guid, ITargetBlock<LogData>>();

        private int _counter = 0;
        private ITraceLogger _logger;
        private ITelemetryDataCollector _telemetry;
        private ITestRunManager _testRunManager;
    }
}
