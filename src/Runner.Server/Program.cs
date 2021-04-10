using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
                    var contentRoot = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    var wwwRoot = System.IO.Path.Combine(contentRoot, "wwwroot");
                    if(System.IO.Directory.Exists(contentRoot) && System.IO.Directory.Exists(wwwRoot)) {
                        webBuilder.UseContentRoot(contentRoot);
                        webBuilder.UseWebRoot(wwwRoot);
                    }
                });
    }
}
