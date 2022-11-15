using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ConfigurationManager
{
    public class AppSetting
    {
        public bool PrefferAppsettingFile = false;
        private IConfiguration _configuration;
        private ILogger _logger;
        private ConcurrentDictionary<string, string> _values = new ConcurrentDictionary<string, string>();

        public AppSetting(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string this[string key]
        {
            get
            {
                var value = _configuration.GetSection("AppSetting").GetSection(key).Value;
                var environmentVariable = Environment.GetEnvironmentVariable(key);
                var result = PrefferAppsettingFile ? value : environmentVariable ?? value;
                return result;
            }
        }
    }
}