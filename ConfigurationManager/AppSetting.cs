using System;
using Microsoft.Extensions.Configuration;

namespace ConfigurationManager
{
    public class AppSetting
    {
        public bool PrefferAppsettingFile = false;
        private IConfiguration _configuration;

        public AppSetting(IConfiguration configuration)
        {
            _configuration = configuration;
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