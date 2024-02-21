using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Runner.Server
{
    class Options : IOptions<MemoryCacheOptions>
    {
        public MemoryCacheOptions Value => new MemoryCacheOptions();
    }
    public partial class Program
    {
        public static void Main(string[] args)
        {
            if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) && System.Environment.GetEnvironmentVariable("GHARUN_CHANGE_PROCESS_GROUP") == "1") {
                try {
                    if (Mono.Unix.Native.Syscall.setpgid(0, 0) != 0) {
                        Console.WriteLine($"Failed to change Process Group");
                    }
                } catch {
                    Console.WriteLine($"Failed to change Process Group exception");
                }
            }
            // Delete the environment variable, otherwise it is defined inside the job
            System.Environment.SetEnvironmentVariable("GHARUN_CHANGE_PROCESS_GROUP", null);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    var contentRoot = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    var wwwRoot = System.IO.Path.Combine(contentRoot, "wwwroot");
                    if(System.IO.Directory.Exists(contentRoot) && System.IO.Directory.Exists(wwwRoot)) {
                        webBuilder.UseContentRoot(contentRoot);
                        webBuilder.UseWebRoot(wwwRoot);
                    }
                    var RUNNER_SERVER_APP_JSON_SETTINGS_FILE = Environment.GetEnvironmentVariable("RUNNER_SERVER_APP_JSON_SETTINGS_FILE");
                    if(RUNNER_SERVER_APP_JSON_SETTINGS_FILE != null) {
                        webBuilder.ConfigureAppConfiguration((ctx, config) => {
                            config.Sources.Clear();
                            config.Properties.Clear();
                            config.Add(new JsonStreamConfigurationSource() { Stream = File.OpenRead(RUNNER_SERVER_APP_JSON_SETTINGS_FILE) });
                            config.Add(new EnvironmentVariablesConfigurationSource() { Prefix = "RUNNER_SERVER_" } );
                        });
                    }
                });
    }
}
