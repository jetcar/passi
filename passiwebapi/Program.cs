using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Serilog;

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
                .UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
                    // loggerConfiguration.WriteTo.LogDNA(apiKey: SecretsLoader.GetValueFromKeyVault("dnaLogKey"), appName: "passiApi");
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<PassiApiStartup>();
                });
    }
}