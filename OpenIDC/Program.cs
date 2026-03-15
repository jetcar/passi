using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Services;
using NLog.Web;

public static class Program
{
    public static void Main(string[] args)
    {
        ThreadPool.SetMinThreads(500, 500);
        var app = CreateWebHostBuilder(args).Build();
        app.Run();
    }

    public static IHostBuilder CreateWebHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseNLog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
