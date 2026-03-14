using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Services;

namespace passi_webapi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(500, 500);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseLog4Net()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<PassiApiStartup>();
                });
    }
}