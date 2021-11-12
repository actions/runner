using System;
using System.Net.Http;

namespace GitHub.Services.Common
{
    /// <summary>
    /// This class is used by the message handler, if injected as a request property, to trace additional
    /// timing details for outgoing requests. This information is added to the HttpOutgoingRequest logs
    /// </summary>
    public class VssHttpMessageHandlerTraceInfo
    {
        DateTime _lastTime;

        private static readonly HttpRequestOptionsKey<VssHttpMessageHandlerTraceInfo> TfsTraceInfoKey = new HttpRequestOptionsKey<VssHttpMessageHandlerTraceInfo>("TFS_TraceInfo");

        public int TokenRetries { get; internal set; }

        public TimeSpan HandlerStartTime { get; private set; }
        public TimeSpan BufferedRequestTime { get; private set; }
        public TimeSpan RequestSendTime { get; private set; }
        public TimeSpan ResponseContentTime { get; private set; }
        public TimeSpan GetTokenTime { get; private set; }
        public TimeSpan TrailingTime { get; private set; }

        public VssHttpMessageHandlerTraceInfo()
        {
            _lastTime = DateTime.UtcNow;
        }

        internal void TraceHandlerStartTime()
        {
            var previous = _lastTime;
            _lastTime = DateTime.UtcNow;
            HandlerStartTime += (_lastTime - previous);
        }

        internal void TraceBufferedRequestTime()
        {
            var previous = _lastTime;
            _lastTime = DateTime.UtcNow;
            BufferedRequestTime += (_lastTime - previous);
        }

        internal void TraceRequestSendTime()
        {
            var previous = _lastTime;
            _lastTime = DateTime.UtcNow;
            RequestSendTime += (_lastTime - previous);
        }

        internal void TraceResponseContentTime()
        {
            var previous = _lastTime;
            _lastTime = DateTime.UtcNow;
            ResponseContentTime += (_lastTime - previous);
        }

        internal void TraceGetTokenTime()
        {
            var previous = _lastTime;
            _lastTime = DateTime.UtcNow;
            GetTokenTime += (_lastTime - previous);
        }

        internal void TraceTrailingTime()
        {
            var previous = _lastTime;
            _lastTime = DateTime.UtcNow;
            TrailingTime += (_lastTime - previous);
        }

        /// <summary>
        /// Set the provided traceInfo as a property on a request message (if not already set)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="traceInfo"></param>
        public static void SetTraceInfo(HttpRequestMessage message, VssHttpMessageHandlerTraceInfo traceInfo)
        {
            if (!message.Options.TryGetValue(TfsTraceInfoKey, out var _))
            {
                message.Options.Set(TfsTraceInfoKey, traceInfo);
            }
        }

        /// <summary>
        /// Get VssHttpMessageHandlerTraceInfo from request message, or return null if none found
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static VssHttpMessageHandlerTraceInfo GetTraceInfo(HttpRequestMessage message)
        {
            VssHttpMessageHandlerTraceInfo traceInfo;
            message.Options.TryGetValue(TfsTraceInfoKey, out traceInfo);
            return traceInfo;
        }

        public override string ToString()
        {
            return $"R:{TokenRetries}, HS:{HandlerStartTime.Ticks}, BR:{BufferedRequestTime.Ticks}, RS:{RequestSendTime.Ticks}, RC:{ResponseContentTime.Ticks}, GT:{GetTokenTime.Ticks}, TT={TrailingTime.Ticks}";
        }
    }

}
