using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Agent.Plugins.Log.TestResultParser.Plugin;

namespace Agent.Plugins.TestResultParser.Plugin
{
    public class LogParserGateway : ILogParserGateway, IBus<LogData>
    {
        /// <inheritdoc />
        public async Task InitializeAsync(IClientFactory clientFactory, IPipelineConfig pipelineConfig, ITraceLogger traceLogger)
        {
            await Task.Run(() =>
            {
                _logger = traceLogger;
                var publisher = new PipelineTestRunPublisher(clientFactory, pipelineConfig);
                var telemetry = new TelemetryDataCollector(clientFactory);
                _testRunManager = new TestRunManager(publisher, _logger);
                var parsers = ParserFactory.GetTestResultParsers(_testRunManager, traceLogger, telemetry);

                foreach (var parser in parsers)
                {
                    //Subscribe parsers to Pub-Sub model
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
                _broadcast.Complete();
                Task.WaitAll(_subscribers.Values.Select(x => x.Completion).ToArray());
                await _testRunManager.FinalizeAsync();
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to finish the complete operation: {ex.StackTrace}");
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
        private int _counter;
        private ITraceLogger _logger;
        private ITestRunManager _testRunManager;
    }
}
