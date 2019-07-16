using GitHub.Runner.Listener.Configuration;
using GitHub.Runner.Common.Util;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Tests.Listener.Configuration
{
    public class PromptManagerTestsL0
    {
        private readonly string _argName = "SomeArgName";
        private readonly string _description = "Some description";
        private readonly PromptManager _promptManager = new PromptManager();
        private readonly Mock<ITerminal> _terminal = new Mock<ITerminal>();
        private readonly string _unattendedExceptionMessage = "Invalid configuration provided for SomeArgName. Terminating unattended configuration.";

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public void FallsBackToDefault()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                _terminal
                    .Setup(x => x.ReadLine())
                    .Returns(string.Empty);
                _terminal
                    .Setup(x => x.ReadSecret())
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = ReadValue(defaultValue: "Some default value");

                // Assert.
                Assert.Equal("Some default value", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public void FallsBackToDefaultWhenTrimmed()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                _terminal
                    .Setup(x => x.ReadLine())
                    .Returns(" ");
                _terminal
                    .Setup(x => x.ReadSecret())
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = ReadValue(defaultValue: "Some default value");

                // Assert.
                Assert.Equal("Some default value", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public void FallsBackToDefaultWhenUnattended()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                _terminal
                    .Setup(x => x.ReadLine())
                    .Throws<InvalidOperationException>();
                _terminal
                    .Setup(x => x.ReadSecret())
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = ReadValue(
                    defaultValue: "Some default value",
                    unattended: true);

                // Assert.
                Assert.Equal("Some default value", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public void Prompts()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                _terminal
                    .Setup(x => x.ReadLine())
                    .Returns("Some prompt value");
                _terminal
                    .Setup(x => x.ReadSecret())
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = ReadValue();

                // Assert.
                Assert.Equal("Some prompt value", actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public void PromptsAgainWhenEmpty()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var readLineValues = new Queue<string>(new[] { string.Empty, "Some prompt value" });
                _terminal
                    .Setup(x => x.ReadLine())
                    .Returns(() => readLineValues.Dequeue());
                _terminal
                    .Setup(x => x.ReadSecret())
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = ReadValue();

                // Assert.
                Assert.Equal("Some prompt value", actual);
                _terminal.Verify(x => x.ReadLine(), Times.Exactly(2));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public void PromptsAgainWhenFailsValidation()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var readLineValues = new Queue<string>(new[] { "Some invalid prompt value", "Some valid prompt value" });
                _terminal
                    .Setup(x => x.ReadLine())
                    .Returns(() => readLineValues.Dequeue());
                _terminal
                    .Setup(x => x.ReadSecret())
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                // Act.
                string actual = ReadValue(validator: x => x == "Some valid prompt value");

                // Assert.
                Assert.Equal("Some valid prompt value", actual);
                _terminal.Verify(x => x.ReadLine(), Times.Exactly(2));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PromptManager")]
        public void ThrowsWhenUnattended()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                _terminal
                    .Setup(x => x.ReadLine())
                    .Throws<InvalidOperationException>();
                _terminal
                    .Setup(x => x.ReadSecret())
                    .Throws<InvalidOperationException>();
                hc.SetSingleton(_terminal.Object);
                _promptManager.Initialize(hc);

                try
                {
                    // Act.
                    string actual = ReadValue(unattended: true);

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

        private string ReadValue(
            bool secret = false,
            string defaultValue = null,
            Func<string, bool> validator = null,
            bool unattended = false)
        {
            return _promptManager.ReadValue(
                argName: _argName,
                description: _description,
                secret: secret,
                defaultValue: defaultValue,
                validator: validator ?? DefaultValidator,
                unattended: unattended);
        }

        private static bool DefaultValidator(string val)
        {
            return true;
        }
    }
}
