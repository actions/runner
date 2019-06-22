using System;

namespace GitHub.Services.WebApi
{
    /// <summary>Define event log id ranges</summary>
    /// This corresponds with values in Framework\Server\Common\EventLog.cs
    public static class VssEventId
    {
        public static readonly int DefaultEventId                       = 0;

        // Errors
        public static readonly int ExceptionBaseEventId                 = 3000;

        private static readonly int EtmBaseEventId = ExceptionBaseEventId + 1200; // 4200
        public static readonly int VssIdentityServiceException = EtmBaseEventId + 7;
        public static readonly int AccountException = EtmBaseEventId + 36;

        //File Container Service range
        public static readonly int FileContainerBaseEventId = ExceptionBaseEventId + 1700; // 4700 
    }
}
