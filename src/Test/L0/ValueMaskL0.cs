using Xunit;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{    
    public sealed class ValueMaskL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ValueMaskTests_GetPositions_EmptyValue()
        {
            var masker = new ValueSecret(null);
            var input = "abcdefg";

            var positions = masker.GetPositions(input);
            Assert.Equal(0, positions.Count());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ValueMaskTests_GetPositions_EmptyInput()
        {
            var masker = new ValueSecret("def");
            string input = null;

            var positions = masker.GetPositions(input);
            Assert.Equal(0, positions.Count());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ValueMaskTests_GetPositions_Basic()
        {
            var masker = new ValueSecret("def");
            string input = "abcdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(3, positions[0].Start);
            Assert.Equal(3, positions[0].Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ValueMaskTests_GetPositions_BeginningOfString()
        {
            var masker = new ValueSecret("abc");
            string input = "abcdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(0, positions[0].Start);
            Assert.Equal(3, positions[0].Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ValueMaskTests_GetPositions_EndOfString()
        {
            var masker = new ValueSecret("efg");
            string input = "abcdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(1, positions.Count);
            Assert.Equal(4, positions[0].Start);
            Assert.Equal(3, positions[0].Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ValueMaskTests_GetPositions_Multiple()
        {
            var masker = new ValueSecret("def");
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
        public void ValueMaskTests_GetPositions_Overlap()
        {
            var masker = new ValueSecret("cdcd");
            string input = "abcdcdcdefg";

            var positions = masker.GetPositions(input).ToList();
            Assert.Equal(2, positions.Count);
            Assert.Equal(2, positions[0].Start);
            Assert.Equal(4, positions[0].Length);
            Assert.Equal(4, positions[1].Start);
            Assert.Equal(4, positions[1].Length);
        }
    }
}
