using Microsoft.Extensions.Configuration;
using System;
using MessagePack.Formatters;

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
                if (string.IsNullOrEmpty(value))
                {
                    value = _configuration.GetSection(key).Value;
                }
                var environmentVariable = Environment.GetEnvironmentVariable(key);
                var result = PrefferAppsettingFile ? value : environmentVariable ?? value;
                return result;
            }
            set
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    public class DBNullFormatter
    {
        public static IMessagePackFormatter Instance
        {
            get
            {
                return new MineDBNullFormatter();
            }
        }
    }

    public class MineDBNullFormatter : IMessagePackFormatter
    {
    }
}