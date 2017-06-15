using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Telemetry;
using Microsoft.VisualStudio.Services.WebPlatform;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Telemetry
{
    public class TelemetryCommandExtensionTests
    {
        private Mock<IExecutionContext> _ec;
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private Mock<ICustomerIntelligenceServer> _mockCiService;
        private Mock<IAsyncCommandContext> _mockCommandContext;
        private TestHostContext _hc;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Telemetry")]
        public void PublishTelemetryCommandWithCiProps()
        {
            SetupMocks();
            var publishTelemetryCmd = new TelemetryCommandExtension();
            publishTelemetryCmd.Initialize(_hc);

            var cmd = new Command("telemetry", "publish");
            cmd.Data = "key1=value1;key2=value2";
            cmd.Properties.Add("area", "Test");
            cmd.Properties.Add("feature", "Task");

            publishTelemetryCmd.ProcessCommand(_ec.Object, cmd);
            _mockCiService.Verify(s => s.PublishEventsAsync(It.Is<CustomerIntelligenceEvent[]>(
                e => e.Length == 1 && e[0].Properties.Count == 2 && e[0].Area == "Test" && e[0].Feature == "Task")), Times.Once());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Telemetry")]
        public void PublishTelemetryWithoutArea()
        {
            SetupMocks();
            var publishTelemetryCmd = new TelemetryCommandExtension();
            publishTelemetryCmd.Initialize(_hc);

            var cmd = new Command("telemetry", "publish");
            cmd.Data = "key1=value1;key2=value2";
            cmd.Properties.Add("feature", "Task");

            Assert.Throws<ArgumentException>(() => publishTelemetryCmd.ProcessCommand(_ec.Object, cmd));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Telemetry")]
        public void PublishTelemetryWithoutFeature()
        {
            SetupMocks();
            var publishTelemetryCmd = new TelemetryCommandExtension();
            publishTelemetryCmd.Initialize(_hc);

            var cmd = new Command("telemetry", "publish");
            cmd.Data = "key1=value1;key2=value2";
            cmd.Properties.Add("area", "Test");

            Assert.Throws<ArgumentException>(() => publishTelemetryCmd.ProcessCommand(_ec.Object, cmd));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Telemetry")]
        public void PublishTelemetryWithoutCiData()
        {
            SetupMocks();
            var publishTelemetryCmd = new TelemetryCommandExtension();
            publishTelemetryCmd.Initialize(_hc);

            var cmd = new Command("telemetry", "publish");
            cmd.Properties.Add("area", "Test");
            cmd.Properties.Add("feature", "Task");

            Assert.Throws<ArgumentException>(() => publishTelemetryCmd.ProcessCommand(_ec.Object, cmd));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Telemetry")]
        public void PublishTelemetryWithoutCommandEvent()
        {
            SetupMocks();
            var publishTelemetryCmd = new TelemetryCommandExtension();
            publishTelemetryCmd.Initialize(_hc);

            var cmd = new Command("telemetry", "abcxyz");
            cmd.Properties.Add("area", "Test");
            cmd.Properties.Add("feature", "Task");

            var ex = Assert.Throws<Exception>(() => publishTelemetryCmd.ProcessCommand(_ec.Object, cmd));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Telemetry")]
        public void PublishTelemetryCiDataWithEqualInPropValue()
        {
            SetupMocks();
            var publishTelemetryCmd = new TelemetryCommandExtension();
            publishTelemetryCmd.Initialize(_hc);

            var cmd = new Command("telemetry", "publish");
            cmd.Properties.Add("area", "Test");
            cmd.Properties.Add("feature", "Task");
            cmd.Data = "key1=value1=value2";

            publishTelemetryCmd.ProcessCommand(_ec.Object, cmd);
            _mockCiService.Verify(s => s.PublishEventsAsync(It.Is<CustomerIntelligenceEvent[]>(e => VerifyEvent(e, "key1", "value1=value2"))), Times.Once());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Telemetry")]
        public void PublishTelemetryCiDataWithEscapeKey()
        {
            SetupMocks();
            var publishTelemetryCmd = new TelemetryCommandExtension();
            publishTelemetryCmd.Initialize(_hc);

            var cmd = new Command("telemetry", "publish");
            cmd.Properties.Add("area", "Test");
            cmd.Properties.Add("feature", "Task");
            cmd.Data = @"col\;key=value1";

            publishTelemetryCmd.ProcessCommand(_ec.Object, cmd);
            _mockCiService.Verify(s => s.PublishEventsAsync(It.Is<CustomerIntelligenceEvent[]>(e => VerifyEvent(e, "col;key", "value1"))), Times.Once());
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            _hc = new TestHostContext(this, name);

            _mockCiService = new Mock<ICustomerIntelligenceServer>();
            _hc.SetSingleton(_mockCiService.Object);

            _mockCommandContext = new Mock<IAsyncCommandContext>();
            _hc.EnqueueInstance(_mockCommandContext.Object);

            var endpointAuthorization = new EndpointAuthorization()
            {
                Scheme = EndpointAuthorizationSchemes.OAuth
            };
            List<string> warnings;
            var variables = new Variables(_hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
            endpointAuthorization.Parameters[EndpointAuthorizationParameters.AccessToken] = "accesstoken";

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Endpoints).Returns(new List<ServiceEndpoint> { new ServiceEndpoint { Url = new Uri("http://dummyurl"), Name = ServiceEndpoints.SystemVssConnection, Authorization = endpointAuthorization } });
            _ec.Setup(x => x.Variables).Returns(variables);
            var asyncCommands = new List<IAsyncCommandContext>();
            _ec.Setup(x => x.AsyncCommands).Returns(asyncCommands);
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>()))
            .Callback<Issue>
            ((issue) =>
            {
                if (issue.Type == IssueType.Warning)
                {
                    _warnings.Add(issue.Message);
                }
                else if (issue.Type == IssueType.Error)
                {
                    _errors.Add(issue.Message);
                }
            });
        }

        private bool VerifyEvent(CustomerIntelligenceEvent[] ciEvent, string expectedKey, string expectedValue)
        {
            object value;
            Assert.True(ciEvent.Length == 1);
            Assert.True(ciEvent[0].Properties.Count == 1);
            Assert.True(ciEvent[0].Properties.TryGetValue(expectedKey, out value));
            Assert.True(ciEvent[0].Area == "Test" && ciEvent[0].Feature == "Task");
            Assert.True(value.ToString().Equals(expectedValue));
            return true;
        }
    }
}
