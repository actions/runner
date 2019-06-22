using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
using GitHub.Services.Content.Common.Tracing;
using GitHub.Services.Common;

namespace GitHub.Services.Content.Common.Telemetry
{
    // shadowed by Content.Common.JsonSerializer
    using JsonSerializer = Microsoft.ApplicationInsights.Extensibility.Implementation.JsonSerializer;
    public class ApplicationInsightsTelemetrySender : ITelemetrySender
    {
        private string instrumentationKey;
        private ITelemetryChannel channel;
        private TelemetryClient client;
        private bool stopping;
        private TimeSpan stopTimeout;

        private int actionsQueued;
        protected readonly ActionBlock<Action> sendQueue;
        protected readonly IAppTraceSource tracer;

        /// <summary>
        /// Composition root constructor for tests
        /// </summary>
        public ApplicationInsightsTelemetrySender(string aiInstrumentationKey, IAppTraceSource tracer, ITelemetryChannel channel, TimeSpan stopTimeout = default(TimeSpan))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(aiInstrumentationKey, nameof(aiInstrumentationKey));
            this.instrumentationKey = aiInstrumentationKey;
            ArgumentUtility.CheckForNull(tracer, nameof(tracer));
            this.tracer = tracer;
            ArgumentUtility.CheckForNull(channel, nameof(channel));
            this.channel = channel;

            if (stopTimeout == default(TimeSpan))
            {
                stopTimeout = ApplicationInsightsTelemetryChannel.DefaultSendTimeout;
            }
            this.stopTimeout = stopTimeout;

