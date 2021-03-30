using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Runner.Server.Controllers;

namespace Runner.Server
{
    class Options : IOptions<MemoryCacheOptions>
    {
        public MemoryCacheOptions Value => new MemoryCacheOptions();
    }
    public class Program
    {
        public static void Main(string[] args)
        {
            //var b = new ConfigurationBuilder();
            //b.AddJsonFile("C:\\Users\\Christopher\\runner\\src\\Runner.Server\\appsettings.Development.json");
            //new MessageController(/* new Logger<MessageController>(new LoggerFactory()) */b.Build(), new MemoryCache(new Options())).ConvertYaml("C:/Users/Christopher/runner/src/Runner.Server/test.yml");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
