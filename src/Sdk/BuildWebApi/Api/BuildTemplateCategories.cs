using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [GenerateAllConstants]
    public static class BuildTemplateCategories
    {
        public static readonly String All = "All";
        public static readonly String Build = "Build";
        public static readonly String Utility = "Utility";
        public static readonly String Test = "Test";
        public static readonly String Package = "Package";
        public static readonly String Deploy = "Deploy";
        public static readonly String Tool = "Tool";
        public static readonly String Custom = "Custom";

        public static readonly String[] AllCategories = new String[] {
            All,
            Build,
            Utility,
            Test,
            Package,
            Deploy,
            Tool,
            Custom
        };
    }
}
