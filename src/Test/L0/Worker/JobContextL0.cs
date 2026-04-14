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

        [Fact]
        public void WorkflowRef_SetAndGet_WorksCorrectly()
        {
            var ctx = new JobContext();
            ctx.WorkflowRef = "owner/repo/.github/workflows/ci.yml@refs/heads/main";
            Assert.Equal("owner/repo/.github/workflows/ci.yml@refs/heads/main", ctx.WorkflowRef);
            Assert.True(ctx.TryGetValue("workflow_ref", out var value));
            Assert.IsType<StringContextData>(value);
        }

        [Fact]
        public void WorkflowRef_NotSet_ReturnsNull()
        {
            var ctx = new JobContext();
            Assert.Null(ctx.WorkflowRef);
        }

        [Fact]
        public void WorkflowRef_SetNull_ClearsValue()
        {
            var ctx = new JobContext();
            ctx.WorkflowRef = "owner/repo/.github/workflows/ci.yml@refs/heads/main";
            ctx.WorkflowRef = null;
            Assert.Null(ctx.WorkflowRef);
        }

        [Fact]
        public void WorkflowSha_SetAndGet_WorksCorrectly()
        {
            var ctx = new JobContext();
            ctx.WorkflowSha = "abc123def456";
            Assert.Equal("abc123def456", ctx.WorkflowSha);
            Assert.True(ctx.TryGetValue("workflow_sha", out var value));
            Assert.IsType<StringContextData>(value);
        }

        [Fact]
        public void WorkflowSha_NotSet_ReturnsNull()
        {
            var ctx = new JobContext();
            Assert.Null(ctx.WorkflowSha);
        }

        [Fact]
        public void WorkflowSha_SetNull_ClearsValue()
        {
            var ctx = new JobContext();
            ctx.WorkflowSha = "abc123def456";
            ctx.WorkflowSha = null;
            Assert.Null(ctx.WorkflowSha);
        }

        [Fact]
        public void WorkflowRepository_SetAndGet_WorksCorrectly()
        {
            var ctx = new JobContext();
            ctx.WorkflowRepository = "owner/repo";
            Assert.Equal("owner/repo", ctx.WorkflowRepository);
            Assert.True(ctx.TryGetValue("workflow_repository", out var value));
            Assert.IsType<StringContextData>(value);
        }

        [Fact]
        public void WorkflowRepository_NotSet_ReturnsNull()
        {
            var ctx = new JobContext();
            Assert.Null(ctx.WorkflowRepository);
        }

        [Fact]
        public void WorkflowRepository_SetNull_ClearsValue()
        {
            var ctx = new JobContext();
            ctx.WorkflowRepository = "owner/repo";
            ctx.WorkflowRepository = null;
            Assert.Null(ctx.WorkflowRepository);
        }

        [Fact]
        public void WorkflowFilePath_SetAndGet_WorksCorrectly()
        {
            var ctx = new JobContext();
            ctx.WorkflowFilePath = ".github/workflows/ci.yml";
            Assert.Equal(".github/workflows/ci.yml", ctx.WorkflowFilePath);
            Assert.True(ctx.TryGetValue("workflow_file_path", out var value));
            Assert.IsType<StringContextData>(value);
        }

        [Fact]
        public void WorkflowFilePath_NotSet_ReturnsNull()
        {
            var ctx = new JobContext();
            Assert.Null(ctx.WorkflowFilePath);
        }

        [Fact]
        public void WorkflowFilePath_SetNull_ClearsValue()
        {
            var ctx = new JobContext();
            ctx.WorkflowFilePath = ".github/workflows/ci.yml";
            ctx.WorkflowFilePath = null;
            Assert.Null(ctx.WorkflowFilePath);
        }
    }
}
