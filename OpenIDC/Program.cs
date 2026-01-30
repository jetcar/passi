using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Serilog;


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
            .UseSerilog((hostingContext, loggerConfiguration) =>
            {
                var hostingContextConfiguration = hostingContext.Configuration;
                loggerConfiguration.ReadFrom.Configuration(hostingContextConfiguration);
                //loggerConfiguration.WriteTo.LogDNA(apiKey: SecretsLoader.GetValueFromKeyVault("dnaLogKey"), appName: "samplewebapp");
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
