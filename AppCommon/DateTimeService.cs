using AppConfig;
using RestSharp;
using System;
using System.Threading;

namespace AppCommon
{
    public class DateTimeService : IDateTimeService
    {
        private long serverTimeDiffirence = 0;

        public void Init()
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

        private long ConvertToTimestamp(DateTime value)
        {
            long epoch = (value.Ticks - 621355968000000000) / 10000000;
            return epoch;
        }

        public DateTime UtcNow
        {
            get
            {
                return DateTime.UtcNow.AddTicks(-serverTimeDiffirence);
            }
        }
    }

    public interface IDateTimeService
    {
        DateTime UtcNow { get; }

        void Init();
    }
}