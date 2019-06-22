using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public class ServicePointConstants
    {
        public static readonly int DefaultConnectionLimit1000 = 1000;
        public static readonly int DefaultConnectionLimitPerProc32 = 32;
        public static readonly int DefaultConnectionLimitPerProc4 = 4;
        public static readonly int MaxConnectionsPerProc8 = 8;
        public static readonly int MaxConnectionsPerProc16 = 16;
        public static readonly int MaxConnectionsPerProc32 = 32;
        public static readonly int MaxConnectionsPerProc64 = 64;
    }
}
