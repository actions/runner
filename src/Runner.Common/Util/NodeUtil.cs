﻿using System;
using System.Collections.ObjectModel;

namespace GitHub.Runner.Common.Util
{
    public static class NodeUtil
    {
        private const string _defaultNodeVersion = "node16";

#if OS_OSX && ARM64
        public static readonly ReadOnlyCollection<string> BuiltInNodeVersions = new(new[] { "node16" });
#else
        public static readonly ReadOnlyCollection<string> BuiltInNodeVersions = new(new[] { "node12", "node16" });
#endif

        public static string GetInternalNodeVersion()
        {
            var forcedInternalNodeVersion = Environment.GetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion);
            var forcedNodeVersion = Environment.GetEnvironmentVariable(Constants.Variables.Agent.ForcedActionsNodeVersion);

            return !string.IsNullOrEmpty(forcedNodeVersion) && BuiltInNodeVersions.Contains(forcedNodeVersion) 
                ? forcedNodeVersion
                : !string.IsNullOrEmpty(forcedInternalNodeVersion) && BuiltInNodeVersions.Contains(forcedInternalNodeVersion) 
                    ? forcedInternalNodeVersion 
                    : _defaultNodeVersion;
        }
    }
}
