using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Build
{
    public sealed class TfsVCCommandManagerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FeaturesEnumHasCorrectValues()
        {
            var hashtable = new HashSet<int>();
            foreach (int val in Enum.GetValues(typeof(TfsVCFeatures)))
            {
                Assert.True(hashtable.Add(val), $"Duplicate value detected: {val}");
                Assert.True(val >= 0, $"Must be greater than or equal to zero: {val}");
                if (val > 0)
                {
                    double log = Math.Log(val, 2);
                    Assert.True(log - Math.Floor(log) == 0, $"Must be a power of 2: {val}");
                }
            }
        }
    }
}
