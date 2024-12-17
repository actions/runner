using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;

namespace Runner.Client
{
    partial class Program
    {
        private class NoopConsoleLifetime : IHostLifetime
        {
            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task WaitForStartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
