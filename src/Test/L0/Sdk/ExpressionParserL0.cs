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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CreateTree_EnvNamedValue_AcceptedInsideLargerExpression_WhenRegistered()
        {
            // Regression test for https://github.com/actions/runner/issues/2372
            // "env" must be a registrable named value, including when referenced inside
            // a larger expression (ex. jobs.<job_id>.with.<with_id> of reusable workflows).
            var parser = new ExpressionParser();
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("github"),
                new NamedValueInfo<ContextValueNode>("inputs"),
                new NamedValueInfo<ContextValueNode>("vars"),
                new NamedValueInfo<ContextValueNode>("needs"),
                new NamedValueInfo<ContextValueNode>("strategy"),
                new NamedValueInfo<ContextValueNode>("matrix"),
                new NamedValueInfo<ContextValueNode>("env"),
            };

            Assert.NotNull(parser.CreateTree("env.SOMETHING", null, namedValues, null));
            Assert.NotNull(parser.CreateTree("format('value: {0}', env.SOMETHING)", null, namedValues, null));
            Assert.NotNull(parser.CreateTree("env.SOMETHING == '1000' && github.event_name == 'push'", null, namedValues, null));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CreateTree_EnvNamedValue_RejectedWithIssueMessage_WhenNotRegistered()
        {
            // Reproduces the exact error from issue #2372 when the allowed context list
            // does not include "env": "Unrecognized named-value: 'env'. Located at position 1 ..."
            var parser = new ExpressionParser();
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("github"),
                new NamedValueInfo<ContextValueNode>("inputs"),
                new NamedValueInfo<ContextValueNode>("vars"),
                new NamedValueInfo<ContextValueNode>("needs"),
                new NamedValueInfo<ContextValueNode>("strategy"),
                new NamedValueInfo<ContextValueNode>("matrix"),
            };

            var ex = Assert.Throws<ParseException>(() =>
                parser.CreateTree("env.SOMETHING", null, namedValues, null));
            Assert.Contains("Unrecognized named-value: 'env'", ex.Message);
            Assert.Contains("position 1", ex.Message);

            var ex2 = Assert.Throws<ParseException>(() =>
                parser.CreateTree("format('value: {0}', env.SOMETHING)", null, namedValues, null));
            Assert.Contains("Unrecognized named-value: 'env'", ex2.Message);
        }
    }
}
