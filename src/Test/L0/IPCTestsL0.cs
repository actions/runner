using System;
using Xunit;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class IPCTests
    {
        //this is a special entry point used (for now) only by RunIPCEndToEnd test,
        //which launches a second process to verify IPC pipes with an end-to-end test
        public static void Main(string[] args)
        {
            if (null != args && 3 == args.Length && "spawnclient".Equals(args[0].ToLower()))
            {
                RunAsync(args).Wait();
            }
        }

        //RunAsync is an "echo" type service which reads
        //one message from IPCClient and sends back to the
        //server same data 
        public static async Task RunAsync(string[] args)
        {            
            using (var client = new IPCClient())
            {
                SemaphoreSlim signal = new SemaphoreSlim(0, 1);
                Func<object, IPCPacket, Task> echoFunc = async (sender, packet) =>
                {
                    var cs2 = new CancellationTokenSource();
                    await client.Transport.SendAsync(packet.MessageType, packet.Body, cs2.Token);
                    signal.Release();
                };
                client.Transport.PacketReceived += echoFunc;                
                await client.Start(args[1], args[2]);
                // Wait server calls us once and we reply back
                await signal.WaitAsync(5000);                
                client.Transport.PacketReceived -= echoFunc;
                await client.Stop();                
            }
        }

        //RunIPCEndToEnd test starts another process (the RunAsync function above),
        //sends one packet and receives one packet using IPCServer/IPCClient classes,
        //and finally verifies if the data we sent is identical to what we have received
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunIPCEndToEnd()
        {            
            using (var server = new IPCServer())
            {
                SemaphoreSlim signal = new SemaphoreSlim(0, 1);
                IPCPacket result = new IPCPacket(-1,"");
                Func<object, IPCPacket, Task> verifyFunc = (sender, packet) =>
                {
                    result = packet;
                    signal.Release();                    
                    return Task.CompletedTask;
                };
                server.Transport.PacketReceived += verifyFunc;
                server.Start("Test");
                var testBody = "some text";
                var testId = 123;
                var cs = new CancellationTokenSource();
                await server.Transport.SendAsync(testId, testBody, cs.Token);

                bool timedOut = !await signal.WaitAsync(5000);
                // Wait until response is received
                if (timedOut)
                {
                    Assert.True(false, "Test timed out.");
                }
                else {
                    Assert.True(testId == result.MessageType && testBody.Equals(result.Body));
                }                
                server.Transport.PacketReceived -= verifyFunc;
                await server.Stop();
            }            
        }
    }
}
