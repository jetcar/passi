using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System;

namespace WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = CreateWebHostBuilder(args).Build();
            app.Run();
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
                    //loggerConfiguration.WriteTo.LogDNA(apiKey: SecretsLoader.GetValueFromKeyVault("dnaLogKey"), appName: "samplewebapp");
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}