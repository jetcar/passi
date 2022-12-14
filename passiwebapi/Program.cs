using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Security.Cryptography.X509Certificates;
using ConfigurationManager;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Http.TextFormatters;

namespace passi_webapi
{
    public class Program
    {
        public static void Main(string[] args)
        {

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
