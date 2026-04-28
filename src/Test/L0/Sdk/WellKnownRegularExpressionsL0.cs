using GitHub.DistributedTask.Pipelines.Expressions;
using Xunit;

namespace GitHub.Runner.Common.Tests.Sdk
{
    public sealed class WellKnownRegularExpressionsL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void SHA1_Key_Returns_CommitHash_Regex()
        {
            var regex = WellKnownRegularExpressions.GetRegex(WellKnownRegularExpressions.SHA1);

            Assert.NotNull(regex);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void CommitHash_Key_Returns_CommitHash_Regex()
        {
            var regex = WellKnownRegularExpressions.GetRegex(WellKnownRegularExpressions.CommitHash);

            Assert.NotNull(regex);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void SHA1_And_CommitHash_Return_Same_Regex()
        {
            var sha1Regex = WellKnownRegularExpressions.GetRegex(WellKnownRegularExpressions.SHA1);
            var commitHashRegex = WellKnownRegularExpressions.GetRegex(WellKnownRegularExpressions.CommitHash);

            Assert.Same(sha1Regex, commitHashRegex);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Matches_40_Char_Hex()
        {
            var regex = WellKnownRegularExpressions.GetRegex(WellKnownRegularExpressions.CommitHash);

            Assert.Matches(regex.Value, new string('a', 40));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Matches_64_Char_Hex()
        {
            var regex = WellKnownRegularExpressions.GetRegex(WellKnownRegularExpressions.CommitHash);

            Assert.Matches(regex.Value, new string('a', 64));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Does_Not_Match_63_Char_Hex()
        {
            var regex = WellKnownRegularExpressions.GetRegex(WellKnownRegularExpressions.CommitHash);

            Assert.DoesNotMatch(regex.Value, new string('a', 63));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Does_Not_Match_65_Char_Hex()
        {
            var regex = WellKnownRegularExpressions.GetRegex(WellKnownRegularExpressions.CommitHash);

            Assert.DoesNotMatch(regex.Value, new string('a', 65));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Matches_Mixed_Case_64_Char()
        {
            var regex = WellKnownRegularExpressions.GetRegex(WellKnownRegularExpressions.CommitHash);
            var value = new string('A', 32) + new string('b', 32);

            Assert.Matches(regex.Value, value);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Does_Not_Match_Hash_Substring_In_Ref()
        {
            var regex = WellKnownRegularExpressions.GetRegex(WellKnownRegularExpressions.CommitHash);
            var value = $"refs/heads/{new string('a', 64)}";

            Assert.DoesNotMatch(regex.Value, value);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Unknown_Key_Returns_Null()
        {
            var regex = WellKnownRegularExpressions.GetRegex("UnknownType");

            Assert.Null(regex);
        }
    }
}
