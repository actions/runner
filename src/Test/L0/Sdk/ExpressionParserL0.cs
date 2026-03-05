using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.ObjectTemplating;
using System;
using System.Collections.Generic;
using Xunit;

namespace GitHub.Runner.Common.Tests.Sdk
{
    /// <summary>
    /// Regression tests for ExpressionParser.CreateTree to verify that
    /// allowCaseFunction does not accidentally set allowUnknownKeywords.
    /// </summary>
    public sealed class ExpressionParserL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CreateTree_RejectsUnrecognizedNamedValue()
        {
            // Regression: allowCaseFunction was passed positionally into
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
        public void CreateTree_CaseFunctionWorks_WhenAllowed()
        {
            var parser = new ExpressionParser();
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("github"),
            };

            var node = parser.CreateTree("case(github.event_name, 'push', 'Push Event')", null, namedValues, null, allowCaseFunction: true);

            Assert.NotNull(node);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CreateTree_CaseFunctionRejected_WhenDisallowed()
        {
            var parser = new ExpressionParser();
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("github"),
            };

            var ex = Assert.Throws<ParseException>(() =>
                parser.CreateTree("case(github.event_name, 'push', 'Push Event')", null, namedValues, null, allowCaseFunction: false));

            Assert.Contains("Unrecognized function", ex.Message);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CreateTree_CaseFunctionDoesNotAffectUnknownKeywords()
        {
            // The key regression test: with allowCaseFunction=true (default),
            // unrecognized named values must still be rejected.
            var parser = new ExpressionParser();
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("inputs"),
            };

            var ex = Assert.Throws<ParseException>(() =>
                parser.CreateTree("github.ref", null, namedValues, null, allowCaseFunction: true));

            Assert.Contains("Unrecognized named-value", ex.Message);
        }
    }
}
