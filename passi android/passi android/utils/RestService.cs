using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using AppConfig;
using RestSharp;

namespace passi_android.utils
{
    public static class RestService
    {
        private static RestClient client;

        static RestService()
        {
            if (Debugger.IsAttached)
                client = new RestClient(ConfigSettings.WebApiUrlLocal);
            else
                client = new RestClient(ConfigSettings.WebApiUrl);
            client.ReadWriteTimeout = 100000;
            client.Timeout = 100000;
        }

        public static Task<IRestResponse> ExecuteGetAsync(string requestUri)
        {
            var request = new RestRequest(requestUri, Method.GET);
            return client.ExecuteAsync(request);
        }

        public static Task<IRestResponse> ExecuteAsync(string requestUri)
        {
            var request = new RestRequest(requestUri, Method.GET);
            return client.ExecuteAsync(request);
        }

        public static Task<IRestResponse> ExecuteAsync(string requestUri, Method method)
        {
            var request = new RestRequest(requestUri, method);
            return client.ExecuteAsync(request);
        }

        public static Task<IRestResponse> ExecutePostAsync<T>(string requestUri, T item)
        {
            var request = new RestRequest(requestUri, Method.POST);
            request.AddJsonBody(item);
            return client.ExecuteAsync(request);
        }
    }
}