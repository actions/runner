using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.ObjectTemplating;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace GitHub.Runner.Common.Tests.Sdk
{
    /// <summary>
    /// Regression tests for ExpressionParser.CreateTree to verify that
    /// the case function does not accidentally set allowUnknownKeywords.
    /// </summary>
    public sealed class ExpressionParserL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CreateTree_RejectsUnrecognizedNamedValue()
        {
            // Regression: the case function parameter was passed positionally into
            // the allowUnknownKeywords parameter, causing all named values
            // to be silently accepted.
            var parser = new ExpressionParser();
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("inputs"),
            };

            var ex = Assert.Throws<ParseException>(() =>
                parser.CreateTree("github.event.repository.private", null, namedValues, null));

            Assert.Contains("Unrecognized named-value", ex.Message);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CreateTree_AcceptsRecognizedNamedValue()
        {
            var parser = new ExpressionParser();
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("inputs"),
            };

            var node = parser.CreateTree("inputs.foo", null, namedValues, null);

            Assert.NotNull(node);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CreateTree_CaseFunctionWorks()
        {
            var parser = new ExpressionParser();
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("github"),
            };

            var node = parser.CreateTree("case(github.event_name, 'push', 'Push Event')", null, namedValues, null);

            Assert.NotNull(node);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CreateTree_CaseFunctionDoesNotAffectUnknownKeywords()
        {
            // The key regression test: unrecognized named values must still be rejected.
            var parser = new ExpressionParser();
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("inputs"),
            };

            var ex = Assert.Throws<ParseException>(() =>
                parser.CreateTree("github.ref", null, namedValues, null));

            Assert.Contains("Unrecognized named-value", ex.Message);
        }

        [Theory]
        [InlineData("'0x1F' == 31", true)]
        [InlineData("'0X1F' == 31", true)]
        [InlineData("'0o17' == 15", true)]
        [InlineData("'0O17' == 15", true)]
        [InlineData("'0b101' == 5", true)]
        [InlineData("'0B101' == 5", true)]
        [InlineData("'0b102' == 0", false)]
        [InlineData("'0b' == 0", false)]
        [InlineData("'0X1G' == 0", false)]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Evaluate_StringToNumberCoercionMatchesJavascriptNumber(string expression, bool expected)
        {
            var parser = new ExpressionParser();
            var node = parser.CreateTree(expression, null, new List<INamedValueInfo>(), null);

            var result = node.Evaluate(null, null, null, null);

            Assert.Equal(expected, result.IsTruthy);
        }
    }
}
