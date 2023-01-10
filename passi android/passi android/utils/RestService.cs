using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using AppConfig;
using passi_android.Menu;
using RestSharp;

namespace passi_android.utils
{
    public static class RestService
    {


        public static Task<IRestResponse> ExecuteGetAsync(Provider provider, string requestUri)
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, Method.GET);
            return client.ExecuteAsync(request);
        }

        public static Task<IRestResponse> ExecuteAsync(Provider provider, string requestUri)
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, Method.GET);
            return client.ExecuteAsync(request);
        }

        public static Task<IRestResponse> ExecuteAsync(Provider provider, string requestUri, Method method)
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, method);
            return client.ExecuteAsync(request);
        }

        public static Task<IRestResponse> ExecutePostAsync<T>(Provider provider, string requestUri, T item)
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, Method.POST);
            request.AddJsonBody(item);
            return client.ExecuteAsync(request);
        }

        private static RestClient GetClient(Provider provider)
        {
            var client = new RestClient(provider.WebApiUrl);
            client.ReadWriteTimeout = 100000;
            client.Timeout = 100000;
            return client;
        }
    }
}