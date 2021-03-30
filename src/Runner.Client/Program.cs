using System;

using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;

namespace Runner.Client
{
    class Program
    {
        static int Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--workflow",
                    description: "Workflow to run"),
                new Option<string>(
                    "--server",
                    getDefaultValue: () => "http://localhost:5000",
                    description: "Runner.Server address"),
                new Option<string>(
                    "--payload",
                    "Webhook payload to send to the Runner"),
                new Option<string>(
                    "--event",
                    getDefaultValue: () => "push",
                    description: "Which event to send to a worker")
            };

            rootCommand.Description = "Send events to your runner";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<string, string, string, string>(async (workflow, server, payload, e) =>
            {
                if(workflow == null || payload == null) {
                    Console.WriteLine("Missing `--workflow` or `--payload` option, type `--help` for help");
                    return -1;
                }
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-GitHub-Event", e ?? "push");
                var b = new UriBuilder(server);
                var query = new QueryBuilder();
                b.Path = "runner/host/_apis/v1/Message";
                try {
                    query.Add("workflow", await File.ReadAllTextAsync(workflow, Encoding.UTF8));
                } catch {
                    Console.WriteLine($"Failed to read file: {workflow}");
                    return 1;
                }
                b.Query = query.ToQueryString().ToString().TrimStart('?');
                string payloadContent;
                try {
                    payloadContent = await File.ReadAllTextAsync(payload, Encoding.UTF8);
                } catch {
                    Console.WriteLine($"Failed to read file: {payload}");
                    return 1;
                }
                try {
                    await client.PostAsync(b.Uri.ToString(), new StringContent(payloadContent));
                } catch {
                    Console.WriteLine($"Failed to connect to Server {server}, make shure the server is running on that address or port");
                    return 1;
                }
                Console.WriteLine($"Job request sent to {server}");
                return 0;
            });

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }
    }
}