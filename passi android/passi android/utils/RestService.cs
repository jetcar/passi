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


        public static Task<RestResponse> ExecuteGetAsync(ProviderDb provider, string requestUri)
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, Method.Get);
            request.Timeout = 100000;
            return client.ExecuteAsync(request);
        }

        public static Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri)
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, Method.Get);
            request.Timeout = 100000;
            return client.ExecuteAsync(request);
        }

        public static Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri, Method method)
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, method);
            request.Timeout = 100000;
            return client.ExecuteAsync(request);
        }

        public static Task<RestResponse> ExecutePostAsync<T>(ProviderDb provider, string requestUri, T item) where T : class
        {
            var client = GetClient(provider);
            var request = new RestRequest(requestUri, Method.Post);
            request.Timeout = 3000;

            request.AddJsonBody(item);
            return client.ExecuteAsync(request);
        }

        private static RestClient GetClient(ProviderDb provider)
        {
            var client = new RestClient(provider.WebApiUrl);
            return client;
        }
    }
}