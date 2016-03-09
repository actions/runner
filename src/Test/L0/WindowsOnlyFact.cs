using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class WindowsOnlyFact : FactAttribute
    {
#if (OS_OSX || OS_LINUX)
        public WindowsOnlyFact()
        {
            Skip = "Skipped on non windows platform";
        }
#endif
    }
}