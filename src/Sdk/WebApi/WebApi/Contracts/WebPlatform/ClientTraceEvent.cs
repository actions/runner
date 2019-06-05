using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.WebPlatform
{
    [DataContract]
    public class ClientTraceEvent
    {
        [DataMember]
        public String Area { get; set; }

        [DataMember]
        public String Feature { get; set; }

        [DataMember]
        public Level Level { get; set; }

        [DataMember]
        public String Method { get; set; }

        [DataMember]
        public String Component { get; set; }

        [DataMember]
        public String Message { get; set; }

        [DataMember]
        public String ExceptionType { get; set; }

        [DataMember]
        public Dictionary<String, Object> Properties { get; set; }
    }

    public enum Level
    {
        /**
         * Off: Output no tracing and debugging messages.
         */
        Off = 0,
        /**
         * Error: Output error-handling messages.
         */
        Error = 1,
        /**
         * Warning: Output warnings and error-handling messages.
         */
        Warning = 2,
        /**
         * Info: Output informational messages, warnings, and error-handling messages.
         */
        Info = 3,
        /**
         * Verbose: Output all debugging and tracing messages.
         */
        Verbose = 4
    }
}
