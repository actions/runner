using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class RegexSecretL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReturnsPosition()
        {
            var masker = new RegexSecret("def");
            string input = "abcdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(3, positions[0].Start);
            Assert.Equal(3, positions[0].Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReturnsPositionFromBeginningOfString()
        {
            var masker = new RegexSecret("abc");
            string input = "abcdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(0, positions[0].Start);
            Assert.Equal(3, positions[0].Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReturnsPositionFromEndOfString()
        {
            var masker = new RegexSecret("efg");
            string input = "abcdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(4, positions[0].Start);
            Assert.Equal(3, positions[0].Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReturnsMultiplePositions()
        {
            var masker = new RegexSecret("def");
            string input = "abcdefgdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(2, positions.Count);
            Assert.Equal(3, positions[0].Start);
            Assert.Equal(3, positions[0].Length);
            Assert.Equal(7, positions[1].Start);
            Assert.Equal(3, positions[1].Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReturnsMatchPosition()
        {
            var masker = new RegexSecret("[ab]");
            string input = "deabfgb";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(3, positions.Count);
            Assert.Equal(2, positions[0].Start);
            Assert.Equal(1, positions[0].Length);
            Assert.Equal(3, positions[1].Start);
            Assert.Equal(1, positions[1].Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReturnsEscapedMatchPosition()
        {
            var regex = "a]bc[";
            var masker = new RegexSecret(Regex.Escape(regex));
            string input = "dfa]bc[]abcdfabc";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(2, positions[0].Start);
            Assert.Equal(5, positions[0].Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReturnsOverlappingPositions()
        {
            var regex = "bcbc";
            var masker = new RegexSecret(regex);
            string input = "aabcbcbc";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(2, positions.Count);
            Assert.Equal(2, positions[0].Start);
            Assert.Equal(4, positions[0].Length);
            Assert.Equal(4, positions[1].Start);
            Assert.Equal(4, positions[1].Length);
        }
    }
}
