namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public static class Program
    {
        //this is a special entry point used (for now) only by RunIPCEndToEnd test,
        //which launches a second process to verify IPC pipes with an end-to-end test
        public static void Main(string[] args)
        {
            if (null != args && 3 == args.Length && "spawnclient".Equals(args[0].ToLower()))
            {
                ProcessChannelL0.RunAsync(args).Wait();
            }
        }
    }
}
