using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Util
{            
    public class StringUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void FormatAlwaysCallsFormat()
        {
            // Arrange.
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                var variableSets = new[]
                {
                    new { Format = null as string, Args = null as object[], Expected = string.Empty },
                    new { Format = null as string, Args = new object[0], Expected = string.Empty },
                    new { Format = null as string, Args = new object[] { 123 }, Expected = string.Empty },
                    new { Format = "Some message", Args = null as object[], Expected = "Some message" },
                    new { Format = "Some message", Args = new object[0], Expected = "Some message" },
                    new { Format = "Some message", Args = new object[] { 123 }, Expected = "Some message" },
                    new { Format = "Some format '{0}'", Args = null as object[], Expected = "Some format ''" },
                    new { Format = "Some format '{0}'", Args = new object[0], Expected = "Some format ''" },
                    new { Format = "Some format '{0}'", Args = new object[] { 123 }, Expected = "Some format '123'" },
                };
                foreach (var variableSet in variableSets)
                {
                    trace.Info($"{nameof(variableSet)}:");
                    trace.Info(variableSet);

                    // Act.
                    string actual = StringUtil.Format(variableSet.Format, variableSet.Args);

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void FormatHandlesFormatException()
        {
            // Arrange.
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                var variableSets = new[]
                {
                    new { Format = "Bad format { 0}", Args = null as object[], Expected = "Bad format { 0}" },
                    new { Format = "Bad format { 0}", Args = new object[0], Expected = "Bad format { 0} " },
                    new { Format = "Bad format { 0}", Args = new object[] { null }, Expected = "Bad format { 0} " },
                    new { Format = "Bad format { 0}", Args = new object[] { 123, 456 }, Expected = "Bad format { 0} 123, 456" },
                };
                foreach (var variableSet in variableSets)
                {
                    trace.Info($"{nameof(variableSet)}:");
                    trace.Info(variableSet);

                    // Act.
                    string actual = StringUtil.Format(variableSet.Format, variableSet.Args);

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void FormatUsesInvariantCulture()
        {
            // Arrange.
            using (TestHostContext hc = new TestHostContext(this))
            {
                CultureInfo originalCulture = CultureInfo.CurrentCulture;
                try
                {
                    CultureInfo.CurrentCulture = new CultureInfo("it-IT");

                    // Act.
                    string actual = StringUtil.Format("{0:N2}", 123456.789);

                    // Actual
                    Assert.Equal("123,456.79", actual);
                }
                finally
                {
                    CultureInfo.CurrentCulture = originalCulture;
                }
            }
        }
    }
}
