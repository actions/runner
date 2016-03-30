using Microsoft.VisualStudio.Services.Agent.Util;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener
{
    public sealed class CapabilitiesProviderTestL0
    {
        private Mock<IProcessInvoker> _processInvoker;
        private Mock<IWhichUtil> _whichUtil;

        public CapabilitiesProviderTestL0()
        {
            _processInvoker = new Mock<IProcessInvoker>();
            _whichUtil = new Mock<IWhichUtil>();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public async void TestGetCapabilities()
        {
            using (var hc = new TestHostContext(this))
            using (var tokenSource = new CancellationTokenSource())
            {
                //Arrange
                var capProvider = new Agent.Listener.CapabilitiesProvider();
                hc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
                hc.SetSingleton<IWhichUtil>(_whichUtil.Object);
                capProvider.Initialize(hc);

                _whichUtil.Setup(x => x.Which(It.IsAny<string>()))
                    .Returns((string command) =>
                    {
                        bool found = false;
                        foreach (var cap in capProvider.RegularCapabilities)
                        {
                            if (command.Equals(cap.Tool ?? cap.Name))
                            {
                                found = true;
                                break;
                            }
                        }
                        foreach (var cap in capProvider.ToolCapabilities)
                        {
                            if (command.Equals(cap.Command))
                            {
                                found = true;
                                break;
                            }
                        }
                        Assert.True(found);
                        return "capability";
                    });

                _processInvoker.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, tokenSource.Token))
                    .Returns((string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment,
                    CancellationToken cancellationToken) =>
                   {
                       return Task.FromResult<int>(0);
                   });

                //Act
                var caps = await capProvider.GetCapabilitiesAsync("IAmAgent007", tokenSource.Token);

                //Assert
                _whichUtil.Verify(x => x.Which(It.IsAny<string>()),
                    Times.AtLeast(capProvider.RegularCapabilities.Count + capProvider.ToolCapabilities.Count));
                foreach (var cap in capProvider.RegularCapabilities)
                {
                    string value;
                    Assert.True(caps.TryGetValue(cap.Name, out value), $"{cap.Name} is missing");
                    Assert.True(value.Equals("capability"));
                }

                foreach (var cap in capProvider.ToolCapabilities)
                {
                    string value;
                    Assert.True(!caps.TryGetValue(cap.Name, out value)); //tool returns null - therefore no cap
                }
            }
        }
    }
}
