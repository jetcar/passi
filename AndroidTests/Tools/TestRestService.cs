using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using passi_android.StorageModels;
using passi_android.utils.Services;
using RestSharp;

namespace AndroidTests.Tools;

internal class TestRestService : IRestService
{
    public Task<RestResponse> ExecuteGetAsync(ProviderDb provider, string requestUri)
    {
        RestResponse result = null;
        if (Result.ContainsKey(requestUri))
        {
            result = Result[requestUri];
        }
        Result.Remove(requestUri);

        return Task.FromResult(result ?? new RestResponse());
    }

    public Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri)
    {
        RestResponse result = null;
        if (Result.ContainsKey(requestUri))
        {
            result = Result[requestUri];
        }
        Result.Remove(requestUri);

        return Task.FromResult(result ?? new RestResponse());
    }

    public Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri, Method method)
    {
        RestResponse result = null;
        if (Result.ContainsKey(requestUri))
        {
            result = Result[requestUri];
        }
        Result.Remove(requestUri);

        return Task.FromResult(result ?? new RestResponse());

    }

    public Task<RestResponse> ExecutePostAsync<T>(ProviderDb provider, string requestUri, T item) where T : class
    {
        RestResponse result = null;
        if (Result.ContainsKey(requestUri))
        {
            result = Result[requestUri];
        }
        Result.Remove(requestUri);

        return Task.FromResult(result ?? new RestResponse());
    }

    public static Dictionary<string, RestResponse> Result = new Dictionary<string, RestResponse>();
}