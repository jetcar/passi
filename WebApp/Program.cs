using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Services;

namespace WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(500, 500);
            var app = CreateWebHostBuilder(args).Build();
            app.Run();
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseLog4Net()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}