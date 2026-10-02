using System;
using System.Globalization;
using GitHub.DistributedTask.Logging;
using Xunit;

namespace GitHub.Runner.Common.Tests.Sdk
{
    public sealed class ValueEncodersL0
    {
        [Theory]
        [InlineData("th-TH")]
        [InlineData("en-US")]
        [InlineData("")]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void PowerShellPreAmpersandEscape_IsCultureInvariant(string cultureName)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

                Assert.Equal("abcdef&", ValueEncoders.PowerShellPreAmpersandEscape("abcdef&"));
                Assert.Equal("abcdef&+", ValueEncoders.PowerShellPreAmpersandEscape("abcdef&+ghijkl"));
                Assert.Equal("abc&def&", ValueEncoders.PowerShellPreAmpersandEscape("abc&def&"));
                Assert.Equal(string.Empty, ValueEncoders.PowerShellPreAmpersandEscape("ab&"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Theory]
        [InlineData("th-TH")]
        [InlineData("en-US")]
        [InlineData("")]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void PowerShellPostAmpersandEscape_IsCultureInvariant(string cultureName)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

                Assert.Equal("ghijkl", ValueEncoders.PowerShellPostAmpersandEscape("abc&ghijkl"));
                Assert.Equal(string.Empty, ValueEncoders.PowerShellPostAmpersandEscape("abcdef&"));
                Assert.Equal("hijklm", ValueEncoders.PowerShellPostAmpersandEscape("abc&+ghijklm"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }
}
