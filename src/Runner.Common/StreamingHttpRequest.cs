using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common
{
    public abstract class StreamingHttpRequest<T>
    {
        private readonly Task _task;
        protected readonly Channel<T> _channel;
        protected readonly StreamingHttpClient _client;

        public StreamingHttpRequest(StreamingHttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
            });
            _task = Task.Run(StreamDataAsync);
        }

        public StreamingHttpClient Client => _client;

        public ChannelWriter<T> Writer => _channel.Writer;

        protected abstract Task StreamDataAsync();

        public async Task DrainAsync()
        {
            _channel.Writer.TryComplete();
            try
            {
                await _task;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    public class StreamingFeedRequest : StreamingHttpRequest<TimelineRecordFeedLinesWrapper>
    {
        private readonly Guid _scopeIdentifier;
        private readonly string _hubName;
        private readonly Guid _planId;
        private readonly Guid _timelineId;
        private readonly Guid _timelineRecordId;
        private readonly Tracing _trace;
        private readonly TimeSpan _maxRequestDuration;

        public StreamingFeedRequest(
            Guid scopeIdentifier,
            string hubName,
            Guid planId,
            Guid timelineId,
            Guid timelineRecordId,
            StreamingHttpClient client,
            TimeSpan maxRequestDuration,
            Tracing trace)
            : base(client)
        {
            _scopeIdentifier = scopeIdentifier;
            _hubName = hubName;
            _planId = planId;
            _timelineId = timelineId;
            _timelineRecordId = timelineRecordId;
            _trace = trace;
            _maxRequestDuration = maxRequestDuration;
        }

        protected override async Task StreamDataAsync()
        {
            while (!_channel.Reader.Completion.IsCompleted)
            {
                using CancellationTokenSource timeoutSource = new CancellationTokenSource(_maxRequestDuration);
                try
                {
                    await Client.StreamTimelineRecordFeedAsync(_scopeIdentifier, _hubName, _planId, _timelineId, _timelineRecordId, _channel.Reader, _trace, cancellationToken: timeoutSource.Token);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == timeoutSource.Token)
                {
                    _trace.Info("Max request duration of {0} reached. Stopping stream and reinitializing a new request.", _maxRequestDuration);
                }
                catch (Exception ex)
                {
                    _trace.Error(ex);
                }
            }

            _trace.Info("StreamTimelineRecordFeedAsync completed");
        }
    }
}
