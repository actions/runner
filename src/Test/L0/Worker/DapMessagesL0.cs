using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using GitHub.Runner.Worker.Dap;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class DapMessagesL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RequestSerializesCorrectly()
        {
            var request = new Request
            {
                Seq = 1,
                Type = "request",
                Command = "initialize",
                Arguments = JObject.FromObject(new { clientID = "test-client" })
            };

            var json = JsonConvert.SerializeObject(request);
            var deserialized = JsonConvert.DeserializeObject<Request>(json);

            Assert.Equal(1, deserialized.Seq);
            Assert.Equal("request", deserialized.Type);
            Assert.Equal("initialize", deserialized.Command);
            Assert.Equal("test-client", deserialized.Arguments["clientID"].ToString());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResponseSerializesCorrectly()
        {
            var response = new Response
            {
                Seq = 2,
                Type = "response",
                RequestSeq = 1,
                Success = true,
                Command = "initialize",
                Body = new Capabilities { SupportsConfigurationDoneRequest = true }
            };

            var json = JsonConvert.SerializeObject(response);
            var deserialized = JsonConvert.DeserializeObject<Response>(json);

            Assert.Equal(2, deserialized.Seq);
            Assert.Equal("response", deserialized.Type);
            Assert.Equal(1, deserialized.RequestSeq);
            Assert.True(deserialized.Success);
            Assert.Equal("initialize", deserialized.Command);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EventSerializesWithCorrectType()
        {
            var evt = new Event
            {
                EventType = "stopped",
                Body = new StoppedEventBody
                {
                    Reason = "entry",
                    Description = "Stopped at entry",
                    ThreadId = 1,
                    AllThreadsStopped = true
                }
            };

            Assert.Equal("event", evt.Type);

            var json = JsonConvert.SerializeObject(evt);
            Assert.Contains("\"type\":\"event\"", json);
            Assert.Contains("\"event\":\"stopped\"", json);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StoppedEventBodyOmitsNullFields()
        {
            var body = new StoppedEventBody
            {
                Reason = "step"
            };

            var json = JsonConvert.SerializeObject(body);
            Assert.Contains("\"reason\":\"step\"", json);
            Assert.DoesNotContain("\"threadId\"", json);
            Assert.DoesNotContain("\"allThreadsStopped\"", json);
            Assert.DoesNotContain("\"description\"", json);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CapabilitiesMvpDefaults()
        {
            var caps = new Capabilities
            {
                SupportsConfigurationDoneRequest = true,
                SupportsFunctionBreakpoints = false,
                SupportsStepBack = false
            };

            var json = JsonConvert.SerializeObject(caps);
            var deserialized = JsonConvert.DeserializeObject<Capabilities>(json);

            Assert.True(deserialized.SupportsConfigurationDoneRequest);
            Assert.False(deserialized.SupportsFunctionBreakpoints);
            Assert.False(deserialized.SupportsStepBack);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ContinueResponseBodySerialization()
        {
            var body = new ContinueResponseBody { AllThreadsContinued = true };
            var json = JsonConvert.SerializeObject(body);
            var deserialized = JsonConvert.DeserializeObject<ContinueResponseBody>(json);

            Assert.True(deserialized.AllThreadsContinued);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ThreadsResponseBodySerialization()
        {
            var body = new ThreadsResponseBody
            {
                Threads = new List<Thread>
                {
                    new Thread { Id = 1, Name = "Job Thread" }
                }
            };

            var json = JsonConvert.SerializeObject(body);
            var deserialized = JsonConvert.DeserializeObject<ThreadsResponseBody>(json);

            Assert.Single(deserialized.Threads);
            Assert.Equal(1, deserialized.Threads[0].Id);
            Assert.Equal("Job Thread", deserialized.Threads[0].Name);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void StackFrameSerialization()
        {
            var frame = new StackFrame
            {
                Id = 1,
                Name = "Step: Checkout",
                Line = 1,
                Column = 1,
                PresentationHint = "normal"
            };

            var json = JsonConvert.SerializeObject(frame);
            var deserialized = JsonConvert.DeserializeObject<StackFrame>(json);

            Assert.Equal(1, deserialized.Id);
            Assert.Equal("Step: Checkout", deserialized.Name);
            Assert.Equal("normal", deserialized.PresentationHint);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExitedEventBodySerialization()
        {
            var body = new ExitedEventBody { ExitCode = 130 };
            var json = JsonConvert.SerializeObject(body);
            var deserialized = JsonConvert.DeserializeObject<ExitedEventBody>(json);

            Assert.Equal(130, deserialized.ExitCode);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DapCommandEnumValues()
        {
            Assert.Equal(0, (int)DapCommand.Continue);
            Assert.Equal(1, (int)DapCommand.Next);
            Assert.Equal(4, (int)DapCommand.Disconnect);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RequestDeserializesFromRawJson()
        {
            var json = @"{""seq"":5,""type"":""request"",""command"":""continue"",""arguments"":{""threadId"":1}}";
            var request = JsonConvert.DeserializeObject<Request>(json);

            Assert.Equal(5, request.Seq);
            Assert.Equal("request", request.Type);
            Assert.Equal("continue", request.Command);
            Assert.Equal(1, request.Arguments["threadId"].Value<int>());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ErrorResponseBodySerialization()
        {
            var body = new ErrorResponseBody
            {
                Error = new Message
                {
                    Id = 1,
                    Format = "Something went wrong",
                    ShowUser = true
                }
            };

            var json = JsonConvert.SerializeObject(body);
            var deserialized = JsonConvert.DeserializeObject<ErrorResponseBody>(json);

            Assert.Equal(1, deserialized.Error.Id);
            Assert.Equal("Something went wrong", deserialized.Error.Format);
            Assert.True(deserialized.Error.ShowUser);
        }
    }
}
