using System;
using System.Collections.Generic;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using Xunit;

namespace GitHub.Runner.Common.Tests.Util
{
    public class NodeUtilL0
    {
        // We're testing the logic with feature flags
        [Theory]
        [InlineData(false, false, false, false, "node20", false)] // Phase 1: No env vars
        [InlineData(false, false, false, true, "node20", false)]  // Phase 1: Allow unsecure (redundant)
        [InlineData(false, false, true, false, "node24", false)]  // Phase 1: Force node24
        [InlineData(false, false, true, true, "node20", true)]    // Phase 1: Both flags (use phase default + warning)
        [InlineData(false, true, false, false, "node24", false)]  // Phase 2: No env vars
        [InlineData(false, true, false, true, "node20", false)]   // Phase 2: Allow unsecure
        [InlineData(false, true, true, false, "node24", false)]   // Phase 2: Force node24 (redundant)
        [InlineData(false, true, true, true, "node24", true)]     // Phase 2: Both flags (use phase default + warning)
        [InlineData(true, false, false, false, "node24", false)]  // Phase 3: Always Node 24 regardless of env vars
        [InlineData(true, false, false, true, "node24", false)]   // Phase 3: Always Node 24 regardless of env vars
        [InlineData(true, false, true, false, "node24", false)]   // Phase 3: Always Node 24 regardless of env vars
        [InlineData(true, false, true, true, "node24", true)]     // Phase 3: Always Node 24 regardless of env vars + warning
        public void TestNodeVersionLogic(bool requireNode24, bool useNode24ByDefault, bool forceNode24, bool allowUnsecureNode, string expectedVersion, bool expectWarning)
        {
            try
            {
                Environment.SetEnvironmentVariable(Constants.Runner.NodeMigration.ForceNode24Variable, forceNode24 ? "true" : null);
                Environment.SetEnvironmentVariable(Constants.Runner.NodeMigration.AllowUnsecureNodeVersionVariable, allowUnsecureNode ? "true" : null);

                // Call the actual method
                var (actualVersion, warningMessage) = NodeUtil.DetermineActionsNodeVersion(null, useNode24ByDefault, requireNode24);
                
                // Assert
                Assert.Equal(expectedVersion, actualVersion);
                
                if (expectWarning)
                {
                    Assert.NotNull(warningMessage);
                    Assert.Contains("Both", warningMessage);
                    Assert.Contains("are set to true", warningMessage);
                }
                else
                {
                    Assert.Null(warningMessage);
                }
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(Constants.Runner.NodeMigration.ForceNode24Variable, null);
                Environment.SetEnvironmentVariable(Constants.Runner.NodeMigration.AllowUnsecureNodeVersionVariable, null);
            }
        }
        
        [Theory]
        [InlineData(false, false, false, false, false, true, "node20", false)]   // Phase 1: System env: none, Workflow env: allow=true
        [InlineData(false, false, true, false, false, false, "node24", false)]   // Phase 1: System env: force node24, Workflow env: none
        [InlineData(false, true, false, false, true, false, "node24", false)]    // Phase 1: System env: none, Workflow env: force node24
        [InlineData(false, true, false, false, false, true, "node20", false)]    // Phase 1: System env: none, Workflow env: allow unsecure
        [InlineData(false, false, false, false, true, true, "node20", true)]     // Phase 1: System env: none, Workflow env: both (phase default + warning)
        [InlineData(true, false, false, false, false, false, "node24", false)]   // Phase 2: System env: none, Workflow env: none
        [InlineData(true, false, false, true, false, false, "node24", false)]    // Phase 2: System env: force node24, Workflow env: none
        [InlineData(true, false, false, false, false, true, "node20", false)]    // Phase 2: System env: none, Workflow env: allow unsecure
        [InlineData(true, false, true, false, false, true, "node20", false)]     // Phase 2: System env: force node24, Workflow env: allow unsecure
        [InlineData(true, false, false, false, true, true, "node24", true)]      // Phase 2: System env: none, Workflow env: both (phase default + warning)
        public void TestNodeVersionLogicWithWorkflowEnvironment(bool useNode24ByDefault, bool requireNode24,
            bool systemForceNode24, bool systemAllowUnsecure,
            bool workflowForceNode24, bool workflowAllowUnsecure, 
            string expectedVersion, bool expectWarning)
        {
            try
            {
                // Set system environment variables
                Environment.SetEnvironmentVariable(Constants.Runner.NodeMigration.ForceNode24Variable, systemForceNode24 ? "true" : null);
                Environment.SetEnvironmentVariable(Constants.Runner.NodeMigration.AllowUnsecureNodeVersionVariable, systemAllowUnsecure ? "true" : null);
                
                // Set workflow environment variables
                var workflowEnv = new Dictionary<string, string>();
                if (workflowForceNode24)
                {
                    workflowEnv[Constants.Runner.NodeMigration.ForceNode24Variable] = "true";
                }
                if (workflowAllowUnsecure)
                {
                    workflowEnv[Constants.Runner.NodeMigration.AllowUnsecureNodeVersionVariable] = "true";
                }
                
                // Call the actual method with our test parameters
                var (actualVersion, warningMessage) = NodeUtil.DetermineActionsNodeVersion(workflowEnv, useNode24ByDefault, requireNode24);
                
                // Assert
                Assert.Equal(expectedVersion, actualVersion);
                
                if (expectWarning)
                {
                    Assert.NotNull(warningMessage);
                    Assert.Contains("Both", warningMessage);
                    Assert.Contains("are set to true", warningMessage);
                }
                else
                {
                    Assert.Null(warningMessage);
                }
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(Constants.Runner.NodeMigration.ForceNode24Variable, null);
                Environment.SetEnvironmentVariable(Constants.Runner.NodeMigration.AllowUnsecureNodeVersionVariable, null);
            }
        }
    }
}
