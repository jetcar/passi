using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace Services
{
    public static class Log4NetExtensions
    {
        private const string CorrelationIdPropertyName = "CorrelationId";
        private const string DefaultConfigFileName = "log4net.config";
        private const string ParentDirectory = "..";

        /// <summary>
        /// Configures Log4net for the application using the log4net.config file
        /// </summary>
        public static IHostBuilder UseLog4Net(this IHostBuilder hostBuilder, string configFileName = DefaultConfigFileName)
        {
            // Configure log4net
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            var configFile = new FileInfo(configFileName);

            if (!configFile.Exists)
            {
                // Try to find it in parent directory (solution root)
                var parentConfigFile = new FileInfo(Path.Combine(ParentDirectory, configFileName));
                if (parentConfigFile.Exists)
                {
                    configFile = parentConfigFile;
                }
            }

            log4net.Config.XmlConfigurator.Configure(logRepository, configFile);

            return hostBuilder;
        }

        /// <summary>
        /// Adds correlation ID middleware to track requests across systems
        /// </summary>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }

        /// <summary>
        /// Gets correlation ID from the current request context
        /// </summary>
        public static string GetCorrelationId()
        {
            return log4net.LogicalThreadContext.Properties[CorrelationIdPropertyName]?.ToString() ?? "no-correlation-id";
        }
    }
}
