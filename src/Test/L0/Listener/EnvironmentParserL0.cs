using GitHub.Runner.Listener;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class EnvironmentParserL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnvironmentParser")]
        //process 2 new job messages, and one cancel message
        public void TestInvalidKeys()
        {
            var lines = new[]
            {
                "test",
                "none",
                "keywithoutvalue=",
                "punctuation."
            };
            
            var variables = EnvironmentParser.LoadAndSetEnvironment(lines);
            Assert.Empty(variables);
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "EnvironmentParser")]
        //process 2 new job messages, and one cancel message
        public void TestValidKeys()
        {
            var lines = new[]
            {
                "RunnerTestOne=One",
                "RunnerTestTwo=Two",
                "RunnerTestThree=Three=Four",
                "RunnerTestFour=Four!Four",
                "RunnerTestFour=Four.Four"
            };
            
            var variables = EnvironmentParser.LoadAndSetEnvironment(lines);
            Assert.Equal(4, variables.Count);
            Assert.Equal("One", variables["RunnerTestOne"]);
            Assert.Equal("Two", variables["RunnerTestTwo"]);
            Assert.Equal("Three=Four", variables["RunnerTestThree"]);
            Assert.Equal("Four.Four", variables["RunnerTestFour"]);
        }
    }
}
