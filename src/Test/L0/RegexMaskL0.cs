using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class RegexMaskL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void RegexMaskTests_GetPositions_Basic()
        {
            var masker = new RegexMask("def");
            string input = "abcdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(3, positions[0].Item1);
            Assert.Equal(3, positions[0].Item2);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void RegexMaskTests_GetPositions_BeginningOfString()
        {
            var masker = new RegexMask("abc");
            string input = "abcdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(0, positions[0].Item1);
            Assert.Equal(3, positions[0].Item2);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void RegexMaskTests_GetPositions_EndOfString()
        {
            var masker = new RegexMask("efg");
            string input = "abcdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(4, positions[0].Item1);
            Assert.Equal(3, positions[0].Item2);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void RegexMaskTests_GetPositions_Multiple()
        {
            var masker = new RegexMask("def");
            string input = "abcdefgdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(2, positions.Count);
            Assert.Equal(3, positions[0].Item1);
            Assert.Equal(3, positions[0].Item2);
            Assert.Equal(7, positions[1].Item1);
            Assert.Equal(3, positions[1].Item2);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void RegexMaskTests_GetPositions_Expression()
        {
            var masker = new RegexMask("[ab]");
            string input = "deabfgb";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(3, positions.Count);
            Assert.Equal(2, positions[0].Item1);
            Assert.Equal(1, positions[0].Item2);
            Assert.Equal(3, positions[1].Item1);
            Assert.Equal(1, positions[1].Item2);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void RegexMaskTests_GetPositions_EscapedCharacters()
        {
            var regex = "a]bc[";
            var masker = new RegexMask(Regex.Escape(regex));
            string input = "dfa]bc[]abcdfabc";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(2, positions[0].Item1);
            Assert.Equal(5, positions[0].Item2);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void RegexMaskTests_GetPositions_Overlap()
        {
            var regex = "bcbc";
            var masker = new RegexMask(regex);
            string input = "aabcbcbc";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(2, positions.Count);
            Assert.Equal(2, positions[0].Item1);
            Assert.Equal(4, positions[0].Item2);
            Assert.Equal(4, positions[1].Item1);
            Assert.Equal(4, positions[1].Item2);
        }
    }
}
