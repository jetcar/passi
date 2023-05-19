using System;
using System.Threading.Tasks;
using passi_android.Menu;
using passi_android.utils;
using RestSharp;

namespace AndroidTests.Tools;

internal class TestRestService : IRestService
{
    public Task<RestResponse> ExecuteGetAsync(ProviderDb provider, string requestUri)
    {
        throw new NotImplementedException();
    }

    public Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri)
    {
        throw new NotImplementedException();
    }

    public Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri, Method method)
    {
        return new Task<RestResponse>(() =>
        {
            return null;
        });    }

    public Task<RestResponse> ExecutePostAsync<T>(ProviderDb provider, string requestUri, T item) where T : class
    {
        return new Task<RestResponse>(() =>
        {
            return null;
        });
    }
}