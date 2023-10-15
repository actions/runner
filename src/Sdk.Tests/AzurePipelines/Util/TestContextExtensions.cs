using Xunit.Abstractions;

namespace Runner.Server.Azure.Devops
{
    internal static class TestContextExtensions
    {
        public static TestContext AddOutputToTest(this TestContext ctx, ITestOutputHelper xUnitHelper)
        {
            ctx.AddTraceWriter(new xUnitTraceWriter(xUnitHelper));
            return ctx;
        }
    }
}
