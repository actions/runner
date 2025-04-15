using System;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Worker;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public class JobContextL0
    {
        [Fact]
        public void CheckRunID_SetAndGet_WorksCorrectly()
        {
            var ctx = new JobContext();
            ctx.CheckRunID = 12345;
            Assert.Equal(12345, ctx.CheckRunID);
            Assert.True(ctx.TryGetValue("check_run_id", out var value));
            Assert.IsType<NumberContextData>(value);
            Assert.Equal(12345, ((NumberContextData)value).Value);
        }

        [Fact]
        public void CheckRunID_NotSet_ReturnsNull()
        {
            var ctx = new JobContext();
            Assert.Null(ctx.CheckRunID);
            Assert.False(ctx.TryGetValue("check_run_id", out var value));
        }

        [Fact]
        public void CheckRunID_SetNull_RemovesKey()
        {
            var ctx = new JobContext();
            ctx.CheckRunID = 12345;
            ctx.CheckRunID = null;
            Assert.Null(ctx.CheckRunID);
        }
    }
}
