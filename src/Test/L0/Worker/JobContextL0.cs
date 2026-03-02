using System;
using System.Collections.Generic;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Worker;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public class JobContextL0
    {
        [Fact]
        public void CheckRunId_SetAndGet_WorksCorrectly()
        {
            var ctx = new JobContext();
            ctx.CheckRunId = 12345;
            Assert.Equal(12345, ctx.CheckRunId);
            Assert.True(ctx.TryGetValue("check_run_id", out var value));
            Assert.IsType<NumberContextData>(value);
            Assert.Equal(12345, ((NumberContextData)value).Value);
        }

        [Fact]
        public void CheckRunId_NotSet_ReturnsNull()
        {
            var ctx = new JobContext();
            Assert.Null(ctx.CheckRunId);
            Assert.False(ctx.TryGetValue("check_run_id", out var value));
        }

        [Fact]
        public void CheckRunId_SetNull_RemovesKey()
        {
            var ctx = new JobContext();
            ctx.CheckRunId = 12345;
            ctx.CheckRunId = null;
            Assert.Null(ctx.CheckRunId);
        }

        [Fact]
        public void GetRuntimeEnvironmentVariables_ReturnsCorrectVariables()
        {
            var ctx = new JobContext();
            ctx.CheckRunId = 12345;
            ctx.Status = ActionResult.Success;

            var dict = new Dictionary<string, string>(ctx.GetRuntimeEnvironmentVariables());
            Assert.Equal("12345", dict["JOB_CHECK_RUN_ID"]);
            Assert.Equal("success", dict["JOB_STATUS"]);

            ctx.CheckRunId = null;
            dict = new Dictionary<string, string>(ctx.GetRuntimeEnvironmentVariables());
            Assert.False(dict.ContainsKey("JOB_CHECK_RUN_ID"));
        }
    }
}
