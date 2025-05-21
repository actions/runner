using System;
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
    }
}
