using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent;

namespace Microsoft.VisualStudio.Services.Agent.CLI
{
    public static class Program
    {
        public static Int32 Main(String[] args)
        {
            HostContext context = new HostContext("Agent");
            return RunAsync(context, args).Result;
        }

        public static async Task<Int32> RunAsync(IHostContext context, String[] args)
        {
            try
            {
                var clArgs = new ProgramArguments(context, args);
                if (clArgs.Configure)
                {
                    throw new System.NotImplementedException();
                }

                var listener = context.GetService<IMessageListener>();
                if (await listener.CreateSessionAsync(context))
                {
                    await listener.ListenAsync(context);
                }

                await listener.DeleteSessionAsync(context);
            }
            catch (Exception)
            {
                // TODO: Log exception.
                return 1;
            }

            return 0;
        }
    }
}
