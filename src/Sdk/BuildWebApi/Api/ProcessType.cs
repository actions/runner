using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [GenerateAllConstants]
    public static class ProcessType
    {
        public const Int32 Designer = 1;
        public const Int32 Yaml = 2;
        public const Int32 Docker = 3;
        public const Int32 JustInTime = 4;

        public static String GetName(Int32 type)
        {
            switch (type)
            {
                case ProcessType.Docker:
                    return nameof(Docker);
                case ProcessType.JustInTime:
                    return nameof(JustInTime);
                case ProcessType.Yaml:
                    return nameof(Yaml);
                default:
                    return nameof(Designer);
            }
        }
    }
}