            this.sendQueue = NonSwallowingActionBlock.Create<Action>(a => a(), new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 4 });
        }

        /// <summary>
        /// Recommended constructor for general use
        /// </summary>
        public ApplicationInsightsTelemetrySender(string aiInstrumentationKey, IAppTraceSource tracer, TimeSpan sendTimeout = default(TimeSpan), TimeSpan stopTimeout = default(TimeSpan)) : this(aiInstrumentationKey, tracer, new ApplicationInsightsTelemetryChannel(new ApplicationInsightsTransmitter(), sendTimeout), stopTimeout)
        {
        }

        public virtual void SendActionTelemetry(ActionTelemetryRecord actionTelemetry)
        {
            SendTelemetry(actionTelemetry.ActionName, actionTelemetry);
        }

        public virtual void SendErrorTelemetry(ErrorTelemetryRecord errorTelemetry)
        {
            SendTelemetry("Error", errorTelemetry);
        }

        public virtual void SendRecord(TelemetryRecord record)
        {
            var eventName = record.GetType().Name;
            var suffixIndex = eventName.IndexOf(nameof(TelemetryRecord));
            if (suffixIndex >= 0)
            {
                eventName = eventName.Substring(0, suffixIndex);
            }

            SendTelemetry(eventName, record);
        }

        /// <summary>
        /// Updates the Instrumentation Key for the telemetry sender and client.
        /// </summary>
        /// <param name="key">Application Insights Instumentation Key</param>
        protected void UpdateInstrumentationKey(string key)
        {
            instrumentationKey = key;
            if (this.client != null)
            {
                client.InstrumentationKey = key;
            }
        }

        protected void SendTelemetry(string eventName, TelemetryRecord record)
        {
            if (this.client == null)
            {
                throw new InvalidOperationException($"{nameof(SendTelemetry)} called before {nameof(StartSender)}");
            }

            if (this.stopping)
            {
                throw new InvalidOperationException($"{nameof(SendTelemetry)} called after {nameof(StopSender)}");
            }

            if (record.DeploymentEnvironmentIsProduction)
            {
                this.client.Context.Session.Id = record.X_TFS_Session.ToString();
                record.SentUtcNow = DateTime.UtcNow.ToString("o");
                Dictionary<string, string> trimmedRecord = record.GetAssignedProperties();

                Interlocked.Increment(ref actionsQueued);
                sendQueue.PostOrThrow(() =>
                    {
                        // TrackEvent will execute synchronously here because:
                        // 1) When we constructed the TelemetryConfiguration, we specified:
                        // 1.1) an ITelemetryChannel with a synchronous implementation of Send(ITelemetry)
                        // 1.2) the default TelemetryProcessorChain, which only includes a single TransmissionProcessor, which just calls ITelemetryChannel.Send
                        // 2) When we constructed the TelemetryClient, we specified the configuration above
                        try
                        {
                            this.client.TrackEvent(eventName, trimmedRecord);
                            tracer.Verbose($"{nameof(ApplicationInsightsTelemetrySender)} sent {eventName} telemetry");
                        }
                        catch (Exception e)
                        {
                            tracer.Warn(e, $"{nameof(ApplicationInsightsTelemetrySender)} failed to TrackEvent({eventName})");
                        }
                    },
                    CancellationToken.None);
            }
        }

        public virtual void StartSender()
        {
            // Note that we use a configuration instance instead of overwriting the global instance at TelemetryConfiguration.Active
            // which reads from ApplicationInsights.config and may be in use by other code in our AppDomain.
            // Reference source: https://github.com/Microsoft/ApplicationInsights-dotnet/blob/v2.3.0/src/Core/Managed/Net40/Extensibility/Implementation/TelemetryConfigurationFactory.cs

            var config = new TelemetryConfiguration(instrumentationKey, this.channel);
#if DEBUG
            //config.TelemetryChannel.DeveloperMode = true;
#endif
            config.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            config.TelemetryProcessorChainBuilder.Build();

            var diagnostics = new DiagnosticsTelemetryModule();

            // We disable the HeartbeatProvider which is new in v2.9.1 of DiagnosticsTelemetryModule (as compared to v2.3.0)
            // because it will call ArtifactServices.App.Shared.Telemetry.ApplicationInsightsTelemetryChannel.Send every 15m.
            // and our codebase only expects Send calls from ApplicationInsightsTelemetrySender.SendTelemetry (via TrackEvent),
            // which catches all Exceptions and traces them as warnings, including a WebException due to timeout after 15s,
            // because telemetry is best effort and should not cause an error in the process or delay its termination.
            // https://github.com/Microsoft/ApplicationInsights-dotnet/blob/2.9.1/src/Microsoft.ApplicationInsights/Extensibility/Implementation/Tracing/DiagnosticsTelemetryModule.cs

            // This prevents the following Unhandled Exception:
            // System.Net.WebException: The request was aborted: The request was canceled.
            //    at System.Net.HttpWebRequest.EndGetRequestStream(IAsyncResult asyncResult, TransportContext& context)
            //    ...
            //    at Microsoft.ApplicationInsights.Channel.Transmission.<SendAsync>d__36.MoveNext()
            //    at GitHub.Services.ArtifactServices.App.Shared.Telemetry.ApplicationInsightsTransmitter.Send(Uri address, Byte[] content, String contentType, String contentEncoding, TimeSpan timeout)
            //    at GitHub.Services.ArtifactServices.App.Shared.Telemetry.ApplicationInsightsTelemetryChannel.Send(ITelemetry item)
            //    at Microsoft.ApplicationInsights.Extensibility.TelemetrySink.Process(ITelemetry item)
            //    at Microsoft.ApplicationInsights.TelemetryClient.Track(ITelemetry telemetry)
            //    at Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.HeartbeatProvider.HeartbeatPulse(Object state)
            //    ...
            //    at System.Threading.TimerQueue.FireNextTimers()
            diagnostics.IsHeartbeatEnabled = false;

            diagnostics.Initialize(config);

            this.client = new TelemetryClient(config);
            this.client.InstrumentationKey = instrumentationKey;

            tracer.Info($"{nameof(ApplicationInsightsTelemetrySender)} will correlate events with X-TFS-Session {TelemetryRecord.Current_X_TFS_Session.ToString()}");
        }

        public virtual void StopSender()
        {
            this.stopping = true;

            sendQueue.Complete();
            if (actionsQueued > 0)
            {
                tracer.Verbose($"{nameof(ApplicationInsightsTelemetrySender)} waiting for {sendQueue.InputCount} of {actionsQueued} {nameof(client.TrackEvent)} operations to complete...");
            }

            var timer = Stopwatch.StartNew();
            if (sendQueue.Completion.Wait(this.stopTimeout))
            {
                if (actionsQueued > 0)
                {
                    tracer.Verbose($"{nameof(ApplicationInsightsTelemetrySender)} operations completed in {timer.ElapsedMilliseconds} ms");
                }
            }
            else
            {
                tracer.Verbose($"{nameof(ApplicationInsightsTelemetrySender)} timeout on {sendQueue.InputCount} of {actionsQueued} {nameof(client.TrackEvent)} operations after {timer.ElapsedMilliseconds}ms");
            }

            if (actionsQueued > 0)
            {
                tracer.Info($"{nameof(ApplicationInsightsTelemetrySender)} correlated {actionsQueued} events with X-TFS-Session {TelemetryRecord.Current_X_TFS_Session.ToString()}");
            }
            else
            {
                tracer.Info($"{nameof(ApplicationInsightsTelemetrySender)} did not correlate any events with X-TFS-Session {TelemetryRecord.Current_X_TFS_Session.ToString()}");
            }
        }
    }

    /// <summary>
    /// Attribution: http://apmtips.com/blog/2016/11/10/sync-channel/
    /// </summary>
    public sealed class ApplicationInsightsTelemetryChannel : ITelemetryChannel
    {
        internal static readonly TimeSpan DefaultSendTimeout = TimeSpan.FromSeconds(15);

        /// <param name="timeout">
        /// Maximum amount of time we'll wait during Transmission.SendAsync() which calls WebRequest.GetResponseAsync().
        /// The default timeout in AppInsights v2.3.0 is 100s per Transmission.cs.
        /// This is enforced by a Task.Delay continuation which will call WebRequest.Abort() if the GetResponseAsync task hasn't completed yet.
        /// https://github.com/Microsoft/ApplicationInsights-dotnet/blob/v2.3.0/src/Core/Managed/Shared/Channel/Transmission.cs
        /// </param>
        public ApplicationInsightsTelemetryChannel(IApplicationInsightsTransmitter transmitter, TimeSpan timeout)
        {
            ArgumentUtility.CheckForNull(transmitter, nameof(transmitter));

            this.transmitter = transmitter;

            if (timeout == default(TimeSpan))
            {
                timeout = DefaultSendTimeout;
            }

            this.timeout = timeout;
        }

        private Uri endpoint = new Uri("https://dc.services.visualstudio.com/v2/track");
        private IApplicationInsightsTransmitter transmitter;
        private TimeSpan timeout;

        public bool? DeveloperMode { get; set; }

        public string EndpointAddress { get; set; }

        public void Dispose() { }

        public void Flush() { }

        public void Send(ITelemetry item)
        {
            // Omit PII/EUII right before we transmit. We cannot do this earlier because
            // TelemetryClient.TrackEvent (which called us) reinitializes some fields
            // on the ITelemetryItem even if they're null on the client's Context.
            // Example stack where this happens:
            //     \applicationinsights-dotnet\applicationinsights-dotnet\src\core\managed\shared\telemetryclient.cs
            //     telemetry.Context.Cloud.RoleInstance = PlatformSingleton.Current.GetMachineName();
            //     TelemetryClient.Initialize
            //     TelemetryClient.Track
            //     TelemetryClient.TrackEvent

            item.Context.Cloud.RoleInstance = null; // PII
            item.Context.Cloud.RoleName = null; // PII
            item.Context.Device.Id = null; // PII
            item.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            item.Context.Location.Ip = null; // EUII

            // Serialize item and send it
            byte[] json = JsonSerializer.Serialize(new List<ITelemetry>() { item }, true);
            transmitter.Send(this.endpoint, json, "application/x-json-stream", JsonSerializer.CompressionType, this.timeout);
        }
    }

    /// <summary>
    /// Matches Microsoft.ApplicationInsights.Channel.Transmission's constructor signature
    /// </summary>
    public interface IApplicationInsightsTransmitter
    {
        /// <summary>
        /// Matches Microsoft.ApplicationInsights.Channel.Transmission's constructor signature
        /// </summary>
        void Send(Uri address, byte[] content, string contentType, string contentEncoding, TimeSpan timeout);
    }

    public class ApplicationInsightsTransmitter : IApplicationInsightsTransmitter
    {
        public void Send(Uri address, byte[] content, string contentType, string contentEncoding, TimeSpan timeout)
        {
            var transmission = new Transmission(address, content, contentType, contentEncoding, timeout);
            TaskSafety.SyncResultOnThreadPool(() => transmission.SendAsync());
        }
    }
}
