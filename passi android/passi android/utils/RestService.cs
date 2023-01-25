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


        public static Task<IRestResponse> ExecuteGetAsync(ProviderDb provider, string requestUri)
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, Method.GET);
            return client.ExecuteAsync(request);
        }

        public static Task<IRestResponse> ExecuteAsync(ProviderDb provider, string requestUri)
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, Method.GET);
            return client.ExecuteAsync(request);
        }

        public static Task<IRestResponse> ExecuteAsync(ProviderDb provider, string requestUri, Method method)
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, method);
            return client.ExecuteAsync(request);
        }

        public static Task<IRestResponse> ExecutePostAsync<T>(ProviderDb provider, string requestUri, T item)
        {
            var client = GetClient(provider);
            client.Timeout = 3000;
            client.ReadWriteTimeout = 3000;
            var request = new RestRequest(requestUri, Method.POST);
            request.AddJsonBody(item);
            return client.ExecuteAsync(request);
        }

        private static RestClient GetClient(ProviderDb provider)
        {
            var client = new RestClient(provider.WebApiUrl);
            client.ReadWriteTimeout = 100000;
            client.Timeout = 100000;
            return client;
        }
    }
}