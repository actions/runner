using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener.Configuration
{
    public class PromptManagerTestsL0
    {
        private readonly string _argName = "SomeArgName";
        private readonly string _description = "Some description";
        private readonly PromptManager _promptManager = new PromptManager();
        private readonly Mock<ITerminal> _terminal = new Mock<ITerminal>();
        private readonly string _unattendedExceptionMessage = StringUtil.Loc("InvalidConfigFor0TerminatingUnattended", "SomeArgName");

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public async Task FallsBackToDefault()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                _terminal
                    .Setup(x => x.ReadLineAsync(CancellationToken.None))
                    .Returns(Task.FromResult<string>(string.Empty));
                _terminal
                    .Setup(x => x.ReadSecretAsync(CancellationToken.None))
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = await ReadValue(defaultValue: "Some default value");

                // Assert.
                Assert.Equal("Some default value", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public async Task FallsBackToDefaultWhenTrimmed()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                _terminal
                    .Setup(x => x.ReadLineAsync(CancellationToken.None))
                    .Returns(Task.FromResult<string>(" "));
                _terminal
                    .Setup(x => x.ReadSecretAsync(CancellationToken.None))
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = await ReadValue(defaultValue: "Some default value");

                // Assert.
                Assert.Equal("Some default value", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public async Task FallsBackToDefaultWhenUnattended()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                _terminal
                    .Setup(x => x.ReadLineAsync(CancellationToken.None))
                    .Throws<InvalidOperationException>();
                _terminal
                    .Setup(x => x.ReadSecretAsync(CancellationToken.None))
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = await ReadValue(
                    defaultValue: "Some default value",
                    unattended: true);

                // Assert.
                Assert.Equal("Some default value", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public async Task Prompts()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                _terminal
                    .Setup(x => x.ReadLineAsync(CancellationToken.None))
                    .Returns(Task.FromResult<string>("Some prompt value"));
                _terminal
                    .Setup(x => x.ReadSecretAsync(CancellationToken.None))
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = await ReadValue();

                // Assert.
                Assert.Equal("Some prompt value", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public async Task PromptsAgainWhenEmpty()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var readLineValues = new Queue<string>(new[] { string.Empty, "Some prompt value" });
                _terminal
                    .Setup(x => x.ReadLineAsync(CancellationToken.None))
                    .Returns(() => Task.FromResult<string>(readLineValues.Dequeue()));
                _terminal
                    .Setup(x => x.ReadSecretAsync(CancellationToken.None))
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = await ReadValue();

                // Assert.
                Assert.Equal("Some prompt value", actual);
                _terminal.Verify(x => x.ReadLineAsync(CancellationToken.None), Times.Exactly(2));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public async Task PromptsAgainWhenFailsValidation()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var readLineValues = new Queue<string>(new[] { "Some invalid prompt value", "Some valid prompt value" });
                _terminal
                    .Setup(x => x.ReadLineAsync(CancellationToken.None))
                    .Returns(() => Task.FromResult<string>(readLineValues.Dequeue()));
                _terminal
                    .Setup(x => x.ReadSecretAsync(CancellationToken.None))
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = await ReadValue(validator: x => x == "Some valid prompt value");

                // Assert.
                Assert.Equal("Some valid prompt value", actual);
                _terminal.Verify(x => x.ReadLineAsync(CancellationToken.None), Times.Exactly(2));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public async Task ThrowsWhenUnattended()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                _terminal
                    .Setup(x => x.ReadLineAsync(CancellationToken.None))
                    .Throws<InvalidOperationException>();
                _terminal
                    .Setup(x => x.ReadSecretAsync(CancellationToken.None))
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                try
                {
                    // Act.
                    string actual = await ReadValue(unattended: true);

                    // Assert.
                    throw new InvalidOperationException();
                }
                catch (Exception ex)
                {
                    // Assert.
                    Assert.Equal(_unattendedExceptionMessage, ex.Message);
                }
            }
        }

        private async Task<string> ReadValue(
            bool secret = false,
            string defaultValue = null,
            Func<string, bool> validator = null,
            bool unattended = false)
        {
            return await _promptManager.ReadValue(
                argName: _argName,
                description: _description,
                secret: secret,
                defaultValue: defaultValue,
                validator: validator ?? DefaultValidator,
                unattended: unattended,
                token: CancellationToken.None);
        }

        private static bool DefaultValidator(string val)
        {
            return true;
        }
    }
}