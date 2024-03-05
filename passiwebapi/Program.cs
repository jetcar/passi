using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System;

namespace passi_webapi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                using (var reader = new StreamReader(args[0]))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine().Split("=");
                        if (line.Length == 2)
                        {
                            Environment.SetEnvironmentVariable(line[0], line[1]);
                        }
                    }
                }
            }
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