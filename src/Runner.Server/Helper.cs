using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Runner.Server
{
    public static class Helper
    {
        public static async Task WaitAnyCancellationToken(params CancellationToken[] tokens) {
            using var allTokens = CancellationTokenSource.CreateLinkedTokenSource(tokens);
            using var waitTask = Task.Delay(-1, allTokens.Token);
            await Task.WhenAny(waitTask);
        }

        public static Task RunTaskWithProvider(IServiceProvider serviceProvider, Func<IServiceProvider, Task> func) {
            var scope = serviceProvider.CreateScope();
            return Task.Run(async () => {
                try {
                    await func(scope.ServiceProvider);
                } finally {
                    scope.Dispose();
                }
            });
        }
        public static Task RunTaskWithProvider(IServiceProvider serviceProvider, Action<IServiceProvider> func) {
            var scope = serviceProvider.CreateScope();
            return Task.Run(() => {
                try {
                    func(scope.ServiceProvider);
                } finally {
                    scope.Dispose();
                }
            });
        }
    }
}