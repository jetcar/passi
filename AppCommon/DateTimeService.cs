using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AppConfig;
using RestSharp;

namespace AppCommon
{
    public static class DateTimeService
    {
        private static long serverTimeDiffirence = 0;

        public static void Init()
        {
            new Thread(() =>
            {
                RestClient client = new RestClient(ConfigSettings.WebApiUrl);
                var request = new RestRequest(ConfigSettings.Time, Method.Get);

                while (true)
                {
                    var start = DateTime.UtcNow.Ticks;
                    var result = client.ExecuteAsync(request);
                    if (result.Result.IsSuccessful)
                    {
                        var end = DateTime.UtcNow.Ticks;
                        var requestDelay = end - start;
                        serverTimeDiffirence = end - long.Parse(result.Result.Content) -
                                               requestDelay;
                        return;
                    }
                    Thread.Sleep(1);
                }
            }).Start();
            
            
        }
        private static long ConvertToTimestamp(DateTime value)
        {
            long epoch = (value.Ticks - 621355968000000000) / 10000000;
            return epoch;
        }

        public static DateTime UtcNow
        {
            get
            {
                return DateTime.UtcNow.AddTicks(-serverTimeDiffirence);
            }
        }
    }
}
